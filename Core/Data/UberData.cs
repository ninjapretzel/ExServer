#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using Ex.Utils;
#endif


namespace Ex.Data {
	
	[System.Serializable]
	public struct UberData {

		public float octaves;

		//[Range(-.4f, .4f)]
		public float perturb;
		//[Range(-1f, 1f)]
		public float sharpness;
		//[Range(0, .5f)]
		public float amplify;

		//[Range(0, .25f)]
		public float altitudeErosion;
		//[Range(0, 1.0f)]
		public float ridgeErosion;
		//[Range(0, 1.0f)]
		public float slopeErosion;

		public float lacunarity;
		public float gain;
		public float startAmplitude;
		public float scale;

		public UberData(int octaves, Ex.Utils.SRNG rng) {
			this.octaves = octaves;

			perturb = rng.NextFloat(-.4f, .4f);
			sharpness = rng.NextFloat(-1f, 1f);
			amplify = rng.NextFloat(0, .5f);

			altitudeErosion = rng.NextFloat(0, .25f);
			ridgeErosion = rng.NextFloat(0, 1.0f);
			slopeErosion = rng.NextFloat(0, 1.0f);

			lacunarity = rng.NextFloat(1.1f, 2.5f);
			gain = rng.NextFloat(.2f, .8f);
			startAmplitude = rng.NextFloat(0.1f, 3f);
			scale = rng.NextFloat(.0005f, .0016f);
		}

		public static UberData Defaults {
			get {
				return new UberData() {
					octaves = 2,
					perturb = 0f,
					sharpness = 0.0f,
					amplify = 0.0f,

					altitudeErosion = 0.0f,
					ridgeErosion = 0.0f,
					slopeErosion = 0.0f,

					lacunarity = 2.0f,
					gain = .5f,
					startAmplitude = .9f,
					scale = 1.0f,
				};
			}

		}

		public JsonValue ToJson() {
			return new JsonArray()
				.Add(octaves)
				.Add(perturb).Add(sharpness).Add(amplify)
				.Add(altitudeErosion).Add(ridgeErosion).Add(slopeErosion)
				.Add(lacunarity).Add(gain).Add(startAmplitude).Add(scale);
		}
		public static UberData FromJson(JsonValue value) {
			UberData noise = new UberData();
			UberData DS = Defaults;
			if (value is JsonObject obj) {
				noise.octaves = obj.Pull("octaves", DS.octaves);

				noise.perturb = obj.Pull("perturb", DS.perturb);
				noise.sharpness = obj.Pull("sharpness", DS.sharpness);
				noise.amplify = obj.Pull("amplify", DS.amplify);

				noise.altitudeErosion = obj.Pull("altitudeErosion", DS.altitudeErosion);
				noise.ridgeErosion = obj.Pull("ridgeErosion", DS.ridgeErosion);
				noise.slopeErosion = obj.Pull("slopeErosion", DS.slopeErosion);

				noise.lacunarity = obj.Pull("lacunarity", DS.lacunarity);
				noise.gain = obj.Pull("gain", DS.gain);
				noise.startAmplitude = obj.Pull("startAmplitude", DS.startAmplitude);
				noise.scale = obj.Pull("scale", DS.scale);
			} else if (value is JsonArray arr) {
				noise.octaves = arr.Pull(0, DS.octaves);

				noise.perturb = arr.Pull(1, DS.perturb);
				noise.sharpness = arr.Pull(2, DS.sharpness);
				noise.amplify = arr.Pull(3, DS.amplify);

				noise.altitudeErosion = arr.Pull(4, DS.altitudeErosion);
				noise.ridgeErosion = arr.Pull(5, DS.ridgeErosion);
				noise.slopeErosion = arr.Pull(6, DS.slopeErosion);

				noise.lacunarity = arr.Pull(7, DS.lacunarity);
				noise.gain = arr.Pull(8, DS.gain);
				noise.startAmplitude = arr.Pull(9 , DS.startAmplitude);
				noise.scale = arr.Pull(10, DS.scale);
			}
			

			return noise;
		}
	}
	
}
