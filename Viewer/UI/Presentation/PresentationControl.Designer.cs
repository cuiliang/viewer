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
            this.NextButton = new System.Windows.Forms.Button();
            this.PrevButton = new System.Windows.Forms.Button();
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
            this.NextButton.Location = new System.Drawing.Point(651, 0);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(60, 434);
            this.NextButton.TabIndex = 3;
            this.NextButton.TabStop = false;
            this.NextButton.UseVisualStyleBackColor = false;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            this.NextButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
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
            this.PrevButton.Name = "PrevButton";
            this.PrevButton.Size = new System.Drawing.Size(60, 434);
            this.PrevButton.TabIndex = 2;
            this.PrevButton.TabStop = false;
            this.PrevButton.UseVisualStyleBackColor = false;
            this.PrevButton.Click += new System.EventHandler(this.PrevButton_Click);
            this.PrevButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            // 
            // PresentationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.PrevButton);
            this.DoubleBuffered = true;
            this.Name = "PresentationControl";
            this.Size = new System.Drawing.Size(711, 434);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PresentationControl_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresentationControl_KeyDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PresentationControl_MouseMove);
            this.Resize += new System.EventHandler(this.PresentationControl_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.Button PrevButton;
    }
}
