namespace Sink_20
{
   partial class Variables
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose( bool disposing )
      {
         if ( disposing && ( components != null ) )
         {
            components.Dispose();
         }
         base.Dispose( disposing );
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.pictureBox = new System.Windows.Forms.PictureBox();
         this.vScrollBar = new System.Windows.Forms.VScrollBar();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
         this.SuspendLayout();
         // 
         // pictureBox
         // 
         this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
         this.pictureBox.Location = new System.Drawing.Point(0, 0);
         this.pictureBox.Name = "pictureBox";
         this.pictureBox.Size = new System.Drawing.Size(284, 262);
         this.pictureBox.TabIndex = 0;
         this.pictureBox.TabStop = false;
         this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
         // 
         // vScrollBar
         // 
         this.vScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.vScrollBar.Location = new System.Drawing.Point(266, 0);
         this.vScrollBar.Name = "vScrollBar";
         this.vScrollBar.Size = new System.Drawing.Size(17, 262);
         this.vScrollBar.TabIndex = 1;
         this.vScrollBar.ValueChanged += new System.EventHandler(this.vScrollBar_ValueChanged);
         // 
         // Variables
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(284, 262);
         this.Controls.Add(this.vScrollBar);
         this.Controls.Add(this.pictureBox);
         this.HideOnClose = true;
         this.Name = "Variables";
         this.Text = "Variables";
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox;
      private System.Windows.Forms.VScrollBar vScrollBar;
   }
}