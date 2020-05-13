using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex;
using MongoDB.Driver;

namespace Eternus {
	
	/// <summary> Server side logic for the Eternus game </summary>
	public class EternusGame : Ex.Service {
		DBService db;
		LoginService logins;
		SyncService sync;

		StatCalc statCalc;

		/// <summary> Callback when the Service is added to a Servcer </summary>
		public override void OnEnable() {
			db = GetService<DBService>();
			logins = GetService<LoginService>();
			sync = GetService<SyncService>();

			var syncData = sync.Context("data");
			
			statCalc = db.Get<StatCalc>("Content", "filename", "StatCalc");
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
			Client client = succ.client;
			Guid clientId = client.id;

			GameState gameState = db.Get<GameState>(clientId);
			if (gameState == null) {
				Initialize(clientId);
			}

		}

		/// <summary> Initialize the game for the player with the given guid. Deletes existing data. </summary>
		/// <param name="guid"> Guid of player to initialize game state of. </param>
		public void Initialize(Guid guid) {
			var gameState = Reinit<GameState>(guid, it => { 
				it.flags["test"] = true;
				it.levels["primary"] = 1;
				it.exp["primary"] = 0;
			});
			var resources = Reinit<UserResources>(guid, it => {

			});
			var stats = Reinit<UnitStats>(guid, it => {
				foreach (var pair in statCalc.BaseStats) {
					it.baseStats[pair.Key] = 5;
				}

				statCalc.FullRecalc(it);

			});
			
		}
		
		/// <summary> Deletes and re-creates a model of a given type by GUID. </summary>
		/// <typeparam name="T"> Generic type of database entry to reinitialize </typeparam>
		/// <param name="guid"> Primary GUID to reinitialize for </param>
		/// <param name="initializer"> Optional procedure to set up the thing. </param>
		/// <returns> Created object </returns>
		private T Reinit<T>(Guid guid, Action<T> initializer = null) where T : DBEntry, new() {
			db.Remove<T>(guid);
			T thing = new T() { guid = guid };
			initializer?.Invoke(thing);
			db.Save(thing);
			return thing;
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
