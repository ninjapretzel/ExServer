namespace Ex {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.logTextBox = new System.Windows.Forms.RichTextBox();
			this.commandEntry = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.logTimer = new System.Windows.Forms.Timer(this.components);
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusGroup = new System.Windows.Forms.GroupBox();
			this.controlsGroup = new System.Windows.Forms.GroupBox();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(401, 415);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 16);
			this.label1.TabIndex = 0;
			this.label1.Text = "$ >";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(401, 28);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(25, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Log";
			// 
			// logTextBox
			// 
			this.logTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.logTextBox.Location = new System.Drawing.Point(404, 44);
			this.logTextBox.Name = "logTextBox";
			this.logTextBox.ReadOnly = true;
			this.logTextBox.Size = new System.Drawing.Size(765, 365);
			this.logTextBox.TabIndex = 2;
			this.logTextBox.Text = "";
			// 
			// commandEntry
			// 
			this.commandEntry.Location = new System.Drawing.Point(436, 415);
			this.commandEntry.Name = "commandEntry";
			this.commandEntry.Size = new System.Drawing.Size(678, 20);
			this.commandEntry.TabIndex = 3;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(1120, 415);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(49, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "Submit";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.SubmitButton_Click);
			// 
			// logTimer
			// 
			this.logTimer.Tick += new System.EventHandler(this.LogTimer_Tick);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.toolsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(1181, 24);
			this.menuStrip1.TabIndex = 5;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "File";
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "Edit";
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
			this.toolsToolStripMenuItem.Text = "Tools";
			// 
			// statusGroup
			// 
			this.statusGroup.Location = new System.Drawing.Point(13, 28);
			this.statusGroup.Name = "statusGroup";
			this.statusGroup.Size = new System.Drawing.Size(382, 140);
			this.statusGroup.TabIndex = 6;
			this.statusGroup.TabStop = false;
			this.statusGroup.Text = "Status";
			// 
			// controlsGroup
			// 
			this.controlsGroup.Location = new System.Drawing.Point(13, 175);
			this.controlsGroup.Name = "controlsGroup";
			this.controlsGroup.Size = new System.Drawing.Size(382, 263);
			this.controlsGroup.TabIndex = 7;
			this.controlsGroup.TabStop = false;
			this.controlsGroup.Text = "Controls";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1181, 450);
			this.Controls.Add(this.controlsGroup);
			this.Controls.Add(this.statusGroup);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.commandEntry);
			this.Controls.Add(this.logTextBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "ExServer";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RichTextBox logTextBox;
		private System.Windows.Forms.TextBox commandEntry;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Timer logTimer;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.GroupBox statusGroup;
		private System.Windows.Forms.GroupBox controlsGroup;
	}
}

