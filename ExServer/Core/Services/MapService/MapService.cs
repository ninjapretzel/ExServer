#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
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
using System.Runtime.CompilerServices;

namespace Ex {
	/// <summary> Service which creates and manages Map instances </summary>
	public class MapService : Service {

		#if !UNITY

		/// <summary> Connected LoginService</summary>
		public LoginService loginService { get { return GetService<LoginService>(); } }
		/// <summary> Connected DBService</summary>
		public DBService dbService { get { return GetService<DBService>(); } }
		/// <summary> Connected EntityService </summary>
		public EntityService entityService { get { return GetService<EntityService>(); } }

		/// <summary> Cached MapInfo from database </summary>
		public ConcurrentDictionary<string, MapInfo> mapInfoByName;

		/// <summary> All map instances </summary>
		public ConcurrentDictionary<string, List<Guid>> instances;

		/// <summary> Live map instances </summary>
		public ConcurrentDictionary<Guid, Map> maps;

		/// <summary> Work pool for map instances. </summary>
		public WorkPool<Map> mapWorkPool;
		#endif

		/// <summary> Position of locally controlled entity. </summary>
		public Vector3 localPosition = Vector3.zero;
		/// <summary> Rotation of locally controlled entity. </summary>
		public Vector4 localRotation = Vector4.zero;
		/// <summary> Local  </summary>
		public string localMapId = null;
		
		public override void OnEnable() {
			#if !UNITY
			if (isMaster) {
				mapInfoByName = new ConcurrentDictionary<string, MapInfo>();
				instances = new ConcurrentDictionary<string, List<Guid>>();
				maps = new ConcurrentDictionary<Guid, Map>();
				mapWorkPool = new WorkPool<Map>(UpdateMap);
			}
			#endif

		}

		public override void OnDisable() {
			#if !UNITY
			if (isMaster) {
				mapWorkPool.Finish();
			}
			#endif

		}


		#if !UNITY
		public void UpdateMap(Map map) {
			if (map.clients.Count > 0) {
				map.Update();
			}
		}
		public Map GetMap(string map, int? mapInstanceIndex = null) {

			//Log.Debug($"Loading map {map}");
			// Load the map from db, or the limbo map if it doesn't exist.
			if (!mapInfoByName.ContainsKey(map)) {
				var loadedMap = dbService.Get<MapInfo>("Content", "name", map) ?? dbService.Get<MapInfo>("Content", "name", "Limbo");
				mapInfoByName[map] = loadedMap;
				string s = loadedMap?.ToString() ?? "NULL";
				Log.Debug($"Loaded MapInfo for {map}");
			}

			// A requested map may have been routed to limbo if it does not exist.
			MapInfo info = mapInfoByName[map];
			string mapName = info.name;

			// First time? initialize instances collection of map 
			if (!instances.ContainsKey(mapName)) {
				Initialize(info);
			}
			List<Guid> instanceIds = instances[mapName];
			
			if (mapInstanceIndex == null) {
				return maps[instanceIds[0]];
			}

			int ind = mapInstanceIndex.Value % instanceIds.Count;
			if (ind < 0) { ind *= -1; }
			var id = instanceIds[ind];
			
			return maps[id];
			
		}

		/// <summary> Initialize default set of Map instances for a MapInfo </summary>
		/// <param name="info"> Info to initialize </param>
		private void Initialize(MapInfo info) {
			Log.Info($"Initializing MapInfo for {info.name}. {(info.instanced ? info.numInstances : 1)} instances");
			if (info.instanced) {
				for (int i = 0; i < info.numInstances; i++) {
					SpinUp(info, i);
				}

			} else {
				SpinUp(info);
			}
		}
		
		/// <summary> Initializes a single instance from a <see cref="MapInfo"/>. </summary>
		/// <param name="info"> Data to use to initialize map instance </param>
		/// <returns> Newly created map instance</returns>
		private Map SpinUp(MapInfo info, int? instanceIndex = null) {
			Map map = new Map(this, info, instanceIndex);

			maps[map.id] = map;
			List<Guid> instanceIds = instances.ContainsKey(info.name) 
				? instances[info.name] 
				: (instances[info.name] = new List<Guid>());

			instanceIds.Add(map.id);
			mapWorkPool.Add(map);

			return map;
		}
		#endif

		// Server-side logic
#if !UNITY
		/// <summary> Server Command, used when transferring a client into a map. </summary>
		/// <param name="client"> Client connection object </param>
		/// <param name="mapId"> ID to put client into </param>
		public void EnterMap(Client client, string mapId, Vector3? position = null, Vector4? rotation = null, int? mapInstanceIndex = null) {
			
			Log.Info($"\\jClient {client.identity} entering map {mapId} ");
			var map = GetMap(mapId, mapInstanceIndex);

			Log.Info($"\\jGot map { map.id }");
			map.EnterMap(client);
			if (position != null || rotation != null) {
				map.Move(client.id, position, rotation);
			}
			
			


			
			

		}
		#endif

	}

	
}
