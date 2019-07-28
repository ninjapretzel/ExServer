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
		/// <summary> Cached MapInfo from database </summary>
		public Dictionary<string, MapInfo> mapInfoByName;

		/// <summary> All map instances </summary>
		public ConcurrentDictionary<string, ConcurrentSet<string>> instances;

		/// <summary> Connected LoginService</summary>
		public LoginService loginService { get { return GetService<LoginService>(); } }
		/// <summary> Connected DBService</summary>
		public DBService dbService { get { return GetService<DBService>(); } }

		#endif

		/// <summary> Local entity ID used for the client to ask for movement. </summary>
		public Guid? localGuid = null;
		

		public override void OnEnable() {
			#if !UNITY
			mapInfoByName = new Dictionary<string, MapInfo>();
			#endif

		}

		public override void OnDisable() {

		}

		/// <summary> RPC, Server -> CLient, Informs the client of their assigned GUID </summary>
		/// <param name="msg"> RPC Message info </param>
		public void SetGUID(RPCMessage msg) {
			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				localGuid = id;
			}
		}

		public void EnterMap(Client client, string mapId) {
			
		}

		
		/// <summary> RPC, Server->Client, tells the client to move the entity to a position </summary>
		/// <param name="msg"> RPC Message info </param>
		public void RubberBand(RPCMessage msg) {
			// Vector3 pos = Unpack<Vector3>(msg[0]);
		}

	}

	
}
