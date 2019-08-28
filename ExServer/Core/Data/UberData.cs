#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

#if !UNITY
[BsonIgnoreExtraElements]
#endif
[System.Serializable]
public struct UberData {

	public int octaves;

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

	public UberData(int octaves, BakaBaka.Utils.SRNG rng) {
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

}
