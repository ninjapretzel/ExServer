
#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
using UnityEngine;
#else

// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using Ex;
using Ex.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Ex.EntityService;

// TODO: Come back at some point and figure out why these tests cause stack overflows...

public static class Server_Tests {

	private class TestData {
		public Server server { get; private set; }
		public Client admin { get; private set; }
		public TestData(Server server, Client admin) {
			this.server = server;
			this.admin = admin;
		}

	}
	private class TestUserEntityInfo : DefaultUserEntityInfo { }

	private static TestData DefaultSetup(params Type[] clientServices) {
		return Setup("Testing", "db", 12345, 50, clientServices);
	}
	private static TestData Setup(string testDbName = "Testing", string testDb = "db", int port = 12345, float tick = 50, Type[] clientServices = null) {
		Server server = new Server(port, tick);
		var debug = server.AddService<DebugService>();
		var login = server.AddService<LoginService>();
		var entity = server.AddService<EntityService>();
		var map = server.AddService<MapService>();
		var db = server.AddService<DBService>()
			.Connect()
			.UseDatabase(testDbName)
			.CleanDatabase()
			;

		// Note: A data type for holding user entity info must be registered.
		// This effectively saves the user's information of where they are,
		// and typically should be customized to either store all of a user's primary information, or links to them.
		entity.RegisterUserEntityInfo<TestUserEntityInfo>();

		// Note: Next, some logic should be provided so that when a user logs in for the first time,
		// their data can be initialized to some default state.
		login.userInitializer = (guid) => {
			// In here, we can initialize anything the user needs.
			// One of those things should be whatever was registered with `RegisterUserEntityInfo`.
			var info = db.Initialize<TestUserEntityInfo>(guid, it => {
				it.map = "HelloWorld";
				it.position = Vector3.zero;
				it.rotation = Vector3.zero;
				it.skin = "Yeet";
				it.color = "0xFFFFFFFF";
			});
		};


		var serverSync = server.AddService<SyncService>();
		{
			var debugSync = serverSync.Context("debug");
			JsonObject data = new JsonObject();
			data["gameState"] = new JsonObject("gravity", 9.8f, "tickrate", 100);
			data["Test"] = new JsonObject("blah", "blarg", "Only", "Top", "Level", "Objects", "Get", "Syncd");
			data["Of"] = "Not an object, This doesn't get sync'd";
			data["Data"] = new JsonArray("Not", "an", "object,", "Neither", "does", "this");
			debugSync.SetData(data);
			debugSync.DefaultSubs("Test", "Data");

		}

		server.Start();
		Thread.Sleep(50);

		Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		sock.Connect("127.0.0.1", port);
		Client admin = new Client(sock);
		// Client admin = new Client(new TcpClient("localhost", port));
		admin.AddService<DebugService>();
		admin.AddService<LoginService>();
		admin.AddService<EntityService>();
		admin.AddService<MapService>();
		var adminSync = admin.AddService<SyncService>();
		if (clientServices != null) {
			object[] EMPTY = new object[0];
			Func<Service> addService = admin.AddService<DebugService>;
			// addService =
			foreach (var serviceType in clientServices) {
				addService.Method.GetGenericMethodDefinition().MakeGenericMethod(new Type[] { serviceType }).Invoke(admin, EMPTY);
			}
		}
		
		try {
			admin.ConnectSlave();
			// Internally, the above does something like the following:
			// Sending network messages, you can either use the Members<> template to access the member method you want to call...
			// admin.Call(Members<LoginService>.i.Login, "admin", "admin", VersionInfo.VERSION);

			// Or if you have an instance, you can use that to grab the member function out of there.
			admin.Call(debug.PingN, 5);
			// Only the name of the function and the service class it is defined in are actually used.

			adminSync.Context("debug").SubscribeTo("gameState");
			
		} catch (Exception e) {
			
			Log.Error("Error starting test server", e);
		
		}
		
		return new TestData(server, admin);
	}

	private static void CleanUp(TestData data) {
		Log.Info("Cleaning Up NOW.");
		data.admin.server.Stop();
		data.server.Stop();
	}

	/// <summary> Waits up to <paramref name="maxWaitMs"/> for the given bool-returning 
	/// <paramref name="sampler"/> function to return a true value. </summary>
	/// <param name="sampler"> Function to sample for `true`</param>
	/// <param name="maxWaitMs"> maximum time to wait, in milliseconds. </param>
	/// <returns> True, if the sampler returned true to stop the wait, false otherwise. </returns>
	public static bool WaitFor(Func<bool> sampler, int maxWaitMs = 1000) {
		DateTime start = DateTime.UtcNow;
		
		while (!sampler()) {
			DateTime now = DateTime.UtcNow;
			if ((now-start).TotalMilliseconds > maxWaitMs) {
				return false;
			}
			Thread.Sleep(1);
		}
		return true;
	}

	public static void TestSetup() {
		if (Environment.OSVersion.Platform.ToString() != "Win32NT") { return; }

		TestService tester = new TestService();
		var testData = DefaultSetup(typeof(TestService));
		// defer CleanUp(testData);
		var testService = testData.admin.GetService<TestService>();
		var loginService = testData.admin.GetService<LoginService>();
		var sync = testData.admin.GetService<SyncService>();
		try {

			// Logging in lights up lots of code paths. Need to wait ~2 seconds for it to finish.
			if (!WaitFor(()=> loginService.serverPublic != null, 3333)) {
				throw new Exception("Test failed: Test service did not recieve public key!");
			}
			
			testData.admin.GetService<LoginService>().RequestLogin("admin", "admin");

			if (!WaitFor(testService.LoggedIn, 3333)) { 
				throw new Exception("Test Failed: Test service did not log in!");
			}

			if (!WaitFor(testService.PingFinished, 1111)) {
				throw new Exception("Test Failed: Test service did not finish pinging!");
			}

		} finally {

			CleanUp(testData);
		}
		

	}

	/// <summary> Service template class. Intended for copy/pasting to create a new service. </summary>
	public class TestService : Service {
		/// <summary> Callback when the Service is added to a Servcer </summary>
		public override void OnEnable() { }
		/// <summary> Callback when the Service is removed from the server </summary>
		public override void OnDisable() { }



		public bool login = false;
		public bool sawLogin = false;
		public bool LoggedIn() { return sawLogin && login; }
		public void On(LoginService.LoginSuccess_Client e) { 
			//Log.Info($"TestService Saw LoginSuccess: {e.credentials.token}");
			login = sawLogin = true; 
		}
		public void On(LoginService.LoginFailure_Client e) { 
			//Log.Info($"TestService Saw LoginFailure: {e.reason}");
			login = false; 
			sawLogin = true; 
		}

		public bool sawPingFinish = false;
		public bool PingFinished() { return sawLogin; }
		public void On(DebugService.PingNEvent e) { if (e.val == 0) { sawPingFinish = true; } }

		/// <summary> Callback every global server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public override void OnTick(float delta) { }

		/// <summary> Callback with a client, called before any <see cref="OnConnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnBeganConnected(Client client) { }

		/// <summary> CallCallbacked with a client when that client has connected. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnConnected(Client client) { }

		/// <summary> Callback with a client when that client has disconnected. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnDisconnected(Client client) { }

		/// <summary> Callback with a client, called after all <see cref="OnDisconnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnFinishedDisconnected(Client client) { }
	}


}
#endif
#endif
