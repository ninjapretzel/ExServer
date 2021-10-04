
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using MiniHttp;
using Ex.Utils;
using Ex.Utils.Ext;
using Ex.Libs;

using static MiniHttp.ProvidedMiddleware;
using BakaDB;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace Ex {

	
	public static class Program {

		/// <summary> Class to hold tests-level to be run after all other tests, 
		/// for console visibility </summary>
		public class zzz_RunMeLast_Tests { 
			public static void Test() {
				
			}
		}

		public static string SourceFileDirectory([CallerFilePath] string callerPath = "[NO PATH]") {
			return callerPath.Substring(0, callerPath.Replace('\\', '/').LastIndexOf('/'));
		}

		public static string TopSourceFileDirectory() { return SourceFileDirectory(); }

		public static string platform;
		public static Server server;
		public static Client admin;
		public static JsonObject global = new JsonObject();
		public static JsonObject config;
		public static Task<int> httpTask = null;
		private static bool running = true;

		[STAThread]
		/// <summary> The main entry point for the application. </summary>
		static void Main(string[] args) {
			bool stopAfterTests = false;
			bool copySources = true;
			if (args.Length > 0) {
				if (args[0] == "--test" || args[0] == "test" || args[0] == "-t") {
					stopAfterTests = true;
					copySources = false;
				}
				if (args[0] == "--noCopy" || args[0] == "noCopy" || args[0] == "-nc") {
					copySources = false;
				}
			}

			Console.Clear();
			// This is disgusting, but the only way I can be sure any `\r\n` get replaced with `\n`.
			Macros.FixSourceFiles(SourceFileDirectory());
			Config();

			try {
#if DEBUG
				// Might be a bad idea long-term.
				// Saves me a ton of work syncing these files into unity as I change them though.
				// Still more visible than doing some weird VS build command hook.
				try {
					if (copySources) {
						Macros.CopySourceFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "D:/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Core");
						Macros.CopySourceFiles((SourceFileDirectory() + "/Game/Shared").Replace('\\', '/'), "D:/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Game/Shared");
					}
					
					//Macros.CopySourceFiles((SourceFileDirectory() + "/Core").Replace('\\', '/'), "/media/d/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Core");
					//Macros.CopySourceFiles((SourceFileDirectory() + "/Game/Shared").Replace('\\', '/'), "/media/d/Development/Unity/Infinigrinder/Assets/Plugins/ExClient/Game/Shared");
				} catch (Exception) {
					Console.WriteLine("Copying source files failed.");
				}
#endif
				SetupLogger();
				StaticSetup();

				//Self Test:
				BakaTest.BakaTestHook.logger = (str) => { Log.Info(str, "Tests"); };
				BakaTest.BakaTestHook.RunTestsSync();
				if (stopAfterTests) { return; }

				ActualProgram();



				// Console.Read();

			} catch (Exception e) {
				Console.WriteLine("Top level exception occurred.... Aborting, " + e.InfoString());
				//Console.Read();
			}
			running = false;
		}

		private static void Config() {
			platform = System.Environment.OSVersion.Platform.ToString();
			
			global["platform"] = platform;
			Console.WriteLine($"Platform detected: ({platform})");
			Console.WriteLine($"Is mono platform? {Unsafe.MonoRuntime}");
			config = Json.Parse<JsonObject>(File.ReadAllText("./config.wtf"));
			
			if (File.Exists("./sensitive.wtf")) {
				JsonObject sensitive = Json.Parse<JsonObject>(File.ReadAllText("./sensitive.wtf"));
				config.SetRecursively(sensitive);
			}

			if (config.Has(platform)) {
				config = config.CombineRecursively(config.Get<JsonObject>(platform));
			}

			if (config.Has("artbreeder")) {
				ArtbreederAPI.cookie = config["artbreeder"]["cookie"].stringVal;
				Console.WriteLine("artbreeder cookie " + ArtbreederAPI.cookie);
			}
		}

		static void StaticSetup() {
			//JsonObject.DictionaryGenerator = () => new ConcurrentDictionary<JsonString, JsonValue>();
			VectorJsonConverter.RegisterSerializers();
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

			Log.Info($"Working Directory: {Directory.GetCurrentDirectory().Replace('\\', '/') }");
			SetupExServer();
			server.Start();

			// Thread.Sleep(1000);

			// SetupAdminClient();
			DB.Drop<LoginService.UserLoginInfo>();

			string str = @"
+===========================================================================+
|                              SERVER STARTED                               |
|                     Press \rENTER \wat any time to exit                       |
+===========================================================================+";
			Log.Info(str);

			Request.onError += (msg, err) => {
				Log.Warning(msg, err);
			};


			// ArtbreederAPI.GenerateNew("anime_portraits");
			//ArtbreederAPI.GenerateNew("landscapes_sg2");
			// ArtbreederAPI.GenerateChild("anime_portraits", "b80cf128f557327cd5a5");
			//ArtbreederAPI.GenerateChild("landscapes_sg2", "ee8e488f375b0101510f");
			//ArtbreederAPI.Breed("anime_portraits", "b80cf128f557327cd5a5", "1eb84726c8a532c763e3", .3f, .6f, .7f, .6f, .7f);
			//ArtbreederAPI.GenerateChild("general", "56f1ddc716ac238b5ce6", .7, .4, .7);
			//ArtbreederAPI.GenerateChild("general", "56f1ddc716ac238b5ce6", .7, .4, .7);
			//ArtbreederAPI.GenerateChild("general", "61efecf69922913b499f", .7, .4, .7);



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

		private static void SetupHttpServer(Server server) {
			List<Middleware> middleware = new List<Middleware>();
			//middleware.Add(Inspect);
			middleware.Add(BodyParser);
			Router router = new Router();
			router.Any("/api/auth/*", server.GetService<LoginService>().router);
			router.Any("/ws/ex", async (ctx, next) => {
				await ctx.OpenWebSocket("ex");
				WebSocket wsock = ctx.webSocket;
				server.AcceptWebSocket(wsock);
			});
			router.Any("/ws/test", async (ctx, next) => {
				await ctx.OpenWebSocket("test");
				WebSocket wsock = ctx.webSocket;
				byte[] data = new byte[1024];
				var seg = new ArraySegment<byte>(data);
				var stupid = CancellationToken.None;
				
				try {
					while (wsock.State == WebSocketState.Open) {
						var result = await wsock.ReceiveAsync(seg, stupid);
						string str = Encoding.UTF8.GetString(data, 0, result.Count);

						if (result.MessageType == WebSocketMessageType.Close) {
							Log.Info($"Web socket closing.");
							await wsock.CloseAsync(WebSocketCloseStatus.NormalClosure, "", stupid);
						} else {
							Log.Info($"Got message {str}");
							await wsock.SendAsync(new ArraySegment<byte>(data, 0, result.Count), WebSocketMessageType.Text, result.EndOfMessage, stupid);
						}
					}
				} catch (Exception e) {
					Log.Error("Websocket error", e);
				} finally {
					wsock.Dispose();
				}
			});
			middleware.Add(router);

			middleware.Add(Static("./public"));
			


			if (config.Has<JsonArray>("httpHost")) {
				string[] hostnames = config.Get<string[]>("httpHost");
				Console.WriteLine($"HTTP Listening at: ");
				foreach (var host in hostnames) {
					Console.WriteLine($"\t{host}");
				}
				httpTask = HttpServer.Watch(hostnames, ()=>running, 1000, middleware.ToArray());

			} else {
				string hostname = config["httpHost"].stringVal;

				httpTask = HttpServer.Watch(hostname, ()=>running, middleware.ToArray());

				Console.WriteLine($"HTTP Listening at {hostname}");
			}

		}

		private static void SetupExServer() {
			server = new Server(32055, 100);

			// server.AddService<Poly.PolyGame>();
			server.AddService<Infinigrinder.Game>();
			server.AddService<DebugService>();
			server.AddService<LoginService>();
			server.AddService<EntityService>();
			server.AddService<MapService>();

			// JsonObject cfg = config["database"] as JsonObject;
			//var dbService = server.AddService<DBService>()
			//	.Connect(cfg["host"].stringVal)
			//	.UseDatabase(cfg["name"].stringVal);
			//
			//if (cfg.Has<JsonString>("reload")) {
			//	dbService.CleanDatabase();
			//}
			//if (cfg.Has<JsonString>("reseed")) {
			//	dbService.Reseed(cfg["reseed"].stringVal);
			//}

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


			if (config.Has<JsonString>("httpHost") || config.Has<JsonArray>("httpHost")) { SetupHttpServer(server); }
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


	}

}
