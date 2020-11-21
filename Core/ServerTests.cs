
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

	private static TestData DefaultSetup(string testDbName = "Testing", string testDb = "db", int port = 12345, float tick = 50) {
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
		sock.Connect(IPAddress.Parse("127.0.0.1"), port);
		Client admin = new Client(sock);
		// Client admin = new Client(new TcpClient("localhost", port));
		admin.AddService<DebugService>();
		admin.AddService<LoginService>();
		admin.AddService<EntityService>();
		admin.AddService<MapService>();
		var adminSync = admin.AddService<SyncService>();
		
		try {
			admin.ConnectSlave();
			admin.Call(Members<LoginService>.i.Login, "admin", "admin", VersionInfo.VERSION);

			adminSync.Context("debug").SubscribeTo("gameState");
			Thread.Sleep(50);
		} catch (Exception e) {
			
			Log.Error("Error starting test server", e);
		
		}

		
		return new TestData(server, admin);
	}

	private static void CleanUp(TestData data) {
		Log.Info("Waiting...");
		Thread.Sleep(300);
		Log.Info("Cleaning Up NOW.");
		data.admin.server.Stop();
		data.server.Stop();
	}

	public static void TestSetup() {

		var testData = DefaultSetup();
		// defer CleanUp(testData);
		try {

			

		} finally {

			CleanUp(testData);
		}
		

	}
	
}
#endif
#endif
