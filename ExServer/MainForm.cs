using Ex.Utils;
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
		
		private Cmdr commander;
		private ConcurrentQueue<RichTextBoxMessage> messageBacklog;

		public MainForm() {
			messageBacklog = new ConcurrentQueue<RichTextBoxMessage>();
			commander = new Cmdr();

			InitializeComponent();
			logTimer.Start();


			// Load theme from ini?
			Theme theme = new Theme();
			this.ForAll((ctrl) => { ctrl.ApplyTheme(theme); } );
			commandEntry.Select();
		}

		public void AddToLog(IEnumerable<RichTextBoxMessage> msgs) {
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

		private void SendCommand() {
			
			string command = commandEntry.Text;
			if (command != "") {
				commandEntry.Text = "";
				
				RichTextBoxMessage[] msg = new RichTextBoxMessage[1];
			
				msg[0].message = $"$>> {command}\n";
				msg[0].color = RTBExtensions.colorCodes['3'];
				AddToLog(msg);
			
				string result = commander.Execute(command);
				if (result == null) { result = "NULL"; }
			
				msg[0].message = $"$<< {result}\n\n";
				msg[0].color = RTBExtensions.colorCodes['4'];
				AddToLog(msg);

			}

		}


		private void SubmitButton_Click(object sender, EventArgs e) {

			SendCommand();
		}

		private void commandEntry_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Return) {
				SendCommand();
			}

			if (e.KeyCode == Keys.Up) {
				string previous = commander.PreviousCommand();
				if (previous != "") {
					commandEntry.Text = previous;
				}
			}

			if (e.KeyCode == Keys.Down) {
				string next = commander.NextCommand();
				commandEntry.Text = next;
			}

		}
	}
}
