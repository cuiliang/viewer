﻿namespace Viewer.UI.Presentation
{
    partial class PresentationControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.NextButton = new System.Windows.Forms.Button();
            this.PrevButton = new System.Windows.Forms.Button();
            this.ControlPanel = new System.Windows.Forms.Panel();
            this.MaxDelayLabel = new System.Windows.Forms.Label();
            this.MinDelayLabel = new System.Windows.Forms.Label();
            this.SpeedTrackBar = new System.Windows.Forms.TrackBar();
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.ToggleFullscreenButton = new Viewer.UI.Forms.IconButton();
            this.ZoomInButton = new Viewer.UI.Forms.IconButton();
            this.ZoomOutButton = new Viewer.UI.Forms.IconButton();
            this.PlayPauseButton = new Viewer.UI.Forms.IconButton();
            this.TogglePlayToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ZoomOutToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ZoomInToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ToggleFullscreenToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ControlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // NextButton
            // 
            this.NextButton.BackColor = System.Drawing.Color.Transparent;
            this.NextButton.BackgroundImage = global::Viewer.Properties.Resources.RightArrowIcon;
            this.NextButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.NextButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.NextButton.FlatAppearance.BorderSize = 0;
            this.NextButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.NextButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.NextButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.NextButton.ForeColor = System.Drawing.Color.White;
            this.NextButton.Location = new System.Drawing.Point(540, 0);
            this.NextButton.Margin = new System.Windows.Forms.Padding(2);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(60, 406);
            this.NextButton.TabIndex = 3;
            this.NextButton.TabStop = false;
            this.NextButton.UseVisualStyleBackColor = false;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            this.NextButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.NextButton.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // PrevButton
            // 
            this.PrevButton.BackColor = System.Drawing.Color.Transparent;
            this.PrevButton.BackgroundImage = global::Viewer.Properties.Resources.LeftArrowIcon;
            this.PrevButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.PrevButton.Dock = System.Windows.Forms.DockStyle.Left;
            this.PrevButton.FlatAppearance.BorderSize = 0;
            this.PrevButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.PrevButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.PrevButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.PrevButton.ForeColor = System.Drawing.Color.White;
            this.PrevButton.Location = new System.Drawing.Point(0, 0);
            this.PrevButton.Margin = new System.Windows.Forms.Padding(2);
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(60, 406);
            this.PrevButton.TabIndex = 2;
            this.PrevButton.TabStop = false;
            this.PrevButton.UseVisualStyleBackColor = false;
            this.PrevButton.Click += new System.EventHandler(this.PrevButton_Click);
            this.PrevButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.PrevButton.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ControlPanel.Controls.Add(this.ToggleFullscreenButton);
            this.ControlPanel.Controls.Add(this.ZoomInButton);
            this.ControlPanel.Controls.Add(this.ZoomOutButton);
            this.ControlPanel.Controls.Add(this.PlayPauseButton);
            this.ControlPanel.Controls.Add(this.MaxDelayLabel);
            this.ControlPanel.Controls.Add(this.MinDelayLabel);
            this.ControlPanel.Controls.Add(this.SpeedTrackBar);
            this.ControlPanel.Location = new System.Drawing.Point(169, 363);
            this.ControlPanel.Margin = new System.Windows.Forms.Padding(2);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Size = new System.Drawing.Size(229, 41);
            this.ControlPanel.TabIndex = 4;
            this.ControlPanel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // MaxDelayLabel
            // 
            this.MaxDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MaxDelayLabel.AutoSize = true;
            this.MaxDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MaxDelayLabel.Location = new System.Drawing.Point(96, 23);
            this.MaxDelayLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.MaxDelayLabel.Name = "MaxDelayLabel";
            this.MaxDelayLabel.Size = new System.Drawing.Size(24, 13);
            this.MaxDelayLabel.TabIndex = 3;
            this.MaxDelayLabel.Text = "10s";
            this.MaxDelayLabel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // MinDelayLabel
            // 
            this.MinDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MinDelayLabel.AutoSize = true;
            this.MinDelayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.MinDelayLabel.Location = new System.Drawing.Point(2, 23);
            this.MinDelayLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.MinDelayLabel.Name = "MinDelayLabel";
            this.MinDelayLabel.Size = new System.Drawing.Size(18, 13);
            this.MinDelayLabel.TabIndex = 2;
            this.MinDelayLabel.Text = "1s";
            this.MinDelayLabel.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // SpeedTrackBar
            // 
            this.SpeedTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SpeedTrackBar.Location = new System.Drawing.Point(2, 2);
            this.SpeedTrackBar.Margin = new System.Windows.Forms.Padding(2);
            this.SpeedTrackBar.Minimum = 1;
            this.SpeedTrackBar.Name = "SpeedTrackBar";
            this.SpeedTrackBar.Size = new System.Drawing.Size(118, 45);
            this.SpeedTrackBar.TabIndex = 1;
            this.SpeedTrackBar.Value = 1;
            this.SpeedTrackBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.SpeedTrackBar.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            // 
            // UpdateTimer
            // 
            this.UpdateTimer.Enabled = true;
            this.UpdateTimer.Interval = 16;
            this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // ToggleFullscreenButton
            // 
            this.ToggleFullscreenButton.Icon = global::Viewer.Properties.Resources.Fullscreen;
            this.ToggleFullscreenButton.IconColor = System.Drawing.Color.Black;
            this.ToggleFullscreenButton.IconSize = new System.Drawing.Size(16, 16);
            this.ToggleFullscreenButton.Location = new System.Drawing.Point(200, 0);
            this.ToggleFullscreenButton.Name = "ToggleFullscreenButton";
            this.ToggleFullscreenButton.Size = new System.Drawing.Size(24, 43);
            this.ToggleFullscreenButton.TabIndex = 7;
            this.ToggleFullscreenButton.Text = "iconButton1";
            this.ToggleFullscreenToolTip.SetToolTip(this.ToggleFullscreenButton, "F5");
            this.ToggleFullscreenButton.Click += new System.EventHandler(this.ToggleFullscreenButton_Click);
            // 
            // ZoomInButton
            // 
            this.ZoomInButton.Icon = global::Viewer.Properties.Resources.ZoomIn;
            this.ZoomInButton.IconColor = System.Drawing.Color.Black;
            this.ZoomInButton.IconSize = new System.Drawing.Size(16, 16);
            this.ZoomInButton.Location = new System.Drawing.Point(176, 0);
            this.ZoomInButton.Name = "ZoomInButton";
            this.ZoomInButton.Size = new System.Drawing.Size(24, 43);
            this.ZoomInButton.TabIndex = 6;
            this.ZoomInButton.Text = "iconButton1";
            this.ZoomInToolTip.SetToolTip(this.ZoomInButton, "Ctrl + Mouse Wheel Up");
            this.ZoomInButton.Click += new System.EventHandler(this.ZoomInButton_Click);
            // 
            // ZoomOutButton
            // 
            this.ZoomOutButton.Icon = global::Viewer.Properties.Resources.ZoomOut;
            this.ZoomOutButton.IconColor = System.Drawing.Color.Black;
            this.ZoomOutButton.IconSize = new System.Drawing.Size(16, 16);
            this.ZoomOutButton.Location = new System.Drawing.Point(152, 0);
            this.ZoomOutButton.Name = "ZoomOutButton";
            this.ZoomOutButton.Size = new System.Drawing.Size(24, 43);
            this.ZoomOutButton.TabIndex = 5;
            this.ZoomOutButton.Text = "iconButton1";
            this.ZoomOutToolTip.SetToolTip(this.ZoomOutButton, "Ctrl + Mouse Wheel Down");
            this.ZoomOutButton.Click += new System.EventHandler(this.ZoomOutButton_Click);
            // 
            // PlayPauseButton
            // 
            this.PlayPauseButton.Icon = global::Viewer.Properties.Resources.Play;
            this.PlayPauseButton.IconColor = System.Drawing.Color.Black;
            this.PlayPauseButton.IconSize = new System.Drawing.Size(16, 16);
            this.PlayPauseButton.Location = new System.Drawing.Point(129, 0);
            this.PlayPauseButton.Name = "PlayPauseButton";
            this.PlayPauseButton.Size = new System.Drawing.Size(24, 43);
            this.PlayPauseButton.TabIndex = 4;
            this.PlayPauseButton.Text = "iconButton1";
            this.TogglePlayToolTip.SetToolTip(this.PlayPauseButton, "P");
            this.PlayPauseButton.Click += new System.EventHandler(this.PlayPauseButton_Click);
            // 
            // TogglePlayToolTip
            // 
            this.TogglePlayToolTip.ToolTipTitle = "Toggle Play";
            // 
            // ZoomOutToolTip
            // 
            this.ZoomOutToolTip.ToolTipTitle = "Zoom Out";
            // 
            // ZoomInToolTip
            // 
            this.ZoomInToolTip.ToolTipTitle = "Zoom In";
            // 
            // ToggleFullscreenToolTip
            // 
            this.ToggleFullscreenToolTip.ToolTipTitle = "Toggle Fullscreen";
            // 
            // PresentationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.ControlPanel);
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.PrevButton);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "PresentationControl";
            this.Size = new System.Drawing.Size(600, 406);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PresentationControl_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseDown);
            this.MouseLeave += new System.EventHandler(this.PresentationControl_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseUp);
            this.Resize += new System.EventHandler(this.PresentationControl_Resize);
            this.ControlPanel.ResumeLayout(false);
            this.ControlPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpeedTrackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button PrevButton;
        private System.Windows.Forms.Panel ControlPanel;
        private System.Windows.Forms.TrackBar SpeedTrackBar;
        private System.Windows.Forms.Label MinDelayLabel;
        private System.Windows.Forms.Label MaxDelayLabel;
        private System.Windows.Forms.Timer UpdateTimer;
        private Forms.IconButton PlayPauseButton;
        private Forms.IconButton ZoomOutButton;
        private Forms.IconButton ZoomInButton;
        private Forms.IconButton ToggleFullscreenButton;
        private System.Windows.Forms.ToolTip ToggleFullscreenToolTip;
        private System.Windows.Forms.ToolTip ZoomInToolTip;
        private System.Windows.Forms.ToolTip ZoomOutToolTip;
        private System.Windows.Forms.ToolTip TogglePlayToolTip;
    }
}