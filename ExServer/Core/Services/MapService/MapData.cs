#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
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
		/// <summary> Is 3d? </summary>
		public bool is3d { get; set; }
		/// <summary> Size of cells </summary>
		public float cellSize { get; set; }
		/// <summary> Radius of visible surrounding cells </summary>
		public int cellDist { get; set; }
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
		public Vector3 oldPos;
		public Vector3 newPos;
		public bool serverMove;
	}

}


