using Ex;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poly {
	/// <summary> Database object for the primary user save data. </summary>
	[BsonIgnoreExtraElements]
	public class GameState : DBData {
		public GameState() : base() {
			data["flags"] = new JsonObject();
			data["levels"] = new JsonObject();
			data["exp"] = new JsonObject();
			data["wallet"] = new JsonObject();
			data["rolls"] = new JsonObject();
		}

		public JsonObject flags { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject levels { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject exp { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject rolls { get { return data.Get<JsonObject>(MemberName()); } }
		
		public string color { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		public string skin { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		public float flat { get { return data.Get<float>(MemberName()); } set { data[MemberName()] = value; } }
		public float curve { get { return data.Get<float>(MemberName()); } set { data[MemberName()] = value; } }


	}

	/// <summary> Database object to store changing resources. 
	/// These would be kept and modifed primarily in memory, 
	/// but have changes journaled every few seconds. </summary>
	[BsonIgnoreExtraElements]
	public class UserResources : DBData { }

	/// <summary> Database object to store player stats. </summary>
	[BsonIgnoreExtraElements]
	public class UnitStats : DBData {
		public UnitStats() : base() {
			data["baseStats"] = new JsonObject();
			data["combatStats"] = new JsonObject();
		}

		public int level { get { return baseStats.Get<int>(MemberName()); } set { baseStats[MemberName()] = value; } }

		public JsonObject baseStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject combatStats { get { return data.Get<JsonObject>(MemberName()); } }
	}



}
