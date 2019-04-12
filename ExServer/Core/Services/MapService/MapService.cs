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
	/// <summary> Service which creates and manages Map instances </summary>
	public class MapService : Service {

		/// <summary> Cached MapInfo from database </summary>
		public Dictionary<string, MapInfo> mapInfoByName;

		/// <summary> All map instances </summary>
		public ConcurrentDictionary<string, ConcurrentSet<string>> instances;

		/// <summary> Connected LoginService</summary>
		public LoginService loginService { get { return GetService<LoginService>(); } }
		/// <summary> Connected DBService</summary>
		public DBService dbService { get { return GetService<DBService>(); } }

		/// <summary> Local entity ID used for the client to ask for movement. </summary>
		public Guid? localGuid = null;
		

		public override void OnEnable() {
			mapInfoByName = new Dictionary<string, MapInfo>();
			
		}

		public override void OnDisable() {

		}

		/// <summary> RPC, Server -> CLient, Informs the client of their assigned GUID </summary>
		/// <param name="msg"> RPC Message info </param>
		public void SetGUID(Message msg) {
			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				localGuid = id;
			}
		}
		
		/// <summary> RPC, Server->Client, tells the client to move the entity to a position </summary>
		/// <param name="msg"> RPC Message info </param>
		public void RubberBand(Message msg) {
			// Vector3 pos = Unpack<Vector3>(msg[0]);
		}

	}

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
	public struct EntityID {
		/// <summary> Index in given array </summary>
		public int location;
		/// <summary> Version of entity </summary>
		public int version;
	}

	public struct EntityData {
		public EntityID id;
		public Map map;
		public bool live;
		public Vector3 position;
		public Vector4 rotation;
		public Vector3 velocity;
		public Vector3 angVelocity;
		public string name;
		public string model;
	}

	public struct EntityMoveRequest {
		public EntityID id;
		public Client client;
		public Vector3 oldPos;
		public Vector3 newPos;
		public bool serverMove;
	}

	/// <summary> A single instance of a map. </summary>
	public class Map {
		
		public MapInfo info;
		public Guid id;
		public List<Client> clients;
		public Dictionary<Vector3Int, Cell> cells;
		public EntityData[] entities;
		public MapService service;

		public bool is3d { get { return info.is3d; } }
		public string name { get { return info.name; } }
		public float cellSize { get { return info.cellSize; } }
		public int cellDist { get { return info.cellDist; } }

		public ConcurrentQueue<EntityID> toDespawn;
		public ConcurrentQueue<EntityMoveRequest> toMove;


		public Map(MapService service) {
			this.service = service;
			id = Guid.NewGuid();

			clients = new List<Client>();
			cells = new Dictionary<Vector3Int, Cell>();

			service.server.CreateUpdateThread(Update);

		}
		/// <summary> Update function, called in server update thread. </summary>
		/// <returns></returns>
		public bool Update() {

			return true;
		}

		/// <summary> Gets the coordinate of the cell that <paramref name="position"/> belongs to. </summary>
		/// <param name="position"> Position in worldspace </param>
		/// <returns> Coordinate of <paramref name="position"/> in cell space </returns>
		public Vector3Int CellPositionFor(Vector3 position) { return CellPositionFor(position, cellSize, is3d); }
		/// <summary> Gets the coordinate of the cell that <paramref name="position"/> belongs to, given a specific cellSize </summary>
		/// <param name="position"> Position in worldspace </param>
		/// <param name="cellSize"> Size of cells </param>
		/// <returns> Position of <paramref name="position"/> in cell space </returns>
		public static Vector3Int CellPositionFor(Vector3 position, float cellSize, bool is3d = false) {
			float halfSize = cellSize / 2f;
			Vector3 cell = position;
			if (!is3d) { cell.y = 0; }
			cell += Vector3.one * halfSize;
			cell /= cellSize;
			return Vector3Int.FloorToInt(cell);
		}
	}

	
}
