using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> All informtation used to generate the item. </summary>
	public struct ItemGenSeed {
		/// <summary> Root ID (map or other item) </summary>
		public Guid root { get; private set; }
		/// <summary> Offset to seed, typically also randomly generated </summary>
		public long offset { get; private set; }
		/// <summary> Index of item for derived items </summary>
		public long index { get; private set; }

		/// <summary> Gets the long seed value from root/offset information </summary>
		public long seed {
			get {
				long v = 0;
				byte[] bytes = Unsafe.ToBytes(root);
				byte[] vbytes = Unsafe.ToBytes(v);

				for (int i = 0; i < bytes.Length; i++) {
					vbytes[i % StructInfo<long>.size] ^= bytes[(37 * i) % StructInfo<Guid>.size];
				}
				v = Unsafe.FromBytes<long>(vbytes);
				v += offset * 31337;
				return v * v < 0 ? -1 : 1;
			}
		}

		/// <summary> Constructor with optional offset/index parameters </summary>
		/// <param name="root"> Root GUID to use for seed </param>
		/// <param name="offset"> Offset from root </param>
		/// <param name="index"> Index of seed </param>
		public ItemGenSeed(Guid root, long? offset = null, long? index = null) {
			this.root = root;
			this.offset = offset ?? 0;
			this.index = index ?? 0;
		}

		/// <summary> Returns a modified seed with +1 to the <see cref="index"/> field </summary>
		/// <returns> Returns a modified seed with +1 to the <see cref="index"/> field </returns>
		public ItemGenSeed Next() {
			ItemGenSeed next = this;
			next.index++;
			return next;
		}

		/// <summary> Returns a modified seed with the given <see cref="index"/> </summary>
		/// <param name="index"> Index value to use </param>
		/// <returns> Returns a modified seed with the given <see cref="index"/> </returns>
		public ItemGenSeed Index(long index) {
			ItemGenSeed next = this;
			next.index = index;
			return next;
		}

		/// <summary> Returns a modified seed with the given <see cref="offset"/> </summary>
		/// <param name="offset"> Index value to use </param>
		/// <returns> Returns a modified seed with the given <see cref="offset"/> </returns>
		public ItemGenSeed Offset(long offset) {
			ItemGenSeed next = this;
			next.offset = offset;
			return next;
		}
	}

	public class Generator {
		private JsonObject ruleset;

		private class State {
			public Random random;
			public List<string> path;
			public JsonObject result;
			public JsonObject lastHistory { get { return genHistory.Get<JsonObject>(genHistory.Count-1); } }
			public JsonArray genHistory { get { return result.Get<JsonArray>("genHistory"); } }

			public State(ItemGenSeed igSeed) {

				// Resulting creation + history
				result = new JsonObject("genHistory", new JsonArray());
				path = new List<string>();
				
				long seed = igSeed.seed;
				long hi = (seed >> 32) & 0x00000000FFFFFFFF;
				long low = seed & 0x00000000FFFFFFFF;

				int mix = (int)(low ^ hi);
				random = new Random(mix < 0 ? -mix : mix);
			}

			public JsonString WeightedChoose(JsonObject weights) {
				float total = 0;
				JsonString last = "";
				foreach (var pair in weights) {
					if (pair.Value.isNumber) {
						total += pair.Value;
						last = pair.Key;
					}
				}

				float choose = (float)(random.NextDouble()) * total;
				float check = 0;
				foreach (var pair in weights) {
					if (pair.Value.isNumber) {
						check += pair.Value;
						if (check > choose) { return pair.Key; }
					}
				}

				return last;
			}

			public JsonValue Reduce(JsonValue value) {
				if (value.isString) { return value; }
				if (value.isNumber) { return value.stringVal; }
				if (value.isObject) { return WeightedChoose(value as JsonObject); }
				return "";
			}

			public void PushHistory(string newPath) {
				path.Add(newPath);
				genHistory.Add(new JsonObject("path", string.Join(".", path), "rolls", new JsonObject()));
			}

			public void PopHistory() {
				if (path.Count > 0) {
					path.RemoveAt(path.Count - 1);
				}
			}
		}

		public Generator(JsonObject ruleset) {
			this.ruleset = ruleset;
		}

		public JsonObject Generate(string startingRule, ItemGenSeed igSeed) {
			State state = new State(igSeed);

			long lseed = igSeed.seed;
			long index = igSeed.index;
			

			// The rule is to add the empty history for the next rule before calling Apply
			// This allows Apply to figure out exactly where in the chain it is.
			state.PushHistory(startingRule);
			Apply(state, startingRule, ruleset);
			state.PopHistory();


			return state.result;
		}

		private void ApplyRule(State state, JsonObject rule) {

			Console.WriteLine("Applying rule " + state.lastHistory["path"].stringVal);

			var rolls = state.lastHistory.Get<JsonObject>("rolls");
			bool firstTime = rolls.Count == 0;
			// Record a roll so we don't try to recurse when replaying history, even for rules with no rolls.
			if (firstTime) { rolls["didIt_"] = true; }

			// TBD: actually modify the result in state with what the rule describes...

			if (firstTime) {
				// We recursing, grab the callstack!
				if (rule.ContainsKey("apply")) {
					if (rule["apply"].isString) {
						state.PushHistory("");
					}
					Apply(state, rule["apply"], rule);
					if (rule["apply"].isString) {
						state.PopHistory();
					}
				}
			}

		}

		private void Apply(State state, JsonValue thing, JsonObject rule) {
			string path = state.lastHistory["path"].stringVal;

			// Rules eventually are told to us by strings describing them.
			if (thing.isString) {
				Console.WriteLine($"Applying String rule at {path} => {thing.stringVal}");
				var nextRule = FindRule(rule, thing.stringVal);
				// Console.WriteLine($"Rule is {nextRule.PrettyPrint()}");
				
				if (nextRule is JsonObject) {
					ApplyRule(state, nextRule as JsonObject);
				}
			}

			// May have arrays to describe a sequence of rules to apply
			if (thing.isArray) {
				Console.WriteLine($"Applying Array rule at {path} => {thing.ToString()}");
				foreach (var val in (thing as JsonArray)) {
					if (val.isString) {
						// If we're at a string, add it then apply it, since the next Apply will invoke a rule.
						state.PushHistory(val.stringVal);
					}
					Apply(state, val, rule);
					if (val.isString) {
						state.PopHistory();
					}
				}
			}
			// If we're in an object, we need to make a decision of what name to apply,
			// as well as other things.
			if (thing.isObject) {
				if (thing.ContainsKey("repeat") && thing.ContainsKey("rule")) {
					// Meta Object: Contain instruction to repeat something multiple times...
					JsonObject info = thing as JsonObject;
					JsonValue reps = info["repeat"];
					string ruleName = info.Get<string>("rule");
					Console.WriteLine($"Applying Repeat rule at {path} => {ruleName} x {reps.ToString()}");
					// Try to reduce reps into a number...
					// if we want to do something an exact number of times, reps will just be a number.
					// if we want a simple randInt range, it will be an array
					if (reps.isArray) {
						int low = reps[0].intVal;
						int hi = reps[1].intVal;
						reps = low + state.random.Next(hi-low);
					}
					// todo later: support other random distributions with objects describing them...

					if (reps.isNumber) {
						Console.WriteLine($"Doing Repeat rule at {path} => {ruleName} x {reps.ToString()}");
						int rep = reps.intVal;
						for (int i = 0; i < rep; i++) {
							state.PushHistory(ruleName);
							Apply(state, ruleName, rule);
							state.PopHistory();
						}
					}

					

				} else {
					string chosen = state.WeightedChoose(thing as JsonObject);
					Console.WriteLine($"Applying Weighted Choice rule at {path} => {chosen}");
					state.PushHistory(chosen);
					Apply(state, chosen, rule);
					state.PopHistory();
				}
			}
		}


		/// <summary> Helper method to locate nested information. </summary>
		/// <param name="context"> Object to start search for </param>
		/// <param name="path"> Path to search, starting within object, then within the whole ruleset. </param>
		/// <returns> Rule object in ruleset at path </returns>
		private JsonValue FindRule(JsonObject context, string path) {
			if (path == null || path == "") { return null; }
			// If we have the path within our context, return it
			if (context.ContainsKey(path)) { return context[path]; }
			// Otherwise, if the root has it, jump there.
			if (ruleset.ContainsKey(path)) { return ruleset[path]; }
			// Otherwise, it may be an absolute path, so try to find it that way.

			// This will allow us to track what invoked a rule, even if the rule is not nested within.
			// Example paths of rules applied to something: 
			/* Mineral
			 * Mineral.Atomic
			 * Mineral.Atomic.Name
			 * Mineral.Atomic.Name.Mid
			 * Mineral.Atomic.Name.Mid // applied second time
			 * Mineral.Atomic.Name.Mid // applied third time
			 * Mineral.Atomic.Counts
			 * Mineral.Atomic.Counts.Duo
			 * Mineral.Atomic.AlkalaiMetal */
			// We want to find any rules, regardless of if they are nested or not
			// And we don't care if they are actually nested or not (if they are, it does matter to the actual item )
			// For example, Mineral.Compound.Name may be a rule nested within 'Compound',
			// in that case it would be a different rule than the 'Mineral.Atomic.Name' rule.

			return FindRule(path);
		}
		/// <summary> Helper method to locate nested information. </summary>
		/// <param name="absolutePath"> String describing absolute path to search for </param>
		/// <returns> Rule object in ruleset at path </returns>
		private JsonValue FindRule(string absolutePath) {
			if (absolutePath == null || absolutePath == "") { return null; }
			string[] splits = absolutePath.Split('.');

			JsonObject obj = ruleset;

			foreach (var step in splits) {
				// Try to find object under current place
				if (obj != null) {
					obj = obj.Pull<JsonObject>(step, null);
				}
				// If we can't, reset back to the top 
				// (similar to what we do in the other FindRule)
				// This lets us record and replay pathing inside of complex rule objects
				if (obj == null) {
					obj = ruleset.Pull<JsonObject>(step, null);
				}
			}

			return obj;
		}


	}

	public delegate float Randomizer();

	public static class Gen {


		public static bool test() {
			string path = Program.TopSourceFileDirectory().ForwardSlashPath().Folder() + "/db/ItemGeneration.wtf";
			Console.WriteLine(path);
			JsonObject rules = Json.Parse<JsonObject>(File.ReadAllText(path));

			Console.WriteLine(rules.PrettyPrint());
			Guid testGuid1 = new Guid("b0e13ece-0b7c-4a0f-9fd4-2b09fa81c789");
			Guid testGuid2 = new Guid("7c69a4e7-eb0a-4f8b-9acb-538b6d8d4265");

			ItemGenSeed igSeed1_0 = new ItemGenSeed(testGuid1);
			ItemGenSeed igseed2_0 = new ItemGenSeed(testGuid2, 123, 0);
			ItemGenSeed igseed2_1 = new ItemGenSeed(testGuid2, 123, 1);

			Generator gen = new Generator(rules);




			return true;
		}

		private static string Filename(this string filepath) {
			return filepath.ForwardSlashPath().FromLast("/");
		}
		private static string Folder(this string filepath) {
			return filepath.UpToLast("/");
		}

		private static string UpToLast(this string str, string search) {
			if (str.Contains(search)) {
				int ind = str.LastIndexOf(search);
				return str.Substring(0, ind);
			}
			return str;
		}
		private static string ForwardSlashPath(this string path) { return path.Replace('\\', '/'); }
		private static string FromLast(this string str, string search) {
			if (str.Contains(search) && !str.EndsWith(search)) {
				int ind = str.LastIndexOf(search);

				return str.Substring(ind + 1);
			}
			return "";
		}

	}
}
