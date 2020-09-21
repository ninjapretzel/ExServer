using Ex;
using Ex.Utils;
using System;
using System.Collections.Generic;
using Random = System.Random;

namespace Eternus.Generation {
	
	public static class GenerationExt {
		public static Guid NextGuid(this Random random) {
			Span<byte> loc = stackalloc byte[16];
			random.NextBytes(loc);
			return new Guid(loc);
		}
	}

	/// <summary> Class simply to root the chain. </summary>
	public class Root : DBEntry { 
		public Guid universeGuid;
	}
	/// <summary> Settings to generate a universe </summary>
	public class UniverseSettings { 
		public int iteration;

	}

	/// <summary> Mineral description</summary>
	public class Mineral : DBData {
		public enum Group {
			NONE,
			AlkalaiMetal, AlkalaiEarth,
			TransMetal, PostTransMetal,
			Metaloid, Nonmetal, Halogen, Noble,
		}
		/// <summary> Name of the Mineral </summary>
		public string name { get { return data.Get<string>(MemberName()); } }
		/// <summary> Standard Temperature/Pressure state </summary>
		public string stpState { get { return data.Get<string>(MemberName()); } }
		/// <summary> Elemental group </summary>
		public Group group { get { return data.Get<Group>(MemberName()); } }
		/// <summary> Series number (0-20 instead of 1-18) </summary>
		public float num { get { return data.Get<float>(MemberName()); } }
		/// <summary> "Atomic Weight" </summary>
		public float weight { get { return data.Get<float>(MemberName()); } }
		/// <summary> Material Density </summary>
		public float density { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative reactivity (0-1) </summary>
		public float reactivity { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative malleability (0-1) </summary>
		public float malleability { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative magnetism (0-1) </summary>
		public float magnetism { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative electron conductivity (0-1) </summary>
		public float conductivity { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative ionization energy (0-1) </summary>
		public float ionizeEnergy { get { return data.Get<float>(MemberName()); } }
		/// <summary> Relative latent energy (0-1) </summary>
		public float latentenergy { get { return data.Get<float>(MemberName()); } }

	}
	/// <summary> Top level item generated. Everything belongs to a universe. </summary>
	public class Universe : Generatable<Universe, Root, UniverseSettings> {
		public int iteration;
		public Guid startingXystem;
		public List<Guid> materials;

		public override void Generate(DBService db, Root root, UniverseSettings settings) {
			iteration = settings.iteration;
			Random rand = new Random(seed);
			startingXystem = rand.NextGuid();
			
			materials = new List<Guid>();
			var data = db.GetData("Content", "ItemGeneration", "filename", "Materials");
			var gen = new Generator(data);
			
			int numMats = 118 + rand.Next(32);
			Log.Info($"Starting Xystem is {startingXystem}. Generating {numMats} minerals.");
			List<Mineral> generated = new List<Mineral>();
			int i;
			for (i = 0; i < numMats; i++) {
				Guid guid = rand.NextGuid();
				GenSeed genSeed = new GenSeed(guid);
				var result = gen.Generate("Mineral", genSeed);
				
				Mineral m = new Mineral() { data = result, guid = guid };
				generated.Add(m);
				db.SaveByGuid(m);


			}

			generated.Sort( (a, b) => (int) (1000 * (a.num - b.num) ) ) ;
			i = 0;
			foreach (var m in generated) {
				Log.Info($"Material {i++:D03} is {guid} / " +
					$"{m.name} / {m.stpState} / {m.group} / " +
					$"{m.num} / {m.weight}");
			}


		}
	}
	/// <summary> Settings to generate an ExoSystem </summary>
	public class XystemSettings {
		public Vector3 position;
		public Vector3 direction;
	}
	public class Xystem : Generatable<Xystem, Universe, XystemSettings> {
		
		public override void Generate(DBService db, Universe universe, XystemSettings settings) {
			SRNG rand = new SRNG(seed);

		}
	}

}
