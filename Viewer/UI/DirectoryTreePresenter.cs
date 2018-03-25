﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;

namespace Viewer.UI
{
    public class DirectoryTreePresenter
    {
        /// <summary>
        /// Directory with at least one of these flags will be hidden.
        /// </summary>
        public FileAttributes HideFlags { get; set; } = FileAttributes.Hidden;

        private IDirectoryTreeView _view;

        public DirectoryTreePresenter(IDirectoryTreeView view)
        {
            _view = view;
            _view.ExpandDirectory += OnExpandDirectory;
            _view.RenameDirectory += OnRenameDirectory;
            _view.DeleteDirectory += OnDeleteDirectory;
            _view.CreateDirectory += OnCreateDirectory;
            _view.OpenInExplorer += OnOpenInExplorer;
            _view.CopyDirectory += OnCopyDirectory;
            _view.CutDirectory += OnCutDirectory;
            _view.PasteToDirectory += OnPasteToDirectory;
        }

        public void UpdateRootDirectories()
        {
            _view.LoadDirectories(new string[] { }, GetRoots());
        }
        
        /// <summary>
        /// Get list of printable invalid file name characters in a string.
        /// </summary>
        /// <returns>String containing invalid file name characters separated by comma</returns>
        public string GetInvalidFileCharacters()
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in invalid)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    continue;

                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\r')
                    sb.Append("\\r");
                else
                    sb.Append(c);
                sb.Append(", ");
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3); // remove the last separator
            }

            return sb.ToString();
        }
        
        private void AddFilesToClipboard(StringCollection fileList, DragDropEffects effect)
        {
            Clipboard.Clear();

            var data = new DataObject();
            data.SetFileDropList(fileList);
            data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
            Clipboard.SetDataObject(data, true);
        }

        private IEnumerable<DirectoryView> GetRoots()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                var name = drive.Name.Substring(0, drive.Name.IndexOfAny(DirectoryUtils.PathSeparators));

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
                result.AddRange(
                    from item in di.EnumerateDirectories()
                    where (item.Attributes & HideFlags) == 0
                    select new DirectoryView
                    {
                        UserName = item.Name,
                        FileName = item.Name,
                        HasChildren = item.EnumerateDirectories().Any()
                    });
            }
            catch (UnauthorizedAccessException)
            {
                _view.UnauthorizedAccess(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                _view.DirectoryNotFound(fullPath);
            }

            return result;
        }

        private void OnExpandDirectory(object sender, DirectoryEventArgs e)
        {
            _view.LoadDirectories(
                DirectoryUtils.SplitPath(e.FullPath), 
                GetValidSubdirectories(e.FullPath));
        }
        
        private void OnRenameDirectory(object sender, RenameDirectoryEventArgs e)
        {
            if (!DirectoryUtils.IsValidName(e.NewName))
            {
                e.Cancel();
                _view.InvalidFileName(e.NewName, GetInvalidFileCharacters());
                return;
            }

            try
            {
                DirectoryUtils.Rename(e.FullPath, e.NewName);
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _view.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                e.Cancel();
                _view.DirectoryNotFound(e.FullPath);
            }
        }
        
        private void OnDeleteDirectory(object sender, DirectoryEventArgs e)
        {
            if (!_view.ConfirmDelete(e.FullPath))
            {
                e.Cancel();
                return;
            }

            try
            {
                Directory.Delete(e.FullPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // we don't want to cancel the operation as the delete was technically successful
                _view.DirectoryNotFound(e.FullPath);
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _view.UnauthorizedAccess(e.FullPath);
            }
        }
        
        private void OnCreateDirectory(object sender, CreateDirectoryEventArgs e)
        {
            try
            {
                e.NewName = "New Folder";
                Directory.CreateDirectory(Path.Combine(e.FullPath, e.NewName));
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _view.UnauthorizedAccess(e.FullPath);
            }
            catch (DirectoryNotFoundException)
            {
                e.Cancel();
                _view.DirectoryNotFound(e.FullPath);
            }
        }

        private void OnOpenInExplorer(object sender, DirectoryEventArgs e)
        {
            Process.Start(
                Resources.ExplorerProcessName,
                string.Format(Resources.ExplorerOpenFolderArguments, e.FullPath));
        }

        private void OnCopyDirectory(object sender, DirectoryEventArgs e)
        {
            AddFilesToClipboard(new StringCollection { e.FullPath }, DragDropEffects.Copy);
        }

        private void OnCutDirectory(object sender, DirectoryEventArgs e)
        {
            AddFilesToClipboard(new StringCollection { e.FullPath }, DragDropEffects.Move);
        }
        
        private void OnPasteToDirectory(object sender, DirectoryEventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsFileDropList())
                    return;

                // check whether we should copy or move
                var effect = DragDropEffects.Copy;
                var effectData = (MemoryStream)Clipboard.GetData("Preferred DropEffect");
                if (effectData != null)
                {
                    var reader = new BinaryReader(effectData);
                    effect = (DragDropEffects)reader.ReadInt32();
                }

                // copy/move all files in the clipboard
                var files = Clipboard.GetFileDropList();
                foreach (var source in files)
                {
                    var sepIndex = source.LastIndexOfAny(DirectoryUtils.PathSeparators);
                    var target = Path.Combine(e.FullPath, source.Substring(sepIndex + 1));

                    if ((effect & DragDropEffects.Move) != 0)
                    {
                        if (File.Exists(source))
                        {
                            File.Move(source, target);
                        }
                        else
                        {
                            Directory.Move(source, target);
                        }
                    }
                    else if ((effect & DragDropEffects.Copy) != 0)
                    {
                        if (File.Exists(source))
                        {
                            File.Copy(source, target);
                        }
                        else
                        {
                            DirectoryUtils.Copy(source, target);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // a directory in the clipboard was deleted
                // ignore the event as if it weren't in the clipboard
            }
            catch (FileNotFoundException)
            {
                // a file in the clipboard was deleted
                // ignore the event as if it weren't in the clipboard
            }
            catch (UnauthorizedAccessException)
            {
                e.Cancel();
                _view.UnauthorizedAccess(e.FullPath);
            }

            // update subdirectories in given path
            _view.LoadDirectories(
                DirectoryUtils.SplitPath(e.FullPath), 
                GetValidSubdirectories(e.FullPath));
        }
    }
}
