using Ex;
using Ex.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infinigrinder {
	
	/// <summary> Database object for the primary user save data. </summary>
	public class GameState : EntityService.UserEntityInfo {

		public List<Guid> units { get; set; }
		public JsonObject flags;
		public Store<AccountLevels, int> levels;
		public Store<AccountLevels, double> exp;
		public Store<Currency, double> wallet; 
		
		public GameState() : base() { 
			units = new List<Guid>();
			flags = new JsonObject();
			levels = new Store<AccountLevels, int>();
			exp = new Store<AccountLevels, double>();
			wallet = new Store<Currency, double>();

		}


		
	}

	

	
	/// <summary> Database object to store unit stat data, for both saved units, and mob information. </summary>
	public class UnitRecord {
		public Guid owner;
		public Stats stats;
		
		public UnitRecord() : base() { 
			stats = Auto.Init<Stats>();
		}

	}

	public enum Currency {
		Gold, Plat, Crystal,
	}
	public enum AccountLevels {
		Primary,
		Gathering, Refining,
		Cooking, Alchemy, 
		Armorsmith, Weaponsmith, Jewelcraft,
	}
	public enum UnitLevels {
		Primary, Job,
		

	}

	public enum Element {
		Fast, Heavy, Long,
		Slash, Pierce, Impact,
		Light, Fire, Earth,
		Dark, Water, Wind,
	}

	public enum Resource {
		Health, Mana, Spirit, Stamina,
	}

	public enum BaseStats {
		Pow, Grd, Sta,
		Skl, Agi, Spr,
		Lck,
	}
	public enum IntermediateStats {
		Armor, Shell,
		Rflex, Intut,
		Sight, Tough,
	}
	public enum CombatStats {
		Atka, Atkb, Acc, Def,
		Aspd, Cspd, Mspd,
	}
	public enum CombatRatios {
		Res, Eva,
		Crit, Flex,
	}

	/// <summary> Data for a function to derive stat values used in stat calculations. </summary>
	public class Curve {
		/// <summary> Kinds of curves available. </summary>
		public enum Kind {
			/// <summary> Linear, v = rate*x </summary>
			Linear, 
			/// <summary> Power, v = x^rate </summary>
			Pow,
			/// <summary> Exponential, v = rate^x </summary>
			Exp,
			/// <summary> Logarithmic, v = log_rate(x) </summary>
			Log,
			/// <summary> Asymptotic, v = 1 - (rate / (x + rate)) </summary>
			Asymp
		}
		/// <summary> Kind of function to use curve </summary>
		public Kind kind = Kind.Linear;
		/// <summary> Basic rate </summary>
		public float rate = 1;
		/// <summary> Scale of value derived from <see cref="kind"/> </summary>
		public float scale = 1;
		/// <summary> Base added to value derived from <see cref="kind"/> </summary>
		public float baseVal = 0;
		/// <summary> Optional. if provided, final result is not allowed to be higher. </summary>
		public float? ceil = null;
		/// <summary> Optional. if provided, final result is not allowed to be lower. </summary>
		public float? floor = null;
		/// <summary> Evaluates this curve. </summary>
		/// <param name="x"> input value </param>
		/// <returns> derived value </returns>
		public float Eval(float x) {
			float v = 0;
			switch (kind) {
				case Kind.Linear: v = rate * x; break;
				case Kind.Pow: v = Mathf.Pow(x, rate); break;
				case Kind.Exp: v = Mathf.Pow(rate, x); break;
				case Kind.Log: v = Mathf.Log(x, rate); break;
				case Kind.Asymp: v = 1.0f - (rate / (x + rate)); break;
				default: break;
			}
			v *= scale;
			v += baseVal;
			return ceil.HasValue ? Mathf.Min(v, ceil.Value) : v;
		}
		/// <summary> Produces a "Linear" curve with the given <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Linear(float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Linear, rate = 1f, scale = scale, baseVal = baseVal};
		}
		/// <summary> Produces a "Square Root" curve with the given <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Sqrt(float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Pow, rate = .5f, scale = scale, baseVal = baseVal};
		}
		/// <summary> Produces a "Square" curve with the given <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Squre(float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Pow, rate = 2f, scale = scale, baseVal = baseVal };
		}
		/// <summary> Produces a "Cube" curve with the given <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Cube(float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Pow, rate = 3f, scale = scale, baseVal = baseVal };
		}
		/// <summary> Produces an "Exponential" curve with the given <paramref name="rate"/>, <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Exp(float rate = 1.05f, float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Exp, rate = rate, scale = scale, baseVal = baseVal };
		}
		/// <summary> Produces an "Logarithmic" curve with the given <paramref name="rate"/>, <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Log(float rate = 10f, float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Log, rate = rate, scale = scale, baseVal = baseVal };
		}
		/// <summary> Produces an "Asymptotic" curve with the given <paramref name="rate"/>, <paramref name="scale"/> and <paramref name="baseVal"/> values. </summary>
		public static Curve Asymp(float rate = 3600, float scale = 1.0f, float baseVal = 0) {
			return new Curve() { kind = Kind.Asymp, rate = rate, scale = scale, baseVal = baseVal };
		}

	}

	/// <summary> Holds collections of <see cref="Store{K, V}"/>s for all stat groups. </summary>
	public class Stats {
		public Store<Resource, double> max, rec, rep;
		public Store<BaseStats, int> baseStats;
		public Store<BaseStats, double> baseExp;
		public Store<CombatStats, double> combatStats;
		public Store<CombatRatios, double> combatRatios;
		public Store<IntermediateStats, double> intermediateStats;

		public Stats Combine(Stats other) {
			Stats result = Auto.Init<Stats>();
			
			

			return result;
		}
	}


}
