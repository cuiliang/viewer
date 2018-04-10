﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI.Tasks;

namespace Viewer.UI.Explorer
{
    public class DirectoryTreePresenter
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private IFileSystem _fileSystem;
        private IClipboardService _clipboard;
        private IDirectoryTreeView _treeView;
        private IFileSystemErrorView _errorView;
        private IProgressViewFactory _progressViewFactory;

        public DirectoryTreePresenter(
            IDirectoryTreeView treeView, 
            IProgressViewFactory progressViewFactory, 
            IFileSystemErrorView errorView,
            IFileSystem fileSystem,
            IClipboardService clipboard)
        {
            _clipboard = clipboard;
            _fileSystem = fileSystem;

            _errorView = errorView;
            _progressViewFactory = progressViewFactory;
            _treeView = treeView;
            
            PresenterUtils.SubscribeTo(_treeView, this, "View");
        }

        public void UpdateRootDirectories()
        {
            _treeView.LoadDirectories(new string[] { }, GetRoots());
        }
        
        private IEnumerable<DirectoryView> GetRoots()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                var name = PathUtils.GetLastPart(drive.Name);

                if (!string.IsNullOrEmpty(drive.VolumeLabel))
                {
                    yield return new DirectoryView
                    {
                        UserName = drive.VolumeLabel + " (" + name + ")",
                        FileName = name,
                        HasChildren = true
                    };
                }
                else
                {
                    yield return new DirectoryView
                    {
                        UserName = name,
                        FileName = name,
                        HasChildren = true
                    };
                }
            }
        }

        private IEnumerable<DirectoryView> GetValidSubdirectories(string fullPath)
        {
            var result = new List<DirectoryView>();
            try
            {
                var di = new DirectoryInfo(fullPath);
                foreach (var item in di.EnumerateDirectories())
                {
                    if ((item.Attributes & HideFlags) != 0)
                        continue;

                    // Find out if the subdirectory has any children.
                    // If user is not authorized to read this directory, don't add 
                    // the option to expand it in the view.
                    var hasChildren = false;
                    try
                    {
                        hasChildren = item.EnumerateDirectories().Any();
                    }
                    catch (SecurityException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }

                    result.Add(new DirectoryView
                    {
                        UserName = item.Name,
                        FileName = item.Name,
                        HasChildren = hasChildren
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(fullPath);
            }
            catch (SecurityException)
            {
                _errorView.UnauthorizedAccess(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(fullPath);
            }

            return result;
        }

        private void View_ExpandDirectory(object sender, DirectoryEventArgs e)
        {
            var directories = GetValidSubdirectories(e.FullPath);
            _treeView.LoadDirectories(
                PathUtils.Split(e.FullPath),
                directories);
        }
        
        private void View_RenameDirectory(object sender, RenameDirectoryEventArgs e)
        {
            if (!PathUtils.IsValidFileName(e.NewName))
            {
                _errorView.InvalidFileName(e.NewName, PathUtils.GetInvalidFileCharacters());
                return;
            }

            try
            {
                var newPath = Path.Combine(Path.GetDirectoryName(e.FullPath), e.NewName);
                _fileSystem.MoveDirectory(e.FullPath, newPath);
                _treeView.SetDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    FileName = e.NewName,
                    UserName = e.NewName,
                });
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
            }
        }
        
        private void View_DeleteDirectory(object sender, DirectoryEventArgs e)
        {
            if (!_errorView.ConfirmDelete(e.FullPath))
            {
                return;
            }

            try
            {
                _fileSystem.DeleteDirectory(e.FullPath, true);
                _treeView.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
                // we stil want to remove the directory from the view
                _treeView.RemoveDirectory(PathUtils.Split(e.FullPath));
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
            catch (IOException)
            {
                _errorView.FileInUse(e.FullPath);
            }
        }
        
        private void View_CreateDirectory(object sender, DirectoryEventArgs e)
        {
            try
            {
                var newName = "New Folder";
                var directoryPath = Path.Combine(e.FullPath, newName);
                _fileSystem.CreateDirectory(directoryPath);
                _treeView.AddDirectory(PathUtils.Split(e.FullPath), new DirectoryView
                {
                    UserName = newName,
                    FileName = newName
                });
                _treeView.BeginEditDirectory(PathUtils.Split(directoryPath));
            }
            catch (UnauthorizedAccessException)
            {
                _errorView.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _errorView.DirectoryNotFound(e.FullPath);
            }
        }

        private void View_OpenInExplorer(object sender, DirectoryEventArgs e)
        {
            Process.Start(
                Resources.ExplorerProcessName,
                string.Format(Resources.ExplorerOpenFolderArguments, e.FullPath));
        }

        private void View_CopyDirectory(object sender, DirectoryEventArgs e)
        {
            _clipboard.SetFiles(new[] { e.FullPath });
            _clipboard.SetPreferredEffect(DragDropEffects.Copy);
        }
        
        private void View_PasteToDirectory(object sender, PasteEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (files == null)
            {
                return;
            }

            PasteFiles(e.FullPath, files, e.Effect);
        }

        private void View_PasteClipboardToDirectory(object sender, DirectoryEventArgs e)
        {
            PasteFiles(e.FullPath, _clipboard.GetFiles(), _clipboard.GetPreferredEffect());
        }

        private class CopyHandle
        {
            private IFileSystem _fileSystem;
            private IProgressView _progressView;
            private IFileSystemErrorView _dialogView;
            private string _baseDir;
            private string _destDir;
            private CancellationToken _cancellation;

            public CopyHandle(
                IFileSystem fileSystem, 
                string baseDir, 
                string desDir, 
                IProgressView progressView, 
                IFileSystemErrorView dialogView,
                CancellationToken cancellation)
            {
                _fileSystem = fileSystem;
                _baseDir = baseDir;
                _destDir = desDir;
                _dialogView = dialogView;
                _progressView = progressView;
                _cancellation = cancellation;
            }
            
            private string GetDestinationPath(string path)
            {
                var partialPath = path.Substring(_baseDir.Length + 1);
                return Path.Combine(_destDir, partialPath);
            }

            private void CancelIfRequested()
            {
                _cancellation.ThrowIfCancellationRequested();
            }

            public SearchControl CopyDirectory(string path)
            {
                CancelIfRequested();
                var destDir = GetDestinationPath(path);
                _fileSystem.CreateDirectory(destDir);
                return SearchControl.Visit;
            }

            public SearchControl CopyFile(string path)
            {
                CancelIfRequested();
                var destPath = GetDestinationPath(path);
                _progressView.StartWork(path);
                try
                {
                    _fileSystem.CopyFile(path, destPath);
                }
                catch (DirectoryNotFoundException e)
                {
                    _dialogView.DirectoryNotFound(e.Message);
                }
                finally
                {
                    _progressView.FinishWork();
                }

                return SearchControl.None;
            }

            public SearchControl MoveFile(string path)
            {
                CancelIfRequested();
                var destPath = GetDestinationPath(path);
                _progressView.StartWork(path);
                try
                {
                    _fileSystem.MoveFile(path, destPath);
                }
                catch (DirectoryNotFoundException e)
                {
                    _dialogView.DirectoryNotFound(e.Message);
                }
                finally
                {
                    _progressView.FinishWork();
                }

                return SearchControl.None;
            }
        }

        private void PasteFiles(string destinationDirectory, IEnumerable<string> files, DragDropEffects effect)
        {
            // copy files
            var fileCount = (int)_fileSystem.CountFiles(files, true);
            _progressViewFactory.Create().Show(Resources.CopyingFiles_Label, fileCount, view =>
            {
                Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        view.CancellationToken.ThrowIfCancellationRequested();
                        var baseDir = PathUtils.GetDirectoryPath(file);
                        var copy = new CopyHandle(_fileSystem, baseDir, destinationDirectory, view, _errorView, view.CancellationToken);
                        if ((effect & DragDropEffects.Move) != 0)
                            _fileSystem.Search(file, copy.CopyDirectory, copy.MoveFile);
                        else
                            _fileSystem.Search(file, copy.CopyDirectory, copy.CopyFile);
                    }
                }, view.CancellationToken).ContinueWith(task =>
                {
                    // update subdirectories in given path
                    _treeView.LoadDirectories(
                        PathUtils.Split(destinationDirectory),
                        GetValidSubdirectories(destinationDirectory));
                }, TaskScheduler.FromCurrentSynchronizationContext());
            });
        }
    }
}
