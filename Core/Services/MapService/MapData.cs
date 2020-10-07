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
using static BakaTest.BakaTests;

namespace Ex {

	/// <summary> Axes to use for 2d maps  </summary>
	public enum Axes : int {
		XZ = 0, xz = 0, Xz = 0, xZ = 0,
		XY = 1, xy = 1, Xy = 1, xY = 1,
		YZ = 2, yz = 2, Yz = 2, yZ = 2
	}
	/// <summary> Shape for the bounds of a map </summary>
	public enum BoundsShape { 
		/// <summary> Axis Aligned Bounding Box, or hard bounding on all XYZ. </summary>
		Box, 
		/// <summary> Y-Axis Aligned Bounding Cylinder, or distance bounding on XZ (Radius of X), hard bounding on Y. </summary>
		Cylinder, 
		/// <summary> Bounding Sphere (Radius of X), or distance bounding on XYZ. </summary>
		Sphere, 
	}

	/// <summary> Shared struct version of <see cref="MapInfo"/> for clients to be told about map information when entering maps. </summary>
	public struct MapSettings {
		/// <summary> Display name </summary>
		public InteropString256 name;
		/// <summary> Is this map instanced? </summary>
		public bool instanced;
		/// <summary> Default number of instances to create, if this map is instanced. </summary>
		public int numInstances;
		/// <summary> Is this map 3d? </summary>
		public bool is3d;
		/// <summary> Does this map show other players's entities? </summary>
		public bool solo;
		/// <summary> Does this map destroy cells (and entities) when not viewed by a client? </summary>
		public bool sparse;
		/// <summary> Axes of map to use if 2d. </summary>
		public Axes axes { get; set; }
		/// <summary> Size of cells in arbitrary units. </summary>
		public float cellSize { get; set; }
		/// <summary> Radius of visible surrounding cells in arbitrary units. </summary>
		public int cellDist { get; set; }
		/// <summary> Size of a "Region" in the map. Regions are used to circumvent floating point precision loss. </summary>
		public float regionSize { get; set; }
		/// <summary> Bounds of map space in arbitrary units. If bounds is zero'd, map is infinite. </summary>
		public Bounds bounds { get; set; }
		/// <summary> Shape of bounds to use for edge of map </summary>
		public BoundsShape boundsShape { get; set; }

	}

	public class Region {
		public static Vector3 TrueDifference(Vector3 posA, Vector3Int regionA, Vector3 posB, Vector3Int regionB, float regionSize = 2048f) {
			if (regionA == regionB) { return posB - posA; }

			Vector3 regionDiff = regionB - regionA; 
			Vector3 posBAdj = posB + regionDiff * regionSize;
			
			return posBAdj - posA;
		}
		public static void Normalize(ref Vector3 pos, ref Vector3Int region, float regionSize = 2048f) {
			float halfSize = regionSize/2;
			if (Mathf.Abs(pos.x) > halfSize) {
				int dir = (int)Mathf.Sign(pos.x);
				if (Mathf.Abs(pos.x) > regionSize) { 
					int step = (1 + (int)((pos.x - halfSize) / regionSize));
					pos.x -= dir * regionSize * step; 
					region.x += dir * step;
				} else { pos.x -= dir * regionSize; region.x += (int)dir; }
			}

			if (Mathf.Abs(pos.y) > halfSize) {
				int dir = (int)Mathf.Sign(pos.y);
				if (Mathf.Abs(pos.y) > regionSize) {
					int step = (1 + (int)((pos.y - halfSize) / regionSize));
					pos.y -= dir * regionSize * step;
					region.y += dir * step;
				} else { pos.y -= dir * regionSize; region.y += (int)dir; }
			}

			if (Mathf.Abs(pos.z) > halfSize) {
				int dir = (int)Mathf.Sign(pos.z);
				if (Mathf.Abs(pos.z) > regionSize) {
					int step = (1 + (int)((pos.z - halfSize) / regionSize));
					pos.z -= dir * regionSize * step;
					region.z += dir * step;
				} else { pos.z -= dir * regionSize; region.z += (int)dir; }
			}
		}
	}


#if !UNITY
	/// <summary> DB info for data about a map, used to create and make decisions about a map instance </summary>
	[BsonIgnoreExtraElements] 
	public class MapInfo : DBEntry {

		/// <summary> Cached <see cref="MapSettings"/> </summary>
		[NonSerialized] private MapSettings? _settings;
		/// <summary> Accessor for this map's data as <see cref="MapSettings"/> </summary>
		public MapSettings settings {
			get {
				if (_settings.HasValue) { return _settings.Value; }
				MapSettings sets = new MapSettings();
				JsonObject data = Json.Reflect(this) as JsonObject;
				Json.ReflectInto(data, sets);
				_settings = sets;
				return _settings.Value;
			}
		}
		

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
		
		/// <summary> Axes of map to use if 2d. </summary>
		[BsonDefaultValue(Axes.XZ)]
		public Axes axes { get; set; }
		/// <summary> Size of cells in arbitrary units. </summary>
		[BsonDefaultValue(10)]
		public float cellSize { get; set; }
		/// <summary> Radius of visible surrounding cells in arbitrary units. </summary>
		[BsonDefaultValue(2)]
		public int cellDist { get; set; }
		/// <summary> Size of a "Region" in the map. Regions are used to circumvent floating point precision loss. </summary>
		[BsonDefaultValue(2048f)]
		public float regionSize { get; set; }
		
		/// <summary> Bounds of map space in arbitrary units. If bounds is zero'd, map is infinite. </summary>
		[BsonIgnoreIfNull]
		public Bounds bounds { get; set; }
		/// <summary> Shape of bounds to use for edge of map </summary>
		[BsonIgnoreIfNull]
		public BoundsShape boundsShape { get; set; }

		/// <summary> Entities in the map </summary>
		[BsonIgnoreIfNull]
		public EntityInstanceInfo[] entities { get; set; }


	}
	
	/// <summary> Entity information. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInfo : DBEntry {
		/// <summary> Kind of entity </summary>
		public string type { get; set; }
		/// <summary> Source filename. </summary>
		[BsonIgnoreIfNull]
		public string filename{ get; set; } = "";
		/// <summary> Is this a global (map-wide) entity? </summary>
		[BsonIgnoreIfNull] 
		public bool global { get; set; } = false;
		/// <summary> Information about <see cref="Comp"/>s attached to entity </summary>
		public ComponentInfo[] components;
	}

	/// <summary> Information about <see cref="Comp"/>s attached to an entity </summary>
	[BsonIgnoreExtraElements]
	public class ComponentInfo {
		/// <summary> name of type of component </summary>
		public string type { get; set; }
		/// <summary> Arbitrary Data to merge into component </summary>
		public BsonDocument data { get; set; }
	}

	/// <summary> <see cref="MapInfo"/> embedded entity information. 
	/// These are spawned into entities when the map starts. </summary>
	[BsonIgnoreExtraElements]
	public class EntityInstanceInfo {
		/// <summary> Location of Entity </summary>
		public Vector3 position { get; set; }
		/// <summary> Map "region", used to circumvent floating point precision loss</summary>
		[BsonIgnoreIfNull]
		public Vector3Int region { get; set; }
		/// <summary> Rotation of Entity </summary>
		public Vector3 rotation { get; set; }
		/// <summary> Scale of Entity </summary>
		public Vector3 scale { get; set; }
		/// <summary> ID of entity <see cref="EntityInfo"/> object in database </summary>
		public string type { get; set; }
	}
	
	
#endif


	/// <summary> Request to move an entity to a position </summary>
	/// <remarks> 
	/// Server uses these internally, but also recieves them from clients.
	/// <see cref="serverMove"/> is set false on any that come in from a client, 
	/// so even if a client tries to pretend they are the server, it should not work.
	/// </remarks>
	public struct EntityMoveRequest {
		/// <summary> ID of entity to move </summary>
		public Guid id;
		/// <summary> new position of the entity </summary>
		public Vector3 newPos;
		/// <summary> new region of the entity, to circumvent </summary>
		public Vector3Int newRegion;
		/// <summary> new rotation of the entity </summary>
		public Vector3 newRot;
		/// <summary> true if the server is instigating this movement, false if it is a client </summary>
		public bool serverMove;
	}
	
	
	public class Region_Tests {
		public static void TestTrueDifference() {
			Vector3 posA = new Vector3(0, 0, 0);
			Vector3Int regionA = new Vector3Int(0, 0, 0);
			Vector3 posB = new Vector3(0, 0, 0);
			Vector3Int regionB = new Vector3Int(0, 0, 0);
			Vector3 expected = Vector3.zero;
			float regionSize = 2048;
			{
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				regionB += Vector3Int.right;
				expected.x = regionSize;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				regionA -= Vector3Int.right;
				expected.x = regionSize*2;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				posA = new Vector3(regionSize / 2, regionSize/2, regionSize/2);
				regionA = new Vector3Int(0, 0, 0);
				posB = new Vector3(-regionSize/2, -regionSize/2, -regionSize/2);
				regionB = new Vector3Int(1,1,1);
				expected = Vector3.zero;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}



		}

		public static void TestNormalize() {
			Vector3 pos = new Vector3(0, 0, 0);
			Vector3Int region = new Vector3Int(0, 0, 0);
			Vector3 expectedPos = Vector3.zero;
			Vector3Int expectedRegion = Vector3Int.zero;
			float regionSize = 2048;
			{
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos); 
				region.ShouldEqual(expectedRegion);
			} {
				pos += Vector3.right * regionSize;
				expectedRegion += Vector3Int.right;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos += Vector3.right * regionSize * 3;
				expectedRegion += Vector3Int.right * 3;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos += new Vector3(1234, 4567, 8910);
				expectedPos += new Vector3(1234 - regionSize, 4567 - (2 * regionSize), 8910 - (4*regionSize));
				expectedRegion += new Vector3Int(1, 2, 4);
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} { // Normalize should not move regions when exactly on closest edge
				pos = new Vector3(regionSize/2, regionSize/2, regionSize/2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = pos;
				expectedRegion = region;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} { // Normalize may move regions when on an edge further away.
				pos = new Vector3(3 * regionSize / 2, 3 * regionSize / 2, 3 * regionSize / 2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = -new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				expectedRegion = Vector3Int.one * 2;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}

		}
	}


}


