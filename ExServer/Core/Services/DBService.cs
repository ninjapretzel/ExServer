#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using MongoDB.Driver;
using MongoDB.Bson;
using BDoc = MongoDB.Bson.BsonDocument;
using MDB = MongoDB.Driver.IMongoDatabase;
using Coll = MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>;
using MongoDB.Bson.Serialization.Attributes;
#endif

namespace Ex {

#if !UNITY
	public class DBEntry {
		[BsonId] public ObjectId id { get; set; }
	}
#endif
	public class DBService : Service {
		
#if !UNITY
		// Server side code.
		public MongoClient dbClient;
		public MDB defaultDB;
		public override void OnEnable() {
			
		}

		/// <summary> Connects the database to a given mongodb server. </summary>
		/// <param name="location"> Location to connect to, defaults to default mongodb port on localhost </param>
		public DBService Connect(string location = "mongodb://localhost:27017") {
			dbClient = new MongoClient(location);
			return this;
		}

		/// <summary> Used to set the default database. </summary>
		public DBService UseDatabase(string dbName) {
			defaultDB = dbClient.GetDatabase(dbName);
			return this;
		}
		

		
		public IMongoCollection<T> Collection<T>(string collectionName) where T : DBEntry {
			return defaultDB.GetCollection<T>(collectionName);
		}

		public IMongoCollection<T> Collection<T>(string databaseName, string collectionName) where T : DBEntry {
			return dbClient.GetDatabase(databaseName).GetCollection<T>(collectionName);
		}

		public T Get<T>(string collectionName, string idField, string id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(idField, id);
			return defaultDB.GetCollection<T>(collectionName).Find(filter).FirstOrDefault();
		}

		public void Save<T>(string collectionName, T item) where T : DBEntry {
			var filter = Builders<T>.Filter.AnyEq(nameof(DBEntry.id), item.id);
			var coll = defaultDB.GetCollection<T>(collectionName);
			var check = coll.Find(filter).FirstOrDefault();
			
			try {
				if (check == null) {
					coll.InsertOne(item);
				} else {
					var result = coll.ReplaceOne(filter, item);
				}

			} catch (Exception e) {
				Log.Error("Failed to save database entry", e);
			}

			// defaultDB.GetCollection<T>(collectionName).UpdateOne(, item);
		}


#endif


	}

#if !UNITY
	public static class BsonHelpers {
		public static BDoc Query(string query) {
			return MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BDoc>(query);
		}
	}
#endif
}
