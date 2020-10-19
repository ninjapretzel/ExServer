using Learnings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using System.Windows.Forms;
using Ex.Libs;
using System.IO;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using Ex.Utils;
using System.Diagnostics;
using MiniHttp;

namespace Ex {

	
	public static class Program {

		public static string SourceFileDirectory([CallerFilePath] string callerPath = "[NO PATH]") {
			return callerPath.Substring(0, callerPath.Replace('\\', '/').LastIndexOf('/'));
		}

		public static string TopSourceFileDirectory() { return SourceFileDirectory(); }

		public static Server server;
		public static Client admin;
		public static JsonObject global = new JsonObject();
		public static JsonObject config;
		public static Task<int> httpTask = null;
		private static bool running = true;
		
		[STAThread]
		/// <summary> The main entry point for the application. </summary>
		static void Main() {
			Console.Clear();
		
			string platform = System.Environment.OSVersion.Platform.ToString();
			global["platform"] = platform;
			Console.WriteLine($"Platform detected: ({platform})");
			Console.WriteLine($"Is mono platform? {Unsafe.MonoRuntime}");
			config = Json.Parse<JsonObject>(File.ReadAllText("./config.wtf"));
			if (config.Has(platform)) {
				config = config.CombineRecursively(config.Get<JsonObject>(platform));
			}
			
			if (config.Has<JsonString>("httpHost")) {
				Middleware MakeTest(int i) {
					return async (ctx, next) => {
						Console.WriteLine($"From Before {i}");
						await next();
						Console.WriteLine($"From After {i}");
					};
				}

				string hostname = config["httpHost"].stringVal;
				string[] prefixes = new string[] { hostname };
				List<Middleware> middleware = new List<Middleware>();
				middleware.Add(ProvidedMiddleware.BodyParser);
				// for (int i = 0; i < 10; i++) { middleware.Add(MakeTest(i)); }
				middleware.Add( async(ctx, next) => {
					ctx.body = "Aww yeet";
					Console.WriteLine($"Raw body: {ctx.req.body}");
					Console.WriteLine($"Object: {ctx.req.bodyObj?.ToString()}");
					Console.WriteLine($"Array: {ctx.req.bodyArr?.ToString()}");
					
				});

				httpTask = HttpServer.Watch(prefixes, ()=>running, 500, middleware.ToArray() );
				Console.WriteLine($"HTTP Listening at {hostname}");
			}

			try {
#if DEBUG
				// Might be a bad idea long-term.
				// Saves me a ton of work syncing these files into unity as I change them though.
				// Still more visible than doing some weird VS build command hook.
				try {
					// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "D:/Development/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Core");
					// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Game/Shared").Replace('\\', '/'), "D:/Development/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Game/Shared");

					// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "D:/Dev/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Core");
					// CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Game/Shared").Replace('\\', '/'), "D:/Dev/Unity/Projects/Infinigrinder/Assets/Plugins/ExClient/Game/Shared");

					CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "C:/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Core");
					CopySourceMacro.CopyAllFiles((SourceFileDirectory() + "/Game/Shared").Replace('\\', '/'), "C:/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Game/Shared");
				} catch (Exception) {
					Console.WriteLine("Copying source files failed.");
				}
#endif	
				SetupLogger();
				StaticSetup();
				
				SelfTest();
				ActualProgram();
				
				
				
				// Console.Read();

			} catch (Exception e) {
				Console.WriteLine("Top level exception occurred.... Aborting, " + e.InfoString());
				//Console.Read();
			}
			running = false;
		}

		static void StaticSetup() {

			//JsonObject.DictionaryGenerator = () => new ConcurrentDictionary<JsonString, JsonValue>();
			try {
				DBService.RegisterSerializers();
			} catch (Exception e) { Log.Error("Error registering DB Serializers", e); }

		}
		
		
		static void ActualProgram() {
			// Application.EnableVisualStyles();
			// Application.SetCompatibleTextRenderingDefault(false);
			// mainForm = new MainForm();

			Action logStuff = () => {
				Log.Verbose("VERBOSE VERBOSE VERBOSE");
				Log.Debug("Debug. Debug.");
				Log.Info("Information.");
				Log.Warning("!!!!ATCHUNG!!!!");
				Log.Error("Oh Shi-");
			};
			// logStuff();

			Action logColors = () => {
				Log.Info("Color Test." +
					"\n\\qq\\ww\\ee\\rr\\tt\\yy\\uu\\ii\\oo\\pp"
					+ "\n\\aa\\ss\\dd\\ff\\gg\\hh\\jj\\kk\\ll"
					+ "\n\\zz\\xx\\cc\\vv\\bb\\nn\\mm"
					+ "\n\\1\\2\\3\\4\\5\\6\\7\\8\\9\\0");

			};

			logStuff();
			logColors();
			// mainForm.FormClosed += (s, e) => { server.Stop(); };

			Log.Info($"Working Directory: Directory.GetCurrentDirectory()");
			SetupServer();
			server.Start();

			// Thread.Sleep(1000);

			// SetupAdminClient();
			string str = @"
+===========================================================================+
|                              SERVER STARTED                               |
|                     Press \rENTER \wat any time to exit                       |
+===========================================================================+";
			Log.Info(str);
			
			
			Console.ReadLine();
			
			


			Console.WriteLine("\n\n\nTerminating server.\n\n\n");
			//admin.server.Stop();
			server.Stop();
			Log.Stop();
			// oof. figure out why we need this. sometimes (errors?)
			// Application.Exit();
		}
		
		private static void SetupLogger() {
			Log.ignorePath = SourceFileDirectory();
			Log.fromPath = "ExServer";
			Log.defaultTag = "Ex";
			LogLevel target = Enum.Parse<LogLevel>(config["logLevel"].stringVal);
			
			// Write all info and more severe to textfield 
			/* Log.logHandler += (info) => {
				if (info.level <= LogLevel.Info && mainForm != null) {
					string msg = info.message;
					var msgs = msg.ToString().Rich();
					msgs.Add(new RichTextBoxMessage("\n"));
					mainForm.AddToLog(msgs);
				}
			}; */
				
			Log.logHandler += (info) => {
				// Console.WriteLine($"{info.tag}: {info.message}");
				if (info.level <= target) {
					Pretty.Print($"\n{info.tag}: {info.message}\n");
					
					//Pretty.Print(Pretty.Code(0, 4));
				}
			};
			
			// Todo: Change logfile location when deployed
			// Log ALL messages to file.
			string logfolder = $"{SourceFileDirectory()}/logs";
			if (!Directory.Exists(logfolder)) { Directory.CreateDirectory(logfolder); }
			string logfile = $"{logfolder}/{DateTime.UtcNow.UnixTimestamp()}.log";
			Log.logHandler += (info) => {
				File.AppendAllText(logfile, $"\n{info.tag}: {info.message}\n");
			};
			

		}

		private static void SetupServer() {
			server = new Server(32055, 100);

			// server.AddService<Poly.PolyGame>();
			server.AddService<Eternus.EternusGame>();
			server.AddService<DebugService>();

			JsonObject cfg = config["database"] as JsonObject;
			var dbService = server.AddService<DBService>()
				.Connect(cfg["host"].stringVal)
				.UseDatabase(cfg["name"].stringVal);

			if (cfg.Has<JsonString>("reload")) {
				dbService.CleanDatabase().Reseed(cfg["reload"].stringVal);
			}

			var sync = server.AddService<SyncService>(); {
				var debugSync = sync.Context("debug");
				JsonObject data = new JsonObject();
				data["gameState"] = new JsonObject("gravity", 9.8f, "tickrate", 100);
				data["Test"] = new JsonObject("blah", "blarg", "Only", "Top", "Level", "Objects", "Get", "Syncd");
				data["Of"] = "Not an object, This doesn't get sync'd";
				data["Data"] = new JsonArray("Not", "an", "object,", "Neither", "does", "this");
				debugSync.SetData(data);
				debugSync.DefaultSubs("Test", "Data");
			}

			server.AddService<LoginService>();
			server.AddService<EntityService>();
			server.AddService<MapService>();
				
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
/*
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

*/
	}

}
