﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    public partial class TaskLoaderView : WindowView
    {
        public IProgressController Progress => _controller;

        /// <summary>
        /// Name of the task
        /// </summary>
        public string OperationName { get; set; }

        private readonly ProgressController _controller;
        private CancellationTokenSource _cancellation;

        private class ProgressController : IProgressController
        {
            /// <summary>
            /// Name of a task which is currently loading
            /// </summary>
            public string Message;

            /// <summary>
            /// Number of finished tasks
            /// </summary>
            public int FinishedCount;

            private readonly TaskLoaderView _view;

            public ProgressController(TaskLoaderView view)
            {
                _view = view;
            }

            public void Report(ILoadingProgress value)
            {
                Message = value.Message;
                Interlocked.Increment(ref FinishedCount);
            }

            public void Close()
            {
                _view._cancellation?.Dispose();
                _view._cancellation = null;
                _view.Close();
            }
        }

        public TaskLoaderView(int totalTaskCount, CancellationTokenSource cancellation)
        {
            InitializeComponent();
            
            _controller = new ProgressController(this);
            _cancellation = cancellation;

            TaskProgressBar.Minimum = 0;
            TaskProgressBar.Maximum = totalTaskCount;
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            // read current state
            var name = _controller.Message;
            var finishedCount = _controller.FinishedCount;

            // update the view
            if (finishedCount > TaskProgressBar.Maximum)
            {
                // This is an invalid state but, as we are dealing with an asyncrhonous operation,
                // it could happend that the total number of tasks has increased.
                TaskProgressBar.Maximum = finishedCount;
            }
            
            var progress = (int) (TaskProgressBar.Value / (double) TaskProgressBar.Maximum * 100);
            TaskProgressBar.Value = finishedCount;
            TaskNameLabel.Text = name;
            ProgressLabel.Text = string.Format(Resources.Progress_Label, progress);

            // update the title
            Text = string.Format(Resources.Progess_Title, OperationName, progress);
        }

        private void TaskLoaderView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                _cancellation?.Cancel();
            }
        }

        private void CancelTaskButton_Click(object sender, EventArgs e)
        {
            _cancellation?.Cancel();
            Close();
        }
    }
}
