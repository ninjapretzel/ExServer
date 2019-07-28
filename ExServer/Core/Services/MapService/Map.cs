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

		public MapInfo info;
		public Guid id;
		public List<Client> clients;
		public Dictionary<Vector3Int, Cell> cells;
		public Dictionary<Guid, Entity> entities;
		
		public MapService service;

		public bool is3d { get { return info.is3d; } }
		public string name { get { return info.name; } }
		public float cellSize { get { return info.cellSize; } }
		public int cellDist { get { return info.cellDist; } }

		private ConcurrentQueue<Guid> toDespawn;
		private ConcurrentQueue<EntityMoveRequest> toMove;

		public Trender spawnTrend = new Trender();
		public Trender despawnTrend = new Trender();
		public Trender updateTrend = new Trender();
		public Trender collideTrend = new Trender();

		public Map(MapService service) {
			this.service = service;
			id = Guid.NewGuid();

			clients = new List<Client>();
			cells = new Dictionary<Vector3Int, Cell>();
			entities = new Dictionary<Guid, Entity>();
			toDespawn = new ConcurrentQueue<Guid>();
			toMove = new ConcurrentQueue<EntityMoveRequest>();

			service.server.CreateUpdateThread(Update);
			

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

			long total = 0;
			foreach (var pair in cells) {
				Cell cell = pair.Value;
				sw.Start();
				CollideCell(cell);
				sw.Stop();
				total += sw.ElapsedMilliseconds;
			}
			sw.Reset();

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

		public void EnterMap(Client c) {

		}

		public void ExitMap(Client c) {

		}

		private void OnDespawn(Guid id) {

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
