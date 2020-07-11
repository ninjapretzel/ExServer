using Ex;
using Ex.Utils;
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
		public JsonObject Groups { get { return data.Get<JsonObject>(MemberName()); } }
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

		public void FullRecalc(UnitStats unit) {
			unit.data.Set(unit.baseStats);
			Log.Info($"Starting with base stats: {unit.baseStats.PrettyPrint()}");

			JsonObject result = CalcStats(unit.data);
			Log.Info($"Calculated Stats: {result.PrettyPrint()}");

			unit.data.Set(result);

		}


		public JsonObject CalcStats(JsonObject fullData) {
			JsonObject baseStats = SmartMask(fullData, BaseStats);
			JsonObject baseCombatStats = BaseCombatStats;

			JsonObject groups = new JsonObject();
			foreach (var pair in Groups) { groups.Add(pair.Key, new JsonObject()); }
			groups["BaseStats"] = baseStats;
			groups["CombatStats"] = SmartMask(baseCombatStats, CombatStats);
			groups["CombatRatios"] = SmartMask(baseCombatStats, CombatRatios);

			// todo later, requires more db lookups and equip system
			if (fullData.Has<JsonObject>("Equipment")) { } 


			JsonObject genStats = MatrixMultiply(baseStats, CombatStatCalc);
			ApplyToGroups(groups, genStats);

			JsonObject results = new JsonObject();

			foreach (var pair in groups) {
				results.SetRecursively(pair.Value as JsonObject);
			}

			foreach (var rule in Rules) {
				ApplyRule(results, rule as JsonObject);
			}

			JsonObject floored = SmartMask(results, FloorRule);
			foreach (var pair in floored) {
				results[pair.Key] = Math.Floor(pair.Value.doubleVal);
			}

			return results;
		}

		public static double Clamp01(double v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }
		public static double Clamp(double v, double a, double b) { return v < a ? a : (v > b ? b : v); }

		public static double CombineRatio(double a, double b) {
			return 1.0 - (1.0 - Clamp01(a)) * (1.0 - Clamp01(b));
		}

		public void ApplyRule(JsonObject results, JsonObject rule) {
			string type = rule.Get<string>("type");
			string result = rule.Get<string>("result");
			string cmbRule = rule.Get<string>("rule");
			string source = rule.Get<string>("source");

			double b = results.Pull(source, 0.0);
			double rate = rule.Pull("rate", 1.0);
			
			if (type == "line") { b *= rate; }
			if (type == "quad") { b *= b * rate; }
			if (type == "cube") { b *= b * b * rate; }
			if (type == "asymp") {
				double cap = rule.Pull("cap", 1.0);
				b = Clamp(1 - (rate / (b + rate)), 0.0, cap);
			}
			if (type == "log") {
				double cap = rule.Pull("cap", 1.0);
				double logBase = rule.Pull("base", 1.5);
				b = rate * Clamp(Math.Log(b + logBase, logBase), 0.0, cap);
			}

			//Debug.Log("Doing stuff from " + source + " to " + stat + " as " + type + " and " + cmbRule);

			double s = results.GetNumber(result);
			if (cmbRule == "add") { s += b; }
			if (cmbRule == "mult") { s += b; }
			if (cmbRule == "ratio") { s = CombineRatio(s,b); }
			if (cmbRule == "set") { s = b; }

			results[result] = s;
		}


		/// <summary> Primary workhorse of stat calculation. </summary>
		/// <param name="groups"> Working stats, separated into groups. </param>
		/// <param name="thing"> Thing to apply stats from (equipment, skill, buff, calculated/derived stats, etc) </param>
		public void ApplyToGroups(JsonObject groups, JsonObject thing) {
			if (thing == null) { return; }
			foreach (var pair in Groups) {
				var key = pair.Key;
				string rule = pair.Value.stringVal;

				JsonObject statGroup = data.Get<JsonObject>(pair.Key);
				JsonObject lhs = groups.Get<JsonObject>(pair.Key);
				JsonObject rhs = SmartMask(thing, statGroup);
				
				JsonObject result = lhs;
				if (rule == "add") { result = lhs.AddNumbers(rhs); }
				if (rule == "ratio") { result = lhs.CombineRatios(rhs); }
				// if (rule == "multiply") { result = lhs.Multiply(rhs); }
				
				groups[key] = result;
			}
		}


		public JsonObject MatrixMultiply(JsonObject lhs, JsonObject rhs) {
			double MultiplyRow(JsonObject row) {
				double d = 0;
				foreach (var pair in row) {
					if (pair.Value.isNumber) {
						string key = pair.Key;
						if (key.StartsWith("@")) { // Anchor
							string rest = key.Replace("@", "");
							JsonObject linked = data.Get<JsonObject>(rest);
							if (linked != null) {
								foreach (var pair2 in linked) {
									d += lhs.GetNumber(pair2.Key) * pair.Value.doubleVal;
								}
							}
						} else {
							d += lhs.GetNumber(pair.Key) * pair.Value.doubleVal;
						}
					}
				}
				return d;
			}
			JsonObject result = new JsonObject();

			foreach (var pair in rhs) {
				JsonValue val = pair.Value;
				JsonString key = pair.Key;
				if (val.isObject) { result[key] = MultiplyRow(val as JsonObject); }
				if (val.isNumber) { result[key] = lhs.GetNumber(pair.Key) * val.numVal; }

			}
			
			return result;
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
