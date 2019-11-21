using Learnings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ex.Libs;
using System.IO;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Ex.Utils;

namespace Ex {

public static class Program {
		
		public static string SourceFileDirectory([CallerFilePath] string callerPath = "[NO PATH]") {
			return callerPath.Substring(0, callerPath.Replace('\\', '/').LastIndexOf('/'));
		}

		public static Server server;
		public static Client admin;
		public static MainForm mainForm;
		
		/// <summary> The main entry point for the application. </summary>
		[STAThread] static void Main() {
			
			try {
#if DEBUG
				// Might be a bad idea long-term.
				// Saves me a ton of work syncing these files into unity as I change them though.
				// Still more visible than doing some weird VS build command hook.
				// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "D:/Development/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Core");
				CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "D:/Dev/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Core");
				// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "C:/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Core");
#endif
				
				SelfTest();

				StaticSetup();

				ActualProgram();
				
				// Console.Read();
				
			} catch (Exception e) {
				Console.WriteLine("Top level exception occurred.... Aborting, " + e.InfoString());
				Console.Read();
			}
		}

		static void StaticSetup() {

			JsonObject.DictionaryGenerator = () => new ConcurrentDictionary<JsonString, JsonValue>();

		}
		
		
		static void ActualProgram() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			mainForm = new MainForm();
			try {
				DBService.RegisterSerializers();
			} catch (Exception e) { Log.Error("Error", e); }
			// mainForm.FormClosed += (s, e) => { server.Stop(); };

			Console.WriteLine(Directory.GetCurrentDirectory());
			SetupLogger();
			SetupServer();
			server.Start();

			// Thread.Sleep(1000);

			SetupAdminClient();
			Console.WriteLine("Server started, showing window.");


			Application.Run(mainForm);

			Console.WriteLine("Window closed, Terminating server.");
			admin.server.Stop();
			server.Stop();

			// oof. figure out why we need this. sometimes (errors?)
			// Application.Exit();
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
			
			Action logstuff = () => {
				Log.Verbose("VERBOSE VERBOSE VERBOSE");
				Log.Debug("Debug. Debug.");
				Log.Info("Information.");
				Log.Warning("!!!!ATCHUNG!!!!");
				Log.Error("Oh Shi-");
			};
			logstuff();

			Log.Info("Color Test." +
				"\n\\qq\\ww\\ee\\rr\\tt\\yy\\uu\\ii\\oo\\pp" +
				"\n\\aa\\ss\\dd\\ff\\gg\\hh\\jj\\kk\\ll" +
				"\n\\zz\\xx\\cc\\vv\\bb\\nn\\mm");
		}

		private static void SetupServer() {
			server = new Server(32055, 100);

			server.AddService<DebugService>();
			server.AddService<LoginService>();
			
			server.AddService<EntityService>();
			server.AddService<MapService>();


			var sync = server.AddService<SyncService>();

			{
				var debugSync = sync.Context("debug");
				JsonObject data = new JsonObject();
				data["gameState"] = new JsonObject("gravity", 9.8f, "tickrate", 100);
				data["Test"] = new JsonObject("blah", "blarg", "Only", "Top", "Level", "Objects", "Get", "Syncd");
				data["Of"] = "Not an object, This doesn't get sync'd";
				data["Data"] = new JsonArray("Not", "an", "object,", "Neither", "does", "this");
				debugSync.SetData(data);
				debugSync.DefaultSubs("Test", "Data");

			}
			
			server.AddService<DBService>()
				.Connect()
				.UseDatabase("Test1")
				.CleanDatabase()
				.Reseed("../../../db")
				;
				



		}

		private static void SetupAdminClient() {
			TcpClient connection = new TcpClient("localhost", 32055);

			admin = new Client(connection);
			admin.AddService<DebugService>();
			admin.AddService<LoginService>();
			admin.AddService<EntityService>();
			admin.AddService<MapService>();
			var sync = admin.AddService<SyncService>();
			
			admin.ConnectSlave();
			admin.Call(Members<LoginService>.i.Login, "admin", "admin", VersionInfo.VERSION);
			
			// Subscribe to more stuff (Initial subscriptions would happen when connected to server)
			var context = sync.Context("debug");
			context.SubscribeTo("gameState");

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
