﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;
using Viewer.Query;
using Viewer.Core.Collections;
using Viewer.Core.UI;
using Viewer.Properties;
using Viewer.Query.Properties;
using Viewer.UI.Explorer;
using Viewer.UI.Forms;
using Viewer.UI.QueryEditor;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ImagesPresenter : Presenter<IImagesView>
    {
        private readonly IExplorer _explorer;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IQueryEvents _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryEvaluatorFactory _queryEvaluatorFactory;

        protected override ExportLifetimeContext<IImagesView> ViewLifetime { get; }

        private Size _currentItemSize = new Size(133, 100);
        private Size MinItemSize => new Size(133, 100);
        private Size MaxItemSize => new Size(
            MinItemSize.Width * 3,
            MinItemSize.Height * 3
        );

        private double _thumbnailSize = 0;

        /// <summary>
        /// Current state of the rectangle selection
        /// </summary>
        private readonly RectangleSelection<EntityView> _rectangleSelection = new RectangleSelection<EntityView>(new EntityViewPathComparer());

        /// <summary>
        /// Currently loaded query
        /// </summary>
        private QueryEvaluator _queryEvaluator;
        
        /// <summary>
        /// Minimal time in milliseconds between 2 poll events.
        /// </summary>
        private const int PollingRate = 200;
        
        /// <summary>
        /// Get current selection strategy based on the state of modifier keys.
        /// If a shift key is pressed, use <see cref="SelectionStrategy.Union"/>.
        /// If a control key is pressed, use <see cref="SelectionStrategy.SymetricDifference"/>.
        /// Otherwise, use <see cref="SelectionStrategy.Replace"/>.
        /// </summary>
        public SelectionStrategy CurrentSelectionStrategy
        {
            get
            {
                var strategy = SelectionStrategy.Replace;
                if (View.ModifierKeyState.HasFlag(Keys.Shift))
                {
                    strategy = SelectionStrategy.Union;
                }
                else if (View.ModifierKeyState.HasFlag(Keys.Control))
                {
                    strategy = SelectionStrategy.SymetricDifference;
                }

                return strategy;
            }
        }
        
        [ImportingConstructor]
        public ImagesPresenter(
            ExportFactory<IImagesView> viewFactory,
            IExplorer explorer,
            IFileSystemErrorView dialogView,
            ISelection selection, 
            IEntityManager entityManager,
            IClipboardService clipboard,
            IQueryEvents state,
            IQueryFactory queryFactory,
            IQueryEvaluatorFactory queryEvaluatorFactory)
        {
            ViewLifetime = viewFactory.CreateExport();
            _explorer = explorer;
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _state = state;
            _queryFactory = queryFactory;
            _queryEvaluatorFactory = queryEvaluatorFactory;

            // initialize context menu options
            UpdateContextOptions();

            // initialize view
            View.ItemSize = _currentItemSize;
            SubscribeTo(View, "View");
        }

        private void UpdateContextOptions()
        {
            View.ContextOptions = Settings.Default.ExternalApplications;
        }

        /// <summary>
        /// Dispose all resources used by current query
        /// </summary>
        private void DisposeQuery()
        {
            _queryEvaluator?.Dispose();
            _queryEvaluator = null;
            View.Items = null; // the items are disposed by the query evaluator
        }
        
        public override void Dispose()
        {
            DisposeQuery();
            base.Dispose();
        }
        
        /// <summary>
        /// Execute given query and show all entities in the result.
        /// </summary>
        /// <param name="query">Query to show</param>
        public async void LoadQueryAsync(IQuery query)
        {
            // release all resources used by the previous query
            DisposeQuery();

            // reset presenter state
            _rectangleSelection.Clear();
            _selection.Clear();

            // start the query
            _queryEvaluator = _queryEvaluatorFactory.Create(query);
            View.Query = _queryEvaluator.Query.Text;
            View.Items = _queryEvaluator.Update();
            View.BeginLoading();
            View.BeginPolling(PollingRate);
            
            try
            {
                await _queryEvaluator.RunAsync();
                View.UpdateItems();
            }   
            catch (OperationCanceledException)
            {
            }
            finally
            {
                View.EndLoading();
            }
        }

        /// <summary>
        /// Set thumbnail size and update the view.
        /// </summary>
        /// <param name="thumbnailSize">Thumbnail size in the [0, 1] range</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="thumbnailSize"/> is not in the [0, 1] range</exception>
        public void SetThumbnailSize(double thumbnailSize)
        {
            if (thumbnailSize < 0 || thumbnailSize > 1)
                throw new ArgumentOutOfRangeException(nameof(thumbnailSize));

            _thumbnailSize = thumbnailSize;
            View.ItemSize = ComputeThumbnailSize();
            View.UpdateItems();
        }
        
        /// <summary>
        /// Compute current thumbnail size.
        /// </summary>
        /// <returns></returns>
        private Size ComputeThumbnailSize()
        {
            var minimal = MinItemSize;
            var maximal = MaxItemSize;
            var weight = _thumbnailSize;
            return new Size(
                (int)(MathUtils.Lerp(minimal.Width, maximal.Width, weight)),
                (int)(MathUtils.Lerp(minimal.Height, maximal.Height, weight))
            );
        }

        private void ChangeSelection(IEnumerable<EntityView> newSelection)
        {
            var changed = _rectangleSelection.Set(newSelection);
            if (!changed)
            {
                return;
            }

            UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            // set global selection
            _selection.Replace(GetEntitiesInSelection());

            // update the view
            foreach (var item in View.Items)
            {
                item.State = _rectangleSelection.Contains(item) ? 
                    FileViewState.Selected : 
                    FileViewState.None;
            }

            View.UpdateItems();
        }

        private IEnumerable<IEntity> GetEntitiesInSelection()
        {
            return _rectangleSelection.Select(item => item.Data);
        }

        #region User input

        private void View_Poll(object sender, EventArgs e)
        {
            if (_queryEvaluator != null)
            {
                View.Items = _queryEvaluator.Update();
                View.ItemSize = ComputeThumbnailSize();
            }

            View.UpdateItems();
        }

        private void View_SelectionBegin(object sender, MouseEventArgs e)
        {
            _rectangleSelection.Begin(e.Location, CurrentSelectionStrategy);
            UpdateSelectedItems();
        }

        private void View_SelectionDrag(object sender, MouseEventArgs e)
        {
            var bounds = _rectangleSelection.GetBounds(e.Location);
            var newSelection = View.GetItemsIn(bounds);
            ChangeSelection(newSelection);

            View.ShowSelection(bounds);
        }

        private void View_SelectionEnd(object sender, MouseEventArgs e)
        {
            var bounds = _rectangleSelection.GetBounds(e.Location);
            var newSelection = View.GetItemsIn(bounds);
            ChangeSelection(newSelection);

            _rectangleSelection.End();
            View.HideSelection();
        }

        private void View_SelectItem(object sender, EntityEventArgs e)
        {
            _rectangleSelection.Begin(Point.Empty, CurrentSelectionStrategy);
            ChangeSelection(new[] { e.Entity });
            _rectangleSelection.End();
        }

        private void View_BeginDragItems(object sender, EventArgs e)
        {
            var dragFiles = GetPathsInSelection().ToArray();
            var data = new DataObject(DataFormats.FileDrop, dragFiles);
            View.BeginDragDrop(data, DragDropEffects.Move);
        }
        
        private void View_HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ChangeSelection(View.Items);
            }
        }
        
        private void View_BeginEditItemName(object sender, EntityEventArgs e)
        {
            View.ShowItemEditForm(e.Entity);
        }

        private void View_CancelEditItemName(object sender, EventArgs e)
        {
            View.HideItemEditForm();
        }

        private void View_RenameItem(object sender, RenameEventArgs e)
        {
            // check the new file name 
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _dialogView.InvalidFileName(e.NewName);
                return;
            }

            // construct the new file path
            var item = e.Entity;
            var basePath = PathUtils.GetDirectoryPath(item.FullPath);
            var newPath = Path.Combine(basePath, e.NewName + Path.GetExtension(item.FullPath));

            // rename the file
            try
            {
                _entityManager.MoveEntity(item.Data, newPath);
                // make sure items in the view are still sorted
                View.Items.Remove(item);
                View.Items.Add(item);
                View.UpdateItems();
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(newPath);
            }
            catch (DirectoryNotFoundException ex)
            {
                _dialogView.DirectoryNotFound(ex.Message);
            }
            catch (IOException)
            {
                _dialogView.FailedToMove(item.FullPath, newPath);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(newPath);
            }
            finally
            {
                View.HideItemEditForm();
            }
        }

        private void View_ViewGotFocus(object sender, EventArgs e)
        {
            _selection.Replace(GetEntitiesInSelection());
        }

        private IEnumerable<string> GetPathsInSelection()
        {
            return _rectangleSelection.Select(item => item.FullPath);
        }

        private void View_CopyItems(object sender, EventArgs e)
        {
            var paths = GetPathsInSelection();
            try
            {
                _clipboard.SetFiles(new ClipboardFileDrop(paths, DragDropEffects.Copy));
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
        }

        private void View_CutItems(object sender, EventArgs e)
        {
            var paths = GetPathsInSelection();
            try
            {
                _clipboard.SetFiles(new ClipboardFileDrop(paths, DragDropEffects.Move));
            }
            catch (ExternalException ex)
            {
                _dialogView.ClipboardIsBusy(ex.Message);
            }
        }

        private void View_DeleteItems(object sender, EventArgs e)
        {
            if (!_rectangleSelection.Any())
            {
                return;
            }

            // confirm delete
            var filesToDelete = GetPathsInSelection().ToArray();
            if (!_dialogView.ConfirmDelete(filesToDelete))
            {
                return;
            }

            // delete entities
            var deletedPaths = new HashSet<string>();
            var entitiesInSelection = _rectangleSelection.Select(item => item.Data);
            foreach (var entity in entitiesInSelection)
            {
                try
                {
                    _entityManager.RemoveEntity(entity);
                    deletedPaths.Add(entity.Path);
                }
                catch (UnauthorizedAccessException)
                {
                    _dialogView.UnauthorizedAccess(entity.Path);
                }
                catch (DirectoryNotFoundException)
                {
                    // ignore
                    deletedPaths.Add(entity.Path);
                }
                catch (PathTooLongException)
                {
                    _dialogView.PathTooLong(entity.Path);
                }
                catch (IOException)
                {
                    _dialogView.FileInUse(entity.Path);
                }
            }

            // remove deleted items from the query and the view
            View.Items.RemoveAll(view => deletedPaths.Contains(view.FullPath));

            // clear selection
            ChangeSelection(Enumerable.Empty<EntityView>());
            
            View.UpdateItems();
        }

        private void View_OpenItem(object sender, EntityEventArgs e)
        {
            if (!_rectangleSelection.Any())
            {
                return;
            }
            
            if (e.Entity.Data is FileEntity fileEntity)
            {
                var items = View.Items.Select(item => item.Data).OfType<FileEntity>().ToList();
                var index = items.IndexOf(fileEntity);
                _state.OpenEntity(items, index < 0 ? 0 : index);
            }
            else
            {
                var query = _queryFactory.CreateQuery(e.Entity.FullPath);
                _state.ExecuteQuery(query);
            }
        }
        
        private void View_CloseView(object sender, EventArgs eventArgs)
        {
            Dispose();
        }
        
        private void View_GoBackInHistory(object sender, EventArgs e)
        {
            _state.Back();
        }

        private void View_GoForwardInHistory(object sender, EventArgs e)
        {
            _state.Forward();
        }
        
        private async void View_OnDrop(object sender, DropEventArgs e)
        {
            if (e.Entity == null)
            {
                return;
            }

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null)
            {
                return;
            }

            try
            {
                if ((e.AllowedEffect & DragDropEffects.Move) != 0)
                {
                    await _explorer.MoveFilesAsync(e.Entity.FullPath, files);
                }
                else if ((e.AllowedEffect & DragDropEffects.Copy) != 0)
                {
                    await _explorer.CopyFilesAsync(e.Entity.FullPath, files);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        #endregion
    }
}
