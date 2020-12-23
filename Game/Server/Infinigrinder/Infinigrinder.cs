using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infinigrinder.Generation;
using Ex;
using Ex.Utils;
using Ex.Utils.Ext;
using MongoDB.Driver;

using Random = System.Random;

namespace Infinigrinder {
	
	/// <summary> Server side logic for the Eternus game </summary>
	public class Infinigrinder : Ex.Service {
		DBService db;
		LoginService logins;
		SyncService sync;
		EntityService entities;

		StatCalc statCalc;

		/// <summary> Callback when all services are loaded on the server.</summary>
		public override void OnStart() {
			db = GetService<DBService>();
			logins = GetService<LoginService>();
			sync = GetService<SyncService>();
			entities = GetService<EntityService>();

			entities.RegisterUserEntityInfo<GameState>();
			logins.userInitializer += Initialize;
			
			statCalc = db.Get<StatCalc>("Content", "filename", "StatCalc");
			JsonObject test = new JsonObject(
				"str", 5, "dex", 12, 
				"maxHealth", 200,
				"recHealth", 1.5,
				"what", 123123
			);

			JsonObject result1 = statCalc.SmartMask(test, statCalc.ExpStatRates);
			JsonObject result2 = statCalc.SmartMask(test, statCalc.CombatStats);
			//Log.Info(result1);
			//Log.Info(result2);

			Guid rootGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
			Root root = db.Initialize<Root>(rootGuid, it => {
				Random rand = new Random(Generatable.GuidToSeed(rootGuid));
				it.universeGuid = rand.NextGuid();
			});
			
			UniverseSettings uniSettings = new UniverseSettings() { iteration = 1 };
			Universe uni = Universe.Generate(db, root.universeGuid, root, uniSettings);


			
		}

		public void On(LoginService.LoginSuccess_Server succ) {
			if (succ.client == null) { return; }
			Log.Info("EternusGame.On(LoginSuccess_Server)");
			//Client client = succ.client;
			//var user = GetService<LoginService>().GetLogin(client);
			//Guid clientId = user.HasValue ? user.Value.credentials.userId : Guid.Empty;

			//GameState gameState = db.Get<GameState>(clientId);
			//if (gameState == null) {
			//	Initialize(clientId);
			//}

		}

		/// <summary> Initialize the game for the player with the given guid. Deletes existing data. </summary>
		/// <param name="guid"> Guid of player to initialize game state of. </param>
		public void Initialize(Guid guid) {
			var userId = guid;

			var gameState = db.Initialize<GameState>(guid, it => { 
				it.map = "Limbo";
				it.position = new Vector3(0, 0, 0);
				it.rotation = new Vector3(0, 0, 0);
				it.skin = "Default";
				it.flags["test"] = true;
				it.levels["primary"] = 1;
				it.exp["primary"] = 0;
				it.color = new Vector4(1, .5f, .5f, 1).Hex();
				it.wallet["gold"] = 100;
				it.wallet["plat"] = 0;
				it.wallet["crys"] = 10;
			});
			var resources = db.Initialize<Inventory>(guid, it => {
			});
			var stats = db.Initialize<UnitRecord>(guid, it => {
				it.owner = userId;

				foreach (var pair in statCalc.BaseStats) {
					it.baseStats[pair.Key] = 5;
				}

				statCalc.FullRecalc(it);
			});
			
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
