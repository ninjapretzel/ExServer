using Ex;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternus {
	[BsonIgnoreExtraElements]
	public class StatCalc : DBEntry {
		public JsonObject Attributes;
		public JsonObject BaseStats;
		public JsonObject IntermediateStats;
		public JsonObject CombatStats;
		public JsonObject CombatRatios;
		public JsonObject Resistances;
		public JsonObject Affinities;
		public JsonObject Resources;
		public JsonObject ResourceCap;
		public JsonObject ResourceCurrent;
		public JsonObject ResourceRecovery;
		public JsonObject ResourceRecovery2;
		public JsonObject ResourceLastUse;
		public JsonArray Vitals;
		public JsonArray Rules;
		public JsonObject FloorRule;
		public JsonObject BaseCombatStats;
		public JsonObject ExpStatRates;
		public JsonObject CombatStatCalc;
	}

}
