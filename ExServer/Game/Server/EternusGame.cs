using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex;

namespace Eternus {
	
	/// <summary> Server side logic for the Eternus game </summary>
	public class EternusGame : Ex.Service {
		DBService db;
		LoginService logins;
		SyncService sync;

		

		/// <summary> Callback when the Service is added to a Servcer </summary>
		public override void OnEnable() {
			db = GetService<DBService>();
			logins = GetService<LoginService>();
			sync = GetService<SyncService>();

			var syncData = sync.Context("data");


			
		}

		public void On(LoginService.LoginSuccess_Server succ) {
			Client client = succ.client;
			Guid clientId = client.id;

			GameState gameState = db.Get<GameState>(clientId);
			if (gameState == null) {
				gameState = new GameState();
				Initialize(gameState);
				gameState.guid = clientId;

			}

			Log.Info("Hey yall I got me a game state: " + Json.Reflect(gameState).PrettyPrint() );
			db.Save(gameState);

			GameState state2 = db.Get<GameState>(clientId);
			Log.Info("Hey yall I got me another game state: " + Json.Reflect(state2).PrettyPrint() );
		}

		public void Initialize(GameState state) {
			state.wallet = new JsonObject();
			state.wallet["gp"] = 50;

			JsonObject drone = new JsonObject();
			state.units = new JsonArray(drone);
			
			
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
