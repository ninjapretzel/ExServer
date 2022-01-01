using Ex;
using Ex.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternus {
	
	/// <summary> Database object for the primary user save data. </summary>
	public class GameState : EntityService.UserEntityInfo {

		//public List<Guid> units { get; set; }
		public JsonObject flags;
		public Stats stats;
		public Store<AccountLevels> levels;
		public Store<AccountLevels> exp;
		public Store<Currency> wallet; 
		
		public GameState() : base() { 
			//units = new List<Guid>();
			flags = new JsonObject();
			levels = new Store<AccountLevels>();
			exp = new Store<AccountLevels>();
			wallet = new Store<Currency>();

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
		/// <summary> Primary in-game currency. </summary>
		Brouzouf,
		/// <summary> Premium currency earned in-game. </summary>
		Eternium,
		/// <summary> Premium currency purchased. </summary>
		Forevite,
	}
	public enum AccountLevels {
		Primary, Job,
		//Gathering, Refining,
		//Cooking, Alchemy, 
		//Armorsmith, Weaponsmith, Jewelcraft,
	}
	
	public enum Element {
		/// <summary> Represents fast weapons like daggers or claws </summary>
		Fast, 
		/// <summary> Represents heavy weapons like axes and hammers </summary>
		Heavy, 
		/// <summary> Represents long weapons like axes and hammers </summary>
		Long,

		/// <summary> Represents damage like slashing, rending, tearing </summary>
		Slash, 
		/// <summary> Represents damage like puncturing, pricking, stabbing  </summary>
		Pierce, 
		/// <summary> Represents damage like impacting, crushing, smashing </summary>
		Impact,

		/// <summary> Represents damage from light magic </summary>
		Light, 
		/// <summary> Represents damage from fire magic </summary>
		Fire, 
		/// <summary> Represents damage from earth magic </summary>
		Earth,
		/// <summary> Represents damage from dark magic </summary>
		Dark, 
		/// <summary> Represents damage from water magic </summary>
		Water, 
		/// <summary> Represents damage from wind magic </summary>
		Wind,
	}

	public enum Resource {
		/// <summary> Primary resource for staying alive </summary>
		Health, 
		/// <summary> Resource for magic skills </summary>
		Mana, 
		/// <summary> Resource for non-magic skills </summary>
		Spirit, 
		/// <summary> Resource for improved movement </summary>
		Stamina,
		/// <summary> Resource for protecting from damage </summary>
		Shield, 
	}

	public enum BaseStats {
		/// <summary> Stat for damaging things more </summary>
		Pow, 
		/// <summary> Stat for being more accurate </summary>
		Aim,
		/// <summary> Stat for taking less damage </summary>
		Grd, 
		/// <summary> Stat for having more health </summary>
		Sta,
		/// <summary> Stat for being lucky </summary>
		Lck,
	}
	public enum IntermediateStats {
		/// <summary> Gives additional <see cref="CombatRatios.Res"/> </summary>
		Armor, 
		/// <summary> Gives additional <see cref="CombatRatios.Eva"/> </summary>
		Rflex, 
		/// <summary> Gives additional <see cref="CombatRatios.Crit"/> </summary>
		Sight, 
		/// <summary> Gives additional <see cref="CombatRatios.Flex"/> </summary>
		Tough,
	}
	public enum CombatStats {
		/// <summary> Additional damage </summary>
		Atka, 
		/// <summary> Base damage </summary>
		Atkb, 
		/// <summary> Accuracy </summary>
		Acc, 
		/// <summary> Defense, exact damage reduction </summary>
		Def,
		/// <summary> Attack Speed </summary>
		Aspd, 
		/// <summary> Cast/Channel/ Ability Speed </summary>
		Cspd, 
		/// <summary> Movement Speed </summary>
		Mspd,
	}
	public enum CombatRatios {
		/// <summary> Resistance, percentage damage reduction </summary>
		Res, 
		/// <summary> Evasion, subtracted from accuracy </summary>
		Eva,
		/// <summary> Critical chance </summary>
		Crit, 
		/// <summary> Flexibility, crit reduction percentage. (50% crit vs 50% flex = 25% crit) </summary>
		Flex,
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
		/// <summary> Maximum of resources </summary>
		public Store<Resource> max;
		/// <summary> REcover Constant</summary>
		public Store<Resource> rec;
		/// <summary> REcover Percentage </summary>
		public Store<Resource> rep;
		/// <summary> base stats </summary>
		public Store<BaseStats> baseStats;
		/// <summary> exp for increasing base stats </summary>
		public Store<BaseStats> baseExp;
		/// <summary> current combat stats </summary>
		public Store<CombatStats> combatStats;
		/// <summary> current combat ratios </summary>
		public Store<CombatRatios> combatRatios;
		/// <summary> current intermediate stats </summary>
		public Store<IntermediateStats> intermediateStats;

		public Stats Combine(Stats other) {
			Stats result = Auto.Init<Stats>();
			
			result.max = max + other.max;
			result.rec = rec + other.rec;
			result.rep = rep + other.rep;

			result.baseStats += other.baseStats;
			result.baseExp += other.baseExp;
			result.combatStats += other.combatStats;
			result.combatRatios = combatRatios.CombineAsRatios(other.combatRatios);
			result.intermediateStats += other.intermediateStats;
			
			return result;
		}


	}

		


}
