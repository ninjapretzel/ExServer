#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif

using Ex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

#if !UNITY

namespace Ex {
	
	/// <summary> A single instance of a map. </summary>
	public class Map {

		/// <summary> Service the map is bound to. Used to facilitate interactions with the <see cref="EntityService"/> and other functionality </summary>
		public MapService service { get; private set; }
		/// <summary> Information about this map. </summary>
		public MapInfo info { get; private set; }
		/// <summary> ID assigned to map </summary>
		public Guid id { get; private set; }
		/// <summary> Clients in the map </summary>
		public List<Client> clients { get; private set; }
		/// <summary> Cells in the map </summary>
		public Dictionary<Vector3Int, Cell> cells { get; private set; }
		/// <summary> Entities in the map </summary>
		public Dictionary<Guid, Entity> entities { get; private set; }
		/// <summary> All ids in the map. </summary>
		public List<Guid> idsInMap { get; private set; }
		
		public EntityService entityService { get { return service.GetService<EntityService>(); } }

		public bool is3d { get { return info.is3d; } }
		public string name { get { return info.name; } }
		/// <summary> Instance index of this map. </summary>
		public int instanceIndex { get; private set; }
		public float cellSize { get { return info.cellSize; } }
		public int cellDist { get { return info.cellDist; } }

		private ConcurrentQueue<Guid> toDespawn;
		private ConcurrentQueue<EntityMoveRequest> toMove;

		/// <summary> Time Taken during spawning part of update tick </summary>
		public Trender spawnTrend = new Trender();
		/// <summary> Time Taken during despawning part of update tick </summary>
		public Trender despawnTrend = new Trender();
		/// <summary> Time Taken during entity update part of update tick </summary>
		public Trender updateTrend = new Trender();
		/// <summary> Time Taken during collision part of update tick </summary>
		public Trender collideTrend = new Trender();

		public Map(MapService service, MapInfo info, int? instanceIndex = null) {
			this.service = service;
			this.info = info;
			id = Guid.NewGuid();
			this.instanceIndex = instanceIndex ?? 0;
			clients = new List<Client>();
			cells = new Dictionary<Vector3Int, Cell>();
			entities = new Dictionary<Guid, Entity>();
			idsInMap = new List<Guid>();
			toDespawn = new ConcurrentQueue<Guid>();
			toMove = new ConcurrentQueue<EntityMoveRequest>();
			
		}


		private Stopwatch sw = new Stopwatch();
		/// <summary> Update function, called in server update thread. </summary>
		/// <returns></returns>
		public bool Update() {
			
			sw.Start();
			UpdateEntities();
			sw.Stop();
			updateTrend.Record(sw.ElapsedMilliseconds);
			sw.Reset();

			long collideTime = 0;
			foreach (var pair in cells) {
				Cell cell = pair.Value;
				sw.Start();
				CollideCell(cell);
				sw.Stop();
				collideTime += sw.ElapsedMilliseconds;
				sw.Reset();
			}
			collideTrend.Record(collideTime);

			sw.Start();
			if (!toDespawn.IsEmpty) {
				Guid id;
				while (toDespawn.TryDequeue(out id)) {
					if (entities.ContainsKey(id)) {
						OnDespawn(id);
						entities.Remove(id);
					}

				}
				
			}
			sw.Reset();

			return true;
		}

		private void UpdateEntities() {

		}

		private void CollideCell(Cell cell) {

		}

		public void Move(Guid entityId, Vector3? position, Vector4? rotation, bool serverMove = false) {
			EntityMoveRequest move = new EntityMoveRequest();
			move.id = entityId;
			TRS trs = entityService.GetComponent<TRS>(entityId);
			if (trs == null) {
				entityService.AddComponent<TRS>(entityId);
				serverMove = true;
			}

			move.oldPos = trs.position;
			move.oldRot = trs.rotation;
			move.newPos = position ?? trs.position;
			move.newRot = rotation ?? trs.rotation;
			move.serverMove = serverMove;

			toMove.Enqueue(move);
		}

		public void EnterMap(Client c) {
			Entity entity = entityService[c.id];

			var onMap = entity.RequireComponent<OnMap>();
			if (onMap != null && onMap.mapId != null) {
				var oldMap = service.GetMap(onMap.mapId, onMap.mapInstanceIndex);
				oldMap.ExitMap(c);
			}

			onMap.mapId = name;
			onMap.mapInstanceIndex = instanceIndex;

		}

		public void ExitMap(Client c) {

			// toDespawn.Enqueue(c.id);
			

		}

		private void OnDespawn(Guid id) {

		}
		
		public IEnumerable<T> All<T>() where T : Comp {
			return entityService.GetEntities<T>() as IEnumerable<T>;
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
#endif
