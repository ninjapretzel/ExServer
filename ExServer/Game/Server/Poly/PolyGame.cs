using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex;
using Ex.Utils.Ext;
using MongoDB.Driver;


namespace Poly {

	public class PolyGame : Ex.Service {
		DBService db;
		LoginService logins;
		SyncService sync;

		StatCalc statCalc;

		public override void OnEnable() {


		}

		public void On(Server.Started start) {

			db = GetService<DBService>();
			logins = GetService<LoginService>();
			sync = GetService<SyncService>();

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
			Log.Info("PolyGame.On(LoginSuccess_Server)");
			Client client = succ.client;
			Guid clientId = client.id;
			

			GameState gameState = db.Get<GameState>(clientId);
			if (gameState == null) {
				Initialize(clientId);
			} else {

			}

		}

		public static readonly string[] skins = new string[] {
			"cube", "sphere", "capsule", "cylinder"
		};

		public void Initialize(Guid guid) {
			byte[] bytes = Unsafe.ToBytes(guid);
			// Modern guids are randomized, so we can use all but byte 6 as seeds!

			var state = db.Initialize<GameState>(guid, it => {
				const byte half = 0x80;
				byte cc(byte src) { return (byte)(half | src); }

				it.flags["fresh"] = true;
				it.levels["primary"] = 1;
				it.exp["primary"] = 0;

				it.rolls["skin"] = bytes[0];
				it.rolls["r"] = cc(bytes[1]);
				it.rolls["g"] = cc(bytes[2]);
				it.rolls["b"] = cc(bytes[3]);
				it.rolls["curve"] = bytes[4];
				it.rolls["lck"] = bytes[5] / 255f;
				// byte 6 is polluted with format info.
				it.rolls["str"] = bytes[7] / 255f;
				it.rolls["vit"] = bytes[8] / 255f;
				it.rolls["end"] = bytes[9] / 255f;
				it.rolls["dex"] = bytes[10] / 255f;
				it.rolls["apt"] = bytes[11] / 255f;
				it.rolls["agi"] = bytes[12] / 255f;
				it.rolls["mag"] = bytes[13] / 255f;
				it.rolls["rsv"] = bytes[14] / 255f;
				it.rolls["spi"] = bytes[15] / 255f;
				
				it.data["skin"] = skins[ bytes[0] % skins.Length ];
				
				byte[] buffer = new byte[] { cc(bytes[1]), cc(bytes[2]), cc(bytes[3]), 0xFF };
				it.data["color"] = buffer.Hex();
			});


			var stats = db.Initialize<UnitStats>(guid, it => {
				foreach (var pair in statCalc.BaseStats) {
					it.baseStats[pair.Key] = 1 + Math.Floor(state.rolls[pair.Key].floatVal * 10f);
				}
				it.level = state.levels["primary"].intVal;
				statCalc.FullRecalc(it);
			});



			var resources = db.Initialize<UserResources>(guid, it => {
				foreach (var pair in stats.combatStats) {
					string key = pair.Key;
					if (key.StartsWith("max")) {
						it.data[key.Replace("max", "")] = pair.Value;
					}

				}

			});


		}

	}
}
