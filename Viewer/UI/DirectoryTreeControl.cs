﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;
using Viewer.Properties;

namespace Viewer.UI
{
    public partial class DirectoryTreeControl : UserControl
    {
        private DirectoryController _controller = new DirectoryController();
        
        public DirectoryTreeControl()
        {
            InitializeComponent();
            
            var list = new ImageList();
            list.Images.Add(Resources.Directory);

            TreeView.ImageList = list;
            TreeView.ImageIndex = 0;

            // initialize the tree view with ready logical drives
            TreeView.Sorted = true;
            TreeView.BeginUpdate();
            foreach (var drive in _controller.GetDrives())
            {
                var node = TreeView.Nodes.Add(drive.Key, drive.Name);
                foreach (var folder in _controller.GetDirectories(drive.Key))
                {
                    node.Nodes.Add(folder, folder);
                }
            }
            TreeView.EndUpdate();
        }
        
        /// <summary>
        /// Update subdirectories of given node.
        /// </summary>
        /// <param name="node">Node of the directory TreeView</param>
        private void UpdateSubdirectories(TreeNode node)
        {
            var path = GetPath(node);

            TreeView.BeginUpdate();

            // remove old subdirectories
            node.Nodes.Clear();

            // add new subdirectories
            try
            {
                foreach (var directory in _controller.GetDirectories(path))
                {
                    var subnode = node.Nodes.Add(directory, directory);
                    try
                    {
                        var subdirectoryPath = path + Path.DirectorySeparatorChar + directory;
                        foreach (var subdirectory in _controller.GetDirectories(subdirectoryPath))
                        {
                            subnode.Nodes.Add(subdirectory, subdirectory);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // skip folders
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                UnauthorizedAccess(path);
            }
            catch (DirectoryNotFoundException)
            {
                node.Remove();
                DirectoryNotFound(path);
            }
            
            TreeView.EndUpdate();
        }

        private void UnauthorizedAccess(string path)
        {
            MessageBox.Show(
                string.Format(Resources.UnauthorizedAccess_Message, path),
                Resources.UnauthorizedAccess_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void DirectoryNotFound(string path)
        {
            MessageBox.Show(
                string.Format(Resources.DirectoryNotFound_Message, path),
                Resources.DirectoryNotFound_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private string GetPath(TreeNode node)
        {
            var parts = new List<string>();
            while (node != null)
            {
                parts.Add(node.Name);
                node = node.Parent;
            }

            parts.Reverse();
            var fullPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
            if (parts.Count == 1)
            {
                return fullPath + Path.DirectorySeparatorChar;
            }
            return fullPath;
        }
        
        #region TreeView Events

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView.SelectedNode = e.Node;
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            var info = TreeView.HitTest(e.Location);
            if (info.Node != null)
            {
                Cursor.Current = Cursors.Hand;
            }
            else
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void TreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            UpdateSubdirectories(e.Node);
        }

        private void TreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            var node = e.Node;
            var path = GetPath(node);
            if (e.Label == null)
            {
                return;
            }

            // don't rename the directory if we haven't changed the name
            if (e.Label == node.Name)
            {
                e.CancelEdit = true;
                return;
            }

            // rename the directory
            try
            {
                _controller.Rename(path, e.Label);

                // update the UI
                node.Name = e.Label;
                node.Text = e.Label;
                node.EndEdit(false);
            }
            catch (UnauthorizedAccessException)
            {
                UnauthorizedAccess(path);
            }
            catch (DirectoryNotFoundException)
            {
                DirectoryNotFound(path);
            }
        }
        
        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                case Keys.Enter:
                    e.SuppressKeyPress = true;
                    ToggleMenuItem_Click(sender, e);
                    break;
            }
        }
        
        #endregion


        #region Context Menu Events

        private void RenameMenuItem_Click(object sender, EventArgs e)
        {
            TreeView.SelectedNode.EnsureVisible();
            TreeView.SelectedNode.BeginEdit();
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            var path = GetPath(node);
            var result = MessageBox.Show(
                string.Format(Resources.ConfirmDelete_Message, path),
                Resources.ConfirmDelete_Label,
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question, 
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Directory.Delete(path, true);
                    node.Remove();
                }
                catch (DirectoryNotFoundException)
                {
                    node.Remove();
                    DirectoryNotFound(path);
                }
                catch (UnauthorizedAccessException)
                {
                    UnauthorizedAccess(path);
                }
            }
        }
        
        private void NewFolderMenuItem_Click(object sender, EventArgs e)
        {
            var parentNode = TreeView.SelectedNode;
            var parentPath = GetPath(parentNode);
            parentNode.Expand();
            _controller.CreateDirectory(parentPath, "New Folder");

            // add a new node for the directory
            var node = parentNode.Nodes.Add("New Folder", "New Folder");
            node.EnsureVisible();
            TreeView.SelectedNode = node;
            node.BeginEdit();
        }

        private void ToggleMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            if (node == null)
                return;
            node.Toggle();
        }

        private void OpenInFileExplorerMenuItem_Click(object sender, EventArgs e)
        {
            var node = TreeView.SelectedNode;
            if (node == null)
                return;
            var path = GetPath(node);
            _controller.OpenInExplorer(path);
        }

        #endregion

    }
}
