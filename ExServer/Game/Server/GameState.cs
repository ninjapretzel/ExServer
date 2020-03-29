using Ex;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternus {
	
	/// <summary> Database object for the primary user save data. </summary>
	[BsonIgnoreExtraElements]
	public class GameState : DBEntry {

		public JsonObject wallet;

		public JsonArray units;
		

	}
	
}
