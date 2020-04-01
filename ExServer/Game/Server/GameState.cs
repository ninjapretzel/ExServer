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
	public class GameState : DBData { 
		public JsonObject flags { get { return data.Get<JsonObject>(MemberName()); } }	
	}

	/// <summary> Database object to store changing resources. 
	/// These would be kept and modifed primarily in memory, 
	/// but have changes journaled every few seconds. </summary>
	[BsonIgnoreExtraElements]
	public class UserResources : DBData { }

	/// <summary> Database object to store player stats. </summary>
	[BsonIgnoreExtraElements]
	public class UserStats : DBData { }
	
		

	
}
