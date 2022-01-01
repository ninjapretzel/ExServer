using Ex;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Eternus {
	public class CombatService : Service {

		public class Combat {
			public Guid owner;
			public Unit[] party;
			public Unit[] foes;

		}

		public class Unit {
			public JsonObject stats;
			public Guid owner { get; private set; }
			public UnitRecord record;

			public Unit(UnitRecord record) {
				this.record = record;
				owner = record.owner;
				stats = new JsonObject();
				




			}

		}

		//DBService db;
		LoginService login;
		Game game;

		ConcurrentQueue<Combat> combats;

		/// <inheritdoc/>
		public override void OnStart() {
			game = GetService<Game>();
			login = GetService<LoginService>();
			//db = GetService<DBService>();
		}

		/// <inheritdoc/>
		public override void OnEnable() { 
			
		}

		/// <inheritdoc/>
		public override void OnDisable() { 
			
		}

		/// <inheritdoc/>
		public override void OnTick(float delta) { 
			
		}

		/// <inheritdoc/>
		public override void OnBeganConnected(Client client) { 
			
		}

		/// <inheritdoc/>
		public override void OnConnected(Client client) { 
			
		}

		/// <inheritdoc/>
		public override void OnDisconnected(Client client) { 
			
		}

		/// <inheritdoc/>
		public override void OnFinishedDisconnected(Client client) { 
			
		}

		public void On(LoginService.LoginSuccess_Server succ) {
			//JsonObject info = new JsonObject();
			//info["kind"] = "grindfest";

			//server.On(new CombatMessages.StartCombat(succ.creds.username, info));
		}

	}
}
