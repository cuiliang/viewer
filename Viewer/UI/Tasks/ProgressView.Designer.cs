﻿using System.Windows.Forms;

namespace Viewer.UI.Tasks
{
    partial class ProgressView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Progress = new System.Windows.Forms.ProgressBar();
            this.CurrentTaskNameLabel = new System.Windows.Forms.Label();
            this.CancelProgressButton = new System.Windows.Forms.Button();
            this.ProgressLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Progress
            // 
            this.Progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.Progress.Location = new System.Drawing.Point(12, 35);
            this.Progress.Name = "Progress";
            this.Progress.Size = new System.Drawing.Size(417, 25);
            this.Progress.TabIndex = 0;
            // 
            // CurrentTaskNameLabel
            // 
            this.CurrentTaskNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CurrentTaskNameLabel.AutoSize = true;
            this.CurrentTaskNameLabel.Location = new System.Drawing.Point(9, 63);
            this.CurrentTaskNameLabel.Name = "CurrentTaskNameLabel";
            this.CurrentTaskNameLabel.Size = new System.Drawing.Size(101, 17);
            this.CurrentTaskNameLabel.TabIndex = 1;
            this.CurrentTaskNameLabel.Text = "Current task ...";
            // 
            // CancelProgressButton
            // 
            this.CancelProgressButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.CancelProgressButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelProgressButton.Location = new System.Drawing.Point(435, 35);
            this.CancelProgressButton.Name = "CancelProgressButton";
            this.CancelProgressButton.Size = new System.Drawing.Size(62, 25);
            this.CancelProgressButton.TabIndex = 2;
            this.CancelProgressButton.Text = "Cancel";
            this.CancelProgressButton.UseVisualStyleBackColor = true;
            this.CancelProgressButton.Click += new System.EventHandler(this.CancelProgressButton_Click);
            // 
            // ProgressLabel
            // 
            this.ProgressLabel.AutoSize = true;
            this.ProgressLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.ProgressLabel.Location = new System.Drawing.Point(8, 12);
            this.ProgressLabel.Name = "ProgressLabel";
            this.ProgressLabel.Size = new System.Drawing.Size(120, 20);
            this.ProgressLabel.TabIndex = 3;
            this.ProgressLabel.Text = "0 % completed";
            // 
            // ProgressView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ProgressLabel);
            this.Controls.Add(this.CancelProgressButton);
            this.Controls.Add(this.CurrentTaskNameLabel);
            this.Controls.Add(this.Progress);
            this.MinimumSize = new System.Drawing.Size(500, 100);
            this.Name = "ProgressView";
            this.Size = new System.Drawing.Size(500, 100);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button CancelButton;
        private System.Windows.Forms.ProgressBar Progress;
        private System.Windows.Forms.Label CurrentTaskNameLabel;
        private System.Windows.Forms.Button CancelProgressButton;
        private Label ProgressLabel;
    }
}