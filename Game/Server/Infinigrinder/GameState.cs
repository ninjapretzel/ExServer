using Ex;
using Ex.Utils;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infinigrinder {
	
	/// <summary> Database object for the primary user save data. </summary>
	[BsonIgnoreExtraElements]
	public class GameState : DBData, EntityService.UserEntityInfo {

		public string map { get; set; }
		public Vector3 position { get; set; }
		public Vector3 rotation { get; set; }
		[BsonIgnoreIfNull] public string color { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		[BsonIgnoreIfNull] public string color2 { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		[BsonIgnoreIfNull] public string color3 { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		[BsonIgnoreIfNull] public string color4 { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		[BsonIgnoreIfNull] public string skin { get { return data.Get<string>(MemberName()); } set { data[MemberName()] = value; } }
		
		public GameState() : base() { 
			data["flags"] = new JsonObject();
			data["levels"] = new JsonObject();
			data["exp"] = new JsonObject();
			data["wallet"] = new JsonObject();
		}

		public JsonObject flags { get { return data.Get<JsonObject>(MemberName()); } }	
		public JsonObject levels { get { return data.Get<JsonObject>(MemberName()); } }	
		public JsonObject exp { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject wallet { get { return data.Get<JsonObject>(MemberName()); } }

		
	}

	/// <summary> Database object to store changing resources. 
	/// These would be kept and modifed primarily in memory, 
	/// but have changes journaled every few seconds. </summary>
	[BsonIgnoreExtraElements]
	public class Inventory : DBData { }

	/// <summary> Database object to store unit stat data, for both saved units, and mob information. </summary>
	[BsonIgnoreExtraElements]
	public class UnitRecord : DBData { 
		public Guid owner;
		
		public UnitRecord() : base() { 
			data["baseStats"] = new JsonObject();
			data["combatStats"] = new JsonObject();
		}
		/// <summary> Base stat values </summary>
		public JsonObject baseStats { get { return data.Get<JsonObject>(MemberName()); } }
		public JsonObject combatStats { get { return data.Get<JsonObject>(MemberName()); } }
	}
	
		

	
}
