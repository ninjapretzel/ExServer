using Ex;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Eternus {
	[BsonIgnoreExtraElements]
	public class StatCalc : DBData {
		// Basically a bunch of auto members.
		public JsonObject Attributes { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject BaseStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject IntermediateStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject CombatStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject CombatRatios { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject Resistances { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject Affinities { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject Resources { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ResourceCap { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ResourceCurrent { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ResourceRecovery { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ResourceRecovery2 { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ResourceLastUse { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonArray Vitals { get { return data.Get<JsonArray>(MemberName()); } }
		public JsonArray Rules { get { return data.Get<JsonArray>(MemberName()); } }
		public JsonObject FloorRule { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject BaseCombatStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject ExpStatRates { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject CombatStatCalc { get { return data.Get<JsonObject>(MemberName()); } }


		public void CalcStats(JsonObject stats) {
			
		}

		public JsonObject SmartMask(JsonObject stats, JsonObject lim) {
			JsonObject result = new JsonObject();
			foreach (var pair in lim) {
				string check = pair.Key;

				if (check.StartsWith("@")) {
					string rest = check.Replace("@", "");
					// @Anchor to some other named group in data
					if (data.Has<JsonObject>(rest)) {
						foreach (var pair2 in data.Get<JsonObject>(rest)) {
							if (stats.Has(pair2.Key)) {
								result[pair2.Key] = stats[pair2.Key];
							}
						}
					}

				} else if (check.StartsWith("$")) {
					// $Suffix matching
					string rest = check.Replace("$", "");
					foreach (var pair2 in stats) {
						if (pair2.Key.stringVal.EndsWith(rest)) {
							result[pair2.Key] = pair2.Value;
						}
					}

				} else if (check.EndsWith("$")) {
					// Prefix$ matching
					string rest = check.Replace("$", "");
					foreach (var pair2 in stats) {
						if (pair2.Key.stringVal.StartsWith(rest)) {
							result[pair2.Key] = pair2.Value;
						}
					}
				} else if (stats.Has(pair.Key)) {
					// Direct key, and present
					result[pair.Key] = pair.Value;
				}
			}

			return result;
		}

		

	}


}
