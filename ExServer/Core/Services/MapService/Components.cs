#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif
using System;
using Ex.Utils;
using Ex.Data;
using Ex.Utils.Ext;

namespace Ex {

	/// <summary> Component that places an entity on a map. </summary>
	public class OnMap : Comp {
		/// <summary> ID of map </summary>
		public InteropString32 mapId;
		/// <summary> </summary>
		public int mapInstanceIndex;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} {mapId}#{mapInstanceIndex}"; }
	}

	/// <summary> Component that gives entity a physical location </summary>
	public class TRS : Comp {
		/// <summary> Location of entity </summary>
		public Vector3 position;
		/// <summary> Rotation of entity (euler angles) </summary>
		public Vector3 rotation;
		/// <summary> Scale of entity's display </summary>
		public Vector3 scale;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} TRS {position} : {rotation} : {scale}"; }
	}

	/// <summary> Component that moves an entity's TRS every tick. </summary>
	public class Mover : Comp {
		/// <summary> Delta position per second </summary>
		public Vector3 velocity;
		/// <summary> Delta rotation per second </summary>
		public Vector3 angVelocity;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} Mover {velocity} : {angVelocity}"; }

	}

	/// <summary> Component that gives entity a simple radius-based collision </summary>
	public class Sphere : Comp {
		/// <summary> Radius of entity </summary>
		public float radius;
		/// <summary> Is this sphere a trigger client side? </summary>
		public bool isTrigger;
		/// <summary> Layer for client side collision to be on? </summary>
		public int layer;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} Sphere {radius} : {isTrigger} : {layer}"; }
	}

	/// <summary> Component that gives entity a box-based collision </summary>
	public class Box : Comp {
		/// <summary> Axis Aligned Bounding Box </summary>
		public Bounds bounds;
		/// <summary> Is this sphere a trigger client side? </summary>
		public bool isTrigger;
		/// <summary> Layer for client side collision to be on? </summary>
		public int layer;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} Box {bounds} : {isTrigger} : {layer}"; }
	}

	/// <summary> Component that gives one entity control over another. </summary>
	public class Owned : Comp {
		/// <summary> ID of owner of this entity, who is also allowed to send commands to this entity. </summary>
		public Guid owner;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} Owned by {owner}"; }
	}

	/// <summary> Component that attaches a visible model to an entity on clients. </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class Display : Comp {
		/// <summary> Prefab to display </summary>
		public InteropString32 prefab;
		/// <summary> Adjustment to position, euler</summary>
		public Vector3 position;
		/// <summary> Adjustment to rotation, euler angles</summary>
		public Vector3 rotation;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} display {{{prefab}}} at pos:{{{position}}} / rot:{{{rotation}}}"; }
	}

	/// <summary> Component that shows a name plate and renames the entity on clients. </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class Nameplate : Comp {
		/// <summary> Name to display  </summary>
		public InteropString32 name;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} displayname {name}"; }
	}

	/// <summary> Component that attaches a visible model to an entity on clients. </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
#endif
	public class AnimData : Comp {
		/// <summary> Animation mapper to use </summary>
		public InteropString32 mapper;
		/// <summary> float animation parameters </summary>
		public InteropFloat32 floats;
		/// <summary> Animation flags </summary>
		public long flags;
		/// <inheritdoc />
		public override string ToString() { return $"{entityId} anim {mapper} flags { flags.Hex() }"; }
	}


		/// <summary> Component that holds procedural terrain information. </summary>
#if !UNITY
	[BsonIgnoreExtraElements]
	#endif
	public class Terrain : Comp {
		/// <summary> Size of each terrain segment </summary>
		public Vector3 tileSize;
		/// <summary> Diameter of area in segments to create terrain in </summary>
		public float viewDist;

		/// <summary> Name of MeshChunk resource to load </summary>
		public InteropString32 chunk;
		/// <summary> Name of ComputeShader resource to use </summary>
		public InteropString32 shader;
		/// <summary> Name of heightmap kernel to use </summary>
		public InteropString32 heightmapKernelName;
		/// <summary> Name of splatmp kernel to use </summary>
		public InteropString32 splatmapKernelName;

		/// <summary> Name of base terrain layer </summary>
		public InteropString32 terrainBaseLayer;
		/// <summary> Name of cliff terrain layer </summary>
		public InteropString32 terrainCliffLayer;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer1;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer2;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer3;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer4;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer5;
		/// <summary> Name of additional terrain layer, or null/empty if unused </summary>
		public InteropString32 terrainLayer6;

		/// <summary> Number of height samples for terrain segments </summary>
		public int meshSamples;
		/// <summary> Number of splatmap samples for terrain segments </summary>
		public int splatSamples;

		/// <summary> Max angle of terrain before being painted as a cliff </summary>
		public float slopeAngle;

		/// <summary> Terrain seed value </summary>
		public long seed;

		/// <summary> Heightmap Ubernoise data </summary>
		public UberData heightmapUberNoise;
		/// <summary> Splatmap Ubernoise data </summary>
		public UberData splatmapUberNoise;
		/// <summary> Heightmap Simplexnoise data </summary>
		public SimplexNoise heightmapNoise;
		/// <summary> Splatmap Simplexnoise data </summary>
		public SimplexNoise splatmapNoise;

		/// <summary> Extra data segment for non-builtin generation </summary>
		public InteropFloat64 extra;
		
		public override string ToString() { return $"{entityId} Terrain"; }
	}


}
