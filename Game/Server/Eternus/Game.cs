using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakaDB;
using Ex;
using Ex.Libs;
using Ex.Utils;
using Ex.Utils.Ext;
using Random = System.Random;

namespace Eternus {
	
	/// <summary> Server side logic for the Eternus game </summary>
	public class Game : Ex.Service {

		static LocalDB<GameState> gameStateDB = DB.Of<GameState>.db;
		//static LocalDB<UnitRecord> unitDB = DB.Of<UnitRecord>.db;
		static LocalDB inventoryDB = DB.Local("Inventory");

		LoginService login;
		SyncService sync;
		EntityService entity;
		MapService map;

		Heap<LiveGame> liveGames;
		Dictionary<string, LiveGame> gamesByUsername;
		Dictionary<Guid, LiveGame> gamesByGUID;
		ConcurrentSet<LiveGame> loggedOut;

		public class LiveGame : IComparable<LiveGame> {
			public DateTime lastUpdate { get; private set; }
			public GameState state { get; private set; }
			public Credentials creds { get; private set; }

			public LiveGame(Credentials creds) {
				this.creds = creds;
				lastUpdate = DateTime.UtcNow;
				state = gameStateDB.Open(""+creds.userId);
			}
			public void Updated() {
				lastUpdate = DateTime.UtcNow;
				Log.Info($"Updated {creds.username} at {lastUpdate}");
			}
			
			public int CompareTo(LiveGame obj) { return lastUpdate.CompareTo(obj.lastUpdate); }
		}


		
		


		/// <summary> Callback when all services are loaded on the server.</summary>
		public override void OnStart() {
			Log.Info("Eternus.Game.OnStart()");
			string s = gameStateDB != null ? gameStateDB.ToString() : "null";
			// Log.Info($"DB is {s}");

			login = GetService<LoginService>();
			sync = GetService<SyncService>();
			entity = GetService<EntityService>();
			map = GetService<MapService>();

			liveGames = new Heap<LiveGame>();
			gamesByUsername = new Dictionary<string, LiveGame>();
			gamesByGUID = new Dictionary<Guid, LiveGame>();
			loggedOut = new ConcurrentSet<LiveGame>();

			entity.RegisterUserEntityInfo<GameState>();
			login.userInitializer += Initialize;
			
			// statCalc = DB.Local("Content").Get<StatCalc>("StatCalc");
			DB.Drop<GameState>();
			DB.Drop<UnitRecord>();
			DB.Drop("Inventory");

			// @TODO: For testing, remove for release
			DB.Drop<List<LoginService.UserAccountCreation>>();

			//JsonObject test = new JsonObject(
			//	"str", 5, "dex", 12, 
			//	"maxHealth", 200,
			//	"recHealth", 1.5,
			//	"what", 123123
			//);

			//JsonObject result1 = statCalc.SmartMask(test, statCalc.ExpStatRates);
			//JsonObject result2 = statCalc.SmartMask(test, statCalc.CombatStats);
			//Log.Info(result1);
			//Log.Info(result2);
			
			
		}
			

		public void On(LoginService.LoginSuccess_Server succ) {
			LiveGame live = new LiveGame(succ.creds);
			gamesByUsername[succ.creds.username] = live;
			gamesByGUID[succ.creds.userId] = live;
			liveGames.Push(live);
			var game = live.state;
			var createEntity = new EntityService.CreateEntityForUser();
			createEntity.userId = succ.creds.userId;
			server.On(createEntity);

			var ctrl = entity.AddComponent<Control>(succ.client.id);
			ctrl.mode = game.controlMode;
			ctrl.Send();

		}

		public void On(LoginService.Logout_Server logout) {
			var name = logout.creds.username;
			if (gamesByUsername.ContainsKey(name)) {
				Log.Info($"On(Logout_Server): user {name} being logged out next tick");
				LiveGame live = gamesByUsername[name];
				loggedOut.Add(live);

			} else {
				Log.Warning($"On(Logout_Server): user {name} never bound to a LiveGame!");
			}

		}

		/// <summary> Initialize the game for the player with the given guid. Deletes existing data. </summary>
		/// <param name="guid"> Guid of player to initialize game state of. </param>
		public void Initialize(Guid guid) {
			string id = $"{guid}";

			Log.Info($"Initializing user {guid}...");
			
			GameState gs = new GameState();
			

			gs.stats = Auto.Init<Stats>();
			gs.stats.baseStats += 10;

			gs.map = "avalon";
			gs.controlMode = "TopDown";
			gs.position = new Vector3(5, 0, -10);
			gs.rotation = Vector3.zero;
			gs.skin = "Default";
			gs.color = "0xD5E3FFFF";
			gs.color2 = "0xA9CBD9FF";
			gs.color3 = "0x2364F1FF";
			gs.color4 = "0x6086E0FF";

			gs.flags["test"] = true;
			foreach (var kind in Enum<AccountLevels>.items) {
				gs.levels[kind] = 1;
				gs.exp[kind] = 0;
			}
			gs.color = new Vector4(1, .5f, .5f, 1).Hex();
			gs.wallet[Currency.Brouzouf] = 100;
			gs.wallet[Currency.Eternium] = 0;
			gs.wallet[Currency.Forevite] = 10;
			gameStateDB.Save(id, gs);

			JsonObject inv = new JsonObject();
			inventoryDB.Save(id, inv);
			
			//UnitRecord unit = new UnitRecord();
			//unit.owner = guid;
			//unit.stats.baseStats += 5;
			
			//unitDB.Save(id, unit);
			
		}

		/// <summary> Callback when the Service is removed from the server </summary>
		public override void OnDisable() {
			
		}

		const float TickTime = 6f;
		/// <summary> Callback every global server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public override void OnTick(float delta) {
			if (liveGames.Count > 0) {
				LiveGame peek = liveGames.Peek();
				TimeSpan diff = DateTime.UtcNow - peek.lastUpdate;

				while (diff.TotalSeconds > TickTime && liveGames.Count > 0) {
					LiveGame next = liveGames.Peek();
					Guid id = next.creds.userId;
					string name = next.creds.username;

					diff = DateTime.UtcNow - next.lastUpdate;
					if (diff.TotalSeconds < TickTime) { break; }
					try {
						next = liveGames.Pop();
						next.Updated();
						Log.Info($"Ticking {name}'s game data. ID={id}");

						// Actual tick logic goes here:
						next.state.stats.baseExp += 1;

						gameStateDB.Save($"{id}", next.state);
					} catch (Exception e) {
						Log.Warning($"Game.OnTick: Error during tick", e);
					} finally {
						if (loggedOut.Contains(next)) { 
							Log.Info($"Stopping ticks for {name}");
							gamesByGUID.Remove(id);
							gamesByUsername.Remove(name);
							loggedOut.Remove(next);
						} else {
							liveGames.Push(next);
						}
					}
				}
			}

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
