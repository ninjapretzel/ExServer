using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ex {

	public partial class MainForm : Form {
		
		public static Theme theme;
		public static Color bgColor { get { return theme.bgColor; } }
		public static Color textColor { get { return theme.textColor; } }
		
		private ConcurrentQueue<RichTextBoxMessage> messageBacklog;

		public MainForm() {
			messageBacklog = new ConcurrentQueue<RichTextBoxMessage>();
			InitializeComponent();
			
			logTimer.Start();

			// Load theme from ini?
			Theme theme = new Theme();
			this.ForAll((ctrl) => { ctrl.ApplyTheme(theme); } );
			commandEntry.Select();
		}

		public void Log(IEnumerable<RichTextBoxMessage> msgs) {
			foreach (var msg in msgs) { messageBacklog.Enqueue(msg); }
		}



		private void LogTimer_Tick(object sender, EventArgs e) {
			if (!messageBacklog.IsEmpty) {
				RichTextBoxMessage msg;
				while (messageBacklog.TryDequeue(out msg)) {
					logTextBox.Append(msg);
				}
				logTextBox.ScrollToBottom();
			}
		}

		private void SubmitButton_Click(object sender, EventArgs e) {

		}
	}
}
