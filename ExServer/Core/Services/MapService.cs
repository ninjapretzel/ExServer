#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace Ex {
	public class EntityService : Service {

		#if !UNITY
		[BsonIgnoreExtraElements]
		public class MapInfo : DBEntry {

		}
		#endif

		public LoginService loginService { get { return GetService<LoginService>(); } }
		public override void OnEnable() {
			
		}

		public override void OnDisable() {

		}
		

	}
}
