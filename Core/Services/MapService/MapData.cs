#if UNITY_2017_1_OR_NEWER
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;

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



#if !UNITY
	/// <summary> DB info for data about a map, used to create and make decisions about a map instance </summary>
	public class MapInfo {

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
		public string name = "UnnamedMap";

		/// <summary> Is this map instanced? </summary>
		public bool instanced = false;
		/// <summary> Default number of instances to create, if this map is instanced. </summary>
		public int numInstances = 1;
		
		/// <summary> Is this map 3d? </summary>
		public bool is3d = false;
		/// <summary> Does this map show other players's entities? </summary>
		public bool solo = false;
		/// <summary> Does this map destroy cells (and entities) when not viewed by a client? </summary>
		public bool sparse = true;
		
		/// <summary> Axes of map to use if 2d. </summary>
		public Axes axes = Axes.XZ;
		/// <summary> Size of cells in arbitrary units. </summary>
		public float cellSize = 10.0f;
		/// <summary> Radius of visible surrounding cells in arbitrary units. </summary>
		public int cellDist = 2;
		/// <summary> Size of a "Region" in the map. Regions are used to circumvent floating point precision loss. </summary>
		public float regionSize = 2048f;
		
		/// <summary> Bounds of map space in arbitrary units. If bounds is zero'd, map is infinite. </summary>
		public Bounds bounds;
		/// <summary> Shape of bounds to use for edge of map </summary>
		public BoundsShape boundsShape = BoundsShape.Box;

		/// <summary> Entities in the map </summary>
		public EntityInstanceInfo[] entities = EMPTY;
		private static readonly EntityInstanceInfo[] EMPTY = new EntityInstanceInfo[0];
	}
	
	/// <summary> Entity information. </summary>
	public class EntityInfo {
		/// <summary> Kind of entity </summary>
		public string type;
		/// <summary> Is this a global (map-wide) entity? </summary>
		public bool global = false;
		/// <summary> Information about <see cref="Comp"/>s attached to entity </summary>
		public ComponentInfo[] components;
		private static readonly ComponentInfo[] EMPTY = new ComponentInfo[0];

	}

	/// <summary> Information about <see cref="Comp"/>s attached to an entity </summary>
	public class ComponentInfo {
		/// <summary> name of type of component </summary>
		public string type;
		/// <summary> Arbitrary Data to merge into component </summary>
		public JsonObject data;
	}

	/// <summary> <see cref="MapInfo"/> embedded entity information. 
	/// These are spawned into entities when the map starts. </summary>
	public class EntityInstanceInfo {
		/// <summary> Location of Entity </summary>
		public Vector3 position;
		/// <summary> Map "region", used to circumvent floating point precision loss</summary>
		public Vector3Int region;
		/// <summary> Rotation of Entity </summary>
		public Vector3 rotation;
		/// <summary> Scale of Entity </summary>
		public Vector3 scale;
		/// <summary> ID of entity <see cref="EntityInfo"/> object in database </summary>
		public string type;
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
	
	

}


