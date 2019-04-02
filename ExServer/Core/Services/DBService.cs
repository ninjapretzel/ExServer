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
#endif

namespace Ex {
	public class DBService : Service {
		
		#if !UNITY
		// Server side code.
		public MongoClient dbClient;
		public MDB defaultDB;
		public override void OnEnable() {
			
		}

		/// <summary> Connects the database to a given mongodb server. </summary>
		/// <param name="location"> Location to connect to, defaults to default mongodb port on localhost </param>
		public void Connect(string location = "mongodb://localhost:27017") {
			dbClient = new MongoClient(location);
		}

		/// <summary> Used to set the default database. </summary>
		public void UseDatabase(string dbName) {
			defaultDB = dbClient.GetDatabase(dbName);
		}
		

		
		public IMongoCollection<T> Collection<T>(string collectionName) {
			return defaultDB.GetCollection<T>(collectionName);
		}

		public IMongoCollection<T> Collection<T>(string databaseName, string collectionName) {
			return dbClient.GetDatabase(databaseName).GetCollection<T>(collectionName);
		}

		public T Get<T>(string collectionName, string idField, string id) {
			var builder = Builders<T>.Filter.AnyEq(idField, new BsonString(id) );
			return defaultDB.GetCollection<T>(collectionName).Find(builder).FirstOrDefault();
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
