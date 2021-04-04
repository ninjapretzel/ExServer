using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakaDB;
using Ex;
using Ex.Utils;
using Ex.Utils.Ext;
using Random = System.Random;

namespace Infinigrinder {
	
	/// <summary> Server side logic for the Eternus game </summary>
	public class Game : Ex.Service {
		static LocalDB<GameState> gameStateDB = DB.Of<GameState>.db;
		static LocalDB<UnitRecord> unitDB = DB.Of<UnitRecord>.db;
		static LocalDB inventoryDB = DB.Local("Inventory");

		LoginService login;
		SyncService sync;
		EntityService entity;

		StatCalc statCalc;
		Heap<LiveGame> liveGames;
		Dictionary<string, LiveGame> logins;

		public class LiveGame : IComparable<LiveGame> {
			public DateTime lastUpdate { get; private set; }
			public GameState state { get; private set; }
			public Credentials creds { get; private set; }

			public LiveGame(Credentials creds) {
				this.creds = creds;
				lastUpdate = DateTime.UtcNow;
				state = gameStateDB.Open(""+creds.userId);
			}
			
			public int CompareTo(LiveGame obj) { return lastUpdate.CompareTo(obj.lastUpdate); }
		}


		
		


		/// <summary> Callback when all services are loaded on the server.</summary>
		public override void OnStart() {
			Log.Info("Infinigrinder.Game.OnStart()");
			string s = gameStateDB != null ? gameStateDB.ToString() : "null";
			Log.Info($"DB is {s}");

			login = GetService<LoginService>();
			sync = GetService<SyncService>();
			entity = GetService<EntityService>();

			liveGames = new Heap<LiveGame>();
			logins = new Dictionary<string, LiveGame>();

			entity.RegisterUserEntityInfo<GameState>();
			login.userInitializer += Initialize;
			
			statCalc = DB.Local("Content").Get<StatCalc>("StatCalc");
			DB.Drop<GameState>();
			DB.Drop<UnitRecord>();
			DB.Drop("Inventory");

			JsonObject test = new JsonObject(
				"str", 5, "dex", 12, 
				"maxHealth", 200,
				"recHealth", 1.5,
				"what", 123123
			);

			JsonObject result1 = statCalc.SmartMask(test, statCalc.ExpStatRates);
			JsonObject result2 = statCalc.SmartMask(test, statCalc.CombatStats);
			Log.Info(result1);
			Log.Info(result2);
			

			
		}

		public void On(LoginService.LoginSuccess_Server succ) {
			LiveGame live = new LiveGame(succ.creds);
			
			logins[succ.creds.username] = live;
			liveGames.Push(live);


			
			

		}

		/// <summary> Initialize the game for the player with the given guid. Deletes existing data. </summary>
		/// <param name="guid"> Guid of player to initialize game state of. </param>
		public void Initialize(Guid guid) {
			string id = $"{guid}";

			Log.Info($"Initializing user {guid}...");
			
			GameState gs = new GameState();

			gs.map = "avalon";
			gs.position = new Vector3(-160, -360, 0);
			gs.rotation = Vector3.zero;
			gs.skin = "Default";
			gs.flags["test"] = true;
			foreach (var kind in Enum<AccountLevels>.items) {
				gs.levels[kind] = 1;
				gs.exp[kind] = 0;
			}
			gs.color = new Vector4(1, .5f, .5f, 1).Hex();
			gs.wallet[Currency.Gold] = 100;
			gs.wallet[Currency.Plat] = 0;
			gs.wallet[Currency.Crystal] = 10;
			gameStateDB.Save(id, gs);

			JsonObject inv = new JsonObject();
			inventoryDB.Save(id, inv);
			
			UnitRecord unit = new UnitRecord();
			unit.owner = guid;
			unit.stats.baseStats += 5;
			
			unitDB.Save(id, unit);


			
		}

		/// <summary> Callback when the Service is removed from the server </summary>
		public override void OnDisable() {
			
		}

		/// <summary> Callback every global server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public override void OnTick(float delta) {
			
		}

		/// <summary> Callback with a client, called before any <see cref="OnConnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnBeganConnected(Client client) {
			
		}

		/// <summary> CallCallbacked with a client when that client has connected. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnConnected(Client client) {
			
		}

		/// <summary> Callback with a client when that client has disconnected. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnDisconnected(Client client) {
			
		}

		/// <summary> Callback with a client, called after all <see cref="OnDisconnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnFinishedDisconnected(Client client) {
			
		}
	}
}
