﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;
using Viewer.Query;
using Viewer.UI.Explorer;
using Viewer.UI.Settings;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.QueryEditor
{
    public interface IEditor
    {
        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns>Task completed when the editor window opens</returns>
        Task<IQueryEditorView> OpenAsync(string path, DockState dockState);

        /// <summary>
        /// Open given file in the editor.
        /// If the file is opened already, the editor will just make its window visible to user.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns></returns>
        IQueryEditorView Open(string path, DockState dockState);

        /// <summary>
        /// Open new empty editor window
        /// </summary>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns>Opened dock content</returns>
        IQueryEditorView OpenNew(DockState dockState);

        /// <summary>
        /// Open new empty editor window
        /// </summary>
        /// <param name="content">Content of the opened window</param>
        /// <param name="dockState">Unknown value won't show the window</param>
        /// <returns>Opened dock content</returns>
        IQueryEditorView OpenNew(string content, DockState dockState);

        /// <summary>
        /// Save <paramref name="query"/> to <paramref name="path"/>.
        /// If the file exists already, it will be rewritten.
        /// </summary>
        /// <param name="path">Path to a file where to save given query</param>
        /// <param name="query">Query to save</param>
        /// <returns>Task finished when <paramref name="query"/> is written to <paramref name="path"/></returns>
        /// <exception cref="IOException">An IO error occurred</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid</exception>
        /// <exception cref="UnauthorizedAccessException">The access is not permitted</exception>
        Task SaveAsync(string path, string query);

        /// <summary>
        /// Close all editor windows
        /// </summary>
        void CloseAll();
    }

    [Export(typeof(IEditor))]
    public class Editor :  IEditor
    {
        private readonly ISettings _settings;
        private readonly IQueryViewRepository _queryViews;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IFileSystem _fileSystem;
        private readonly ExportFactory<QueryEditorPresenter> _editorFactory;

        [ImportingConstructor]
        public Editor(
            ExportFactory<QueryEditorPresenter> editorFactory, 
            ISettings settings,
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView,
            IQueryViewRepository queryViews)
        {
            _settings = settings;
            _queryViews = queryViews;
            _dialogView = dialogView;
            _fileSystem = fileSystem;
            _editorFactory = editorFactory;
        }

        /// <summary>
        /// Opened editor windows
        /// </summary>
        private readonly List<ExportLifetimeContext<QueryEditorPresenter>> _windows = new List<ExportLifetimeContext<QueryEditorPresenter>>();

        public async Task<IQueryEditorView> OpenAsync(string path, DockState dockState)
        {
            // don't open a new window, if an window with this file is opened already
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            // otherwise load the file and show it in a new window
            try
            {
                var data = await _fileSystem.ReadToEndAsync(path);
                return OpenWindow(path, data, dockState);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (FileNotFoundException)
            {
                _dialogView.FileNotFound(path);
            }

            return null;
        }

        public IQueryEditorView Open(string path, DockState dockState)
        {
            var window = FindWindow(path);
            if (window != null)
            {
                return window;
            }

            try
            {
                var data = Encoding.UTF8.GetString(_fileSystem.ReadAllBytes(path));
                return OpenWindow(path, data, dockState);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (FileNotFoundException)
            {
                _dialogView.FileNotFound(path);
            }

            return null;
        }

        public IQueryEditorView OpenNew(DockState dockState)
        {
            return OpenWindow(dockState).View;
        }

        public IQueryEditorView OpenNew(string content, DockState dockState)
        {
            var window = OpenWindow(dockState);
            window.SetContent(null, content);
            return window.View;
        }

        public async Task SaveAsync(string path, string query)
        {
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                var data = Encoding.UTF8.GetBytes(query);
                await stream.WriteAsync(data, 0, data.Length);
            }

            // update views if we have saved the view to the query view directory
            var directory = Path.GetDirectoryName(fullPath);
            var viewsDirectory = Path.GetFullPath(_settings.QueryViewDirectoryPath);
            if (directory == viewsDirectory)
            {
                _queryViews.Add(new Viewer.Query.QueryView(Path.GetFileNameWithoutExtension(fullPath), query, fullPath));
            }
        }

        public void CloseAll()
        {
            foreach (var editor in _windows)
            {
                editor.Value.View.Close();
            }
        }

        private IQueryEditorView FindWindow(string path)
        {
            var window = _windows.Find(editor =>
                StringComparer.CurrentCultureIgnoreCase.Compare(editor.Value.View.FullPath, path) == 0);
            if (window != null)
            {
                window.Value.View.EnsureVisible();
                return window.Value.View;
            }

            return null;
        }

        private IQueryEditorView OpenWindow(string path, string content, DockState dockState)
        {
            var editor = OpenWindow(dockState);
            editor.SetContent(path, content);
            return editor.View;
        }
        
        private QueryEditorPresenter OpenWindow(DockState dockState)
        {
            var editor = _editorFactory.CreateExport();
            if (dockState != DockState.Unknown)
            {
                editor.Value.ShowView("Query", dockState);
            }

            editor.Value.View.CloseView += (sender, args) =>
            {
                _windows.Remove(editor);
                editor.Dispose();
            };
            _windows.Add(editor);
            return editor.Value;
        }
    }
}
