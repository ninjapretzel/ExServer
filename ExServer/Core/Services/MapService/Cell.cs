#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else
using MongoDB.Bson.Serialization.Attributes;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ex.Utils;

namespace Ex {
	/// <summary> Support class for <see cref="Map"/> to divide a map into cells. </summary>
	public class Cell {
		/// <summary> Entities in the cell </summary>
		public List<EntityID> entities { get; private set; }
		/// <summary> Clients connected to the cell </summary>
		public List<Client> clients { get; private set; }
		/// <summary> Cached cell visibility </summary>
		private List<Vector3Int> _visibility = null;
		/// <summary> All visible Cells from this Cell </summary>
		public IEnumerable<Vector3Int> visibility {
			get {
				return (_visibility == null)
				  ? (_visibility = (map.is3d ? Visibility3d(map.cellDist) : Visibility2d(map.cellDist)))
				  : _visibility;
			}
		}
		/// <summary> Cell position in the given map </summary>
		public Vector3Int cellPos { get; private set; }
		/// <summary> Map this cell belongs to </summary>
		public Map map { get; private set; }

		public Cell(Map map, Vector3Int cellPos) {
			this.map = map;
			this.cellPos = cellPos;
			entities = new List<EntityID>();
			clients = new List<Client>();
		}

		/// <summary> Provides a list of 2d cell neighbors for a given <paramref name="position"/> and <paramref name="maxDist"/> </summary>
		/// <param name="position"> Center position to get neighbors for </param>
		/// <param name="maxDist"> Radius of valid neighbors, in cell distance. Defaults to 1, which is enough for a 3x3 grid around the cell. </param>
		/// <returns> Collection of neighbors </returns>
		public List<Vector3Int> Visibility2d(int maxDist = 1) {
			List<Vector3Int> vis = new List<Vector3Int>();
			maxDist = Mathf.Abs(maxDist);
			Vector3Int center = cellPos;

			for (int z = -maxDist; z <= maxDist; z++) {
				for (int x = -maxDist; x <= maxDist; x++) {
					vis.Add(center + new Vector3Int(x, 0, z));
				}
			}

			return vis;
		}

		/// <summary> Provides a list of 3d cell neighbors for a given <paramref name="position"/> and <paramref name="maxDist"/> </summary>
		/// <param name="position"> Center position to get neighbors for </param>
		/// <param name="maxDist"> Radius of valid neighbors, in cell distance. Defaults to 1, which is enough for a 3x3x3 grid around the cell. </param>
		/// <returns> Collection of neighbors </returns>
		public List<Vector3Int> Visibility3d(int maxDist = 1) {
			List<Vector3Int> vis = new List<Vector3Int>();
			maxDist = Mathf.Abs(maxDist);
			Vector3Int center = cellPos;

			for (int z = -maxDist; z < maxDist; z++) {
				for (int y = -maxDist; y < maxDist; y++) {
					for (int x = -maxDist; x < maxDist; x++) {
						vis.Add(center + new Vector3Int(x, y, z));
					}
				}
			}

			return vis;
		}

	}
}
