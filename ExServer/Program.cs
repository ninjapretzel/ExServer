using Learnings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ex.Libs;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ex {

	public static class Program {
		public static string SourceFileDirectory([CallerFilePath] string callerPath = "[NO PATH]") {
			return callerPath.Substring(0, callerPath.Replace('\\', '/').LastIndexOf('/'));
		}

		public static Server server;
		public static MainForm mainForm;

		public static CompSys<Server> context;

		/// <summary> The main entry point for the application. </summary>
		[STAThread] static void Main() {
			try {
				#if DEBUG
				// Might be a bad idea long-term.
				// Saves me a ton of work syncing these files into unity as I change them though.
				CopySourceMacro.CopyAllFiles((SourceFileDirectory()+"/Core").Replace('\\', '/'), "D:/Development/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Core");
				#endif
				SelfTest();

				ActualProgram();

			} catch (Exception e) {
				Console.WriteLine("Top level exception occurred.... Aborting, " + e.InfoString());
				Console.Read();
			}
		}


		static void ActualProgram() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			mainForm = new MainForm();
			// mainForm.FormClosed += (s, e) => { server.Stop(); };

			Console.WriteLine(Directory.GetCurrentDirectory());
			SetupLogger();
			SetupServer();

			server.Start();
			Console.WriteLine("Server started, showing window.");


			Application.Run(mainForm);

			Console.WriteLine("Window closed, Terminating server.");
			server.Stop();
		}

		private static void SetupLogger() {
			Log.ignorePath = SourceFileDirectory();
			Log.fromPath = "ExServer";
			
			Log.logHandler += (tag, msg) => {
				var msgs = msg.ToString().Rich();
				msgs.Add(new RichTextBoxMessage("\n"));
				mainForm.Log(msgs);
			};
			Log.logHandler += (tag, msg) => {
				Console.WriteLine($"{tag}: {msg}");
			};
			//Log.Verbose("VERBOSE VERBOSE VERBOSE");
			//Log.Debug("Debug. Debug.");
			//Log.Info("Information.");
			//Log.Warning("!!!!ATCHUNG!!!!");
			//Log.Error("Oh Shi-");

			Log.Info("Color Test." +
				"\n\\qq\\ww\\ee\\rr\\tt\\yy\\uu\\ii\\oo\\pp\\aa\\ss\\dd" +
				"\n\\ff\\gg\\hh\\jj\\kk\\ll\\zz\\xx\\cc\\vv\\bb\\nn\\mm");
		}

		private static void SetupServer() {
			server = new Server();

			server.AddService<DebugService>();

		}

		static void SelfTest() {

			BakaTest.BakaTestHook.RunTests();
			
			
		}

		private static void TestColors(string testStr) {
			var msgs = testStr.Rich();

			Console.WriteLine($"Input: [{testStr}]");

			foreach (var msg in msgs) {
				string str = msg.color.HasValue
					? $"{msg.color.Value.HexString()} => {msg.message}"
					: msg.message;

				Console.WriteLine($"\t{str}");
			}
		}
	}

}

