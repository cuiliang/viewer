﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core.Collections;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query;

namespace Viewer.UI.Images
{
    public class QueryEvaluator : IDisposable
    {
        // dependencies
        private readonly IFileWatcher _fileWatcher;
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _queryErrorListener;

        // state
        private readonly ConcurrentSortedSet<EntityView> _addRequests;
        private readonly ConcurrentQueue<RenamedEventArgs> _moveRequests;
        private readonly ConcurrentQueue<FileSystemEventArgs> _deleteRequests;
        private SortedList<EntityView> _views;

        /// <summary>
        /// Cancellation of the query evaluation
        /// </summary>
        public CancellationTokenSource Cancellation { get; }

        /// <summary>
        /// Comparer of the result set
        /// </summary>
        public IComparer<EntityView> Comparer => _addRequests.Comparer;

        /// <summary>
        /// Current query
        /// </summary>
        public IQuery Query { get; }

        /// <summary>
        /// Current load task
        /// </summary>
        public Task LoadTask { get; private set; } = Task.CompletedTask;

        public QueryEvaluator(IFileWatcherFactory fileWatcherFactory, ILazyThumbnailFactory thumbnailFactory, IErrorListener queryErrorListener, IQuery query)
        {
            _fileWatcher = fileWatcherFactory.Create();
            _fileWatcher.Renamed += FileWatcherOnRenamed;
            _fileWatcher.Deleted += FileWatcherOnDeleted;
            _thumbnailFactory = thumbnailFactory;
            _queryErrorListener = queryErrorListener;
            Cancellation = new CancellationTokenSource();
            Query = query;
            
            var entityViewComparer = new EntityViewComparer(Query.Comparer);
            _moveRequests = new ConcurrentQueue<RenamedEventArgs>();
            _deleteRequests = new ConcurrentQueue<FileSystemEventArgs>();
            _addRequests = new ConcurrentSortedSet<EntityView>(entityViewComparer);
            _views = new SortedList<EntityView>(entityViewComparer);
        }

        private static bool IsEntityEvent(FileSystemEventArgs e)
        {
            var newExtension = Path.GetExtension(e.FullPath)?.ToLowerInvariant();
            return newExtension == ".jpeg" || 
                   newExtension == ".jpg" || 
                   newExtension == ""; // directory
        }

        private void FileWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsEntityEvent(e))
                return; // skip this event
            _deleteRequests.Enqueue(e);
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            // note: side effect of this check is that it ignores move operations done during the
            //       FileSystemAttributeStorage.Store call. 
            if (!IsEntityEvent(e))
                return; // skip this event
            _moveRequests.Enqueue(e);
        }
        
        /// <summary>
        /// Evaluate the query on a differet thread.
        /// Found entities will be added to a waiting queue.
        /// Use <see cref="Update"/> to get all entities loaded so far.
        /// </summary>
        /// <returns>Task finished when the evaluation ends</returns>
        public Task RunAsync()
        {
            LoadTask = Task.Factory.StartNew(
                Run,
                Cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            return LoadTask;
        }

        /// <summary>
        /// Load the query synchronously. See <see cref="RunAsync"/>
        /// </summary>
        public void Run()
        {
            var directories = new HashSet<string>();

            try
            {
                foreach (var entity in Query.Evaluate(Cancellation.Token))
                {
                    Cancellation.Token.ThrowIfCancellationRequested();

                    // if the entity is in an undiscovered directory,
                    // start watching changes in this directory
                    var parentDirectory = PathUtils.GetDirectoryPath(entity.Path);
                    if (!directories.Contains(parentDirectory))
                    {
                        _fileWatcher.Watch(parentDirectory);
                        directories.Add(parentDirectory);
                    }
                    
                    // add a new entity
                    _addRequests.Add(new EntityView(entity, _thumbnailFactory.Create(entity, Cancellation.Token)));
                }
            }
            catch (QueryRuntimeException e)
            {
                _queryErrorListener.ReportError(0, 0, e.Message);
            }
        }

        /// <summary>
        /// Update current collection.
        /// It takes all changes made so far and applies them to the local collection of <see cref="EntityView"/>s
        /// </summary>
        /// <returns>Modified collection</returns>
        public SortedList<EntityView> Update()
        {
            // process all add requests
            var added = _addRequests.Consume();
            if (added.Count > 0)
            {
                _views = _views.Merge(added);
            }

            // process all rename requests
            while (_moveRequests.TryDequeue(out var req))
            {
                var oldPath = PathUtils.UnifyPath(req.OldFullPath);
                for (var i = 0; i < _views.Count; ++i)
                {
                    if (_views[i].FullPath == oldPath)
                    {
                        var item = _views[i];
                        item.FullPath = req.FullPath;
                        _views.RemoveAt(i);
                        _views.Add(item);
                        break;
                    }
                }
            }

            // process all delete requests
            var deleted = new HashSet<string>();
            while (_deleteRequests.TryDequeue(out var req))
            {
                var path = PathUtils.UnifyPath(req.FullPath);
                deleted.Add(path);
            }

            _views.RemoveAll(item => deleted.Contains(item.FullPath));

            return _views;
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose this evaluator and all system resources.
        /// If a load task is in progress, it will be cancelled and disposed asynchronously.
        /// </summary>
        public void Dispose()
        {
            // stop watching file changes
            _fileWatcher.Dispose();

            // cancel current loading operation
            Cancellation.Cancel();
            LoadTask.ContinueWith(parent =>
            {
                Cancellation.Dispose();
                foreach (var view in _views)
                {
                    view.Dispose();
                }
                _views = null;
            });
        }
    }

    public interface IQueryEvaluatorFactory
    {
        /// <summary>
        /// Create query evaluator from query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        QueryEvaluator Create(IQuery query);
    }

    [Export(typeof(IQueryEvaluatorFactory))]
    public class QueryEvaluatorFactory : IQueryEvaluatorFactory
    {
        private readonly IFileWatcherFactory _fileWatcherFactory;
        private readonly ILazyThumbnailFactory _thumbnailFactory;
        private readonly IErrorListener _errorListener;

        [ImportingConstructor]
        public QueryEvaluatorFactory(IFileWatcherFactory fileWatcherFactory, ILazyThumbnailFactory thumbnailFactory, IErrorListener errorListener)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _thumbnailFactory = thumbnailFactory;
            _errorListener = errorListener;
        }

        public QueryEvaluator Create(IQuery query)
        {
            return new QueryEvaluator(_fileWatcherFactory, _thumbnailFactory, _errorListener, query);
        }
    }
}
