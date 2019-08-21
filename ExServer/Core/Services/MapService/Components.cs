#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif
using Ex.Utils;
using System;

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
}
