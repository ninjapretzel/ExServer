#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;

namespace Ex {


#if !UNITY
	/// <summary> DB info for data about a map, used to create and make decisions about a map instance </summary>
	[BsonIgnoreExtraElements] 
	public class MapInfo : DBEntry {

		/// <summary> Display name </summary>
		public string name { get; set; }

		/// <summary> Is this map instanced? </summary>
		[BsonDefaultValue(false)]
		public bool instanced { get; set; }
		/// <summary> Default number of instances to create, if this map is instanced. </summary>
		[BsonDefaultValue(1)]
		public int numInstances { get; set; }
		
		/// <summary> Is this map 3d? </summary>
		[BsonDefaultValue(false)]
		public bool is3d { get; set; }
		/// <summary> Does this map show other players's entities? </summary>
		[BsonDefaultValue(false)]
		public bool solo { get; set; }
		/// <summary> Does this map destroy cells (and entities) when not viewed by a client? </summary>
		[BsonDefaultValue(true)]
		public bool sparse { get; set; }
		
		/// <summary> Axes of map to use if 2d. Used on client to hint at orientation if not provided, treated as "xz" </summary>
		[BsonIgnoreIfNull]
		public string axes { get; set; }
		/// <summary> Size of cells in arbitrary units. </summary>
		[BsonDefaultValue(10)]
		public float cellSize { get; set; }
		/// <summary> Radius of visible surrounding cells in arbitrary units. </summary>
		[BsonDefaultValue(2)]
		public int cellDist { get; set; }
		
		/// <summary> Bounds of map space in arbitrary units. If bounds is zero'd, map is infinite. </summary>
		[BsonIgnoreIfNull]
		public Bounds bounds { get; set; }

		/// <summary> Entities in the map </summary>
		[BsonIgnoreIfNull]
		public EntityInstanceInfo[] entities { get; set; }

		[BsonIgnoreIfNull]
		public SkyboxInfo skyboxInfo { get; set; }

		/// <summary> Terrain generation information </summary>
		[BsonIgnoreIfNull]
		public TerrainInfo terrain { get; set; }


	}
	
	/// <summary> Entity information. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInfo {
		/// <summary> Kind of entity </summary>
		public string type { get; set; }
		/// <summary> Information about <see cref="Comp"/>s attached to entity </summary>
		public ComponentInfo[] components;
	}
	/// <summary> Information about <see cref="Comp"/>s attached to an entity </summary>
	[BsonIgnoreExtraElements]
	public class ComponentInfo {
		/// <summary> name of type of component </summary>
		public string type { get; set; }
		/// <summary> Data to merge into component </summary>
		public BsonDocument data { get; set; }
	}

	/// <summary> <see cref="MapInfo"/> embedded entity information. 
	/// These are spawned into entities when the map starts. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInstanceInfo {
		/// <summary> Location of Entity </summary>
		public Vector3 position { get; set; }
		/// <summary> Rotation of Entity </summary>
		public Vector4 rotation { get; set; }
		/// <summary> Scale of Entity </summary>
		public Vector3 scale { get; set; }
		/// <summary> ID of entity object in database </summary>
		public string id { get; set; }
	}

	/// <summary> <see cref="MapInfo"/> embedded background object information.
	/// These are spawned on the client, but not tracked by the server. </summary>
	[BsonIgnoreExtraElements]
	public class BackgroundObjectInfo {
		/// <summary> Location of object </summary>
		public Vector3 position { get; set; }
		/// <summary> Rotation of object </summary>
		public Vector4 rotation { get; set; }
		/// <summary> Scale of object </summary>
		public Vector3 scale { get; set; }
		/// <summary> Name of object prefab to load </summary>
		public string name { get; set; }
	}
#endif

	/// <summary> <see cref="MapInfo"/> embedded skybox information </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class SkyboxInfo {
		/// <summary> Name of Material inside of Unity to use </summary>
		public string material { get; set; }
		/// <summary> Name of Generator to use to perturb skybox material </summary>
		public string generator { get; set; }
		/// <summary> Seed to use for generator </summary>
		public int seed { get; set; }
	}

	/// <summary> <see cref="MapInfo"/> embedded terrain generation information </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class TerrainInfo {
		/// <summary> Physical size of a single terrain tile. </summary>
		public Vector3 tileSize { get; set; }
		/// <summary> Maximum radius of terrain tiles that a client may create. Scaled based on client-side settings. </summary>
		public int repeating { get; set; }
		/// <summary> Samples of heightmap for a single terrain tile. Scaled based on client-side settings. </summary>
		public int meshSegments { get; set; }
		/// <summary> Samples of splatmap for a single terrain tile. Scaled based on client-side settings. </summary>
		public int splatSegments { get; set; }
		/// <summary> Angle of terrain from 'flat' normal to be textured as a cliff </summary>
		public float slopeAngle { get; set; }
		/// <summary> Name of ComputeShader on client </summary>
		public string generator { get; set; }
		/// <summary> Seed passed to all kernels </summary>
		public int seed { get; set; }
		/// <summary> Name of Kernel in ComputeShader to use for heightmap. Defaults to "Heightmap" if not provided. </summary>
#if !UNITY
		[BsonIgnoreIfNull]
#endif
		public string heightmapKernelName { get; set; }
		/// <summary> Name of Kernel in ComputeShader to use for splatmap. Defaults to "Splatmap" if not provided. </summary>
#if !UNITY
		[BsonIgnoreIfNull]
#endif
		public string splatmapKernelName { get; set; }
		/// <summary>
		/// Names of layers for splatting textures on terrain client side.
		/// At least two layers must be provided.
		/// First element is used as clifs, and the rest are painted on with samples of the splatmap kernel. </summary>
		public string[] terrainLayers { get; set; }
		/// <summary> List of Noise data for terrain generation </summary>
		public NoiseInfo noises { get; set; }
		/// <summary> List of UberNoise data for terrain generation </summary>
		public UberNoiseInfo[] ubers { get; set; }

	}

	/// <summary> Information used to generate basic Noise</summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class NoiseInfo {
		/// <summary> Octaves (layers) applied </summary>
		public int octaves { get; set; }
		/// <summary> Scale change per layer </summary>
		public float lacunarity { get; set; }
		/// <summary> Scale of base layer </summary>
		public float scale { get; set; }
	}

	/// <summary> Information used to generate UberNoise </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class UberNoiseInfo : NoiseInfo {
		public float perturb { get; set; }
		public float sharpness { get; set; }
		public float altitudeErosion { get; set; }
		public float ridgeErosion { get; set; }
		public float slopeErosion { get; set; }
		public float gain { get; set; }
		public float startAmplitude { get; set; }
	}


	/// <summary> Request to move an entity to a position </summary>
	/// <remarks> 
	/// Server uses these internally, but also recieves them from clients.
	/// <see cref="serverMove"/> is set false on any that come in from a client, 
	/// so even if a client tries to pretend they are the server, it should not work.
	/// </remarks>
	public struct EntityMoveRequest {
		/// <summary> ID of entity to move </summary>
		public Guid id;
		/// <summary> Assumed old position of the entity </summary>
		public Vector3 oldPos;
		/// <summary> new position of the entity </summary>
		public Vector3 newPos;
		/// <summary> Assumed old rotation of the entity </summary>
		public Vector3 oldRot;
		/// <summary> new rotation of the entity </summary>
		public Vector3 newRot;
		/// <summary> true if the server is instigating this movement, false if it is a client </summary>
		public bool serverMove;
		
	}

}


