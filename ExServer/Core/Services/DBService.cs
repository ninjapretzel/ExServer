#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
using UnityEngine;
#else

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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using Ex.Utils;
#endif

namespace Ex {

#if !UNITY
	/// <summary> Base class for any types being stored in the database. Standardizes access to the MongoDB "_id" property. </summary>
	public class DBEntry {
		/// <summary> MongoDB "_id" property for uniqueness </summary>
		[BsonId] public ObjectId id { get; set; }
		/// <summary> Relational guid </summary>
		public Guid guid { get; set; }
	}
#endif 

	/// <summary> Service type for holding database connection. Empty on client. </summary>
	public class DBService : Service {
#if !UNITY
		public static void RegisterSerializers() {
			BsonSerializer.RegisterSerializer<Rect>(new RectSerializer());
			BsonSerializer.RegisterSerializer<Bounds>(new BoundsSerializer());
			BsonSerializer.RegisterSerializer<Ray>(new RaySerializer());
			BsonSerializer.RegisterSerializer<Ray2D>(new Ray2DSerializer());
			BsonSerializer.RegisterSerializer<Plane>(new PlaneSerializer());
			BsonSerializer.RegisterSerializer<RectInt>(new RectIntSerializer());
			BsonSerializer.RegisterSerializer<Vector2>(new Vector2Serializer());
			BsonSerializer.RegisterSerializer<Vector2Int>(new Vector2IntSerializer());
			BsonSerializer.RegisterSerializer<Vector3>(new Vector3Serializer());
			BsonSerializer.RegisterSerializer<Vector3Int>(new Vector3IntSerializer());
			BsonSerializer.RegisterSerializer<Vector4>(new Vector4Serializer());
			
		}
		
		/// <summary> MongoDB Connection </summary>
		public MongoClient dbClient { get; private set; }
		/// <summary> Database to use by default </summary>
		public MDB defaultDB { get; private set; }
		public string dbName { get; private set; } = "debug";
		public bool cleanedDB { get; private set; } = false;
		
		public override void OnEnable() {
		}

		/// <summary> Connects the database to a given mongodb server. </summary>
		/// <param name="location"> Location to connect to, defaults to default mongodb port on localhost </param>
		public DBService Connect(string location = "mongodb://localhost:27017") {
			dbClient = new MongoClient(location);
			defaultDB = dbClient.GetDatabase("debug");
			return this;
		}

		/// <summary> Used to set the default database. </summary>
		public DBService UseDatabase(string dbName) {
			this.dbName = dbName;
			defaultDB = dbClient.GetDatabase(dbName);
			return this;
		}
		
		/// <summary> Cleans (drops) the current database </summary>
		public DBService CleanDatabase() {
			Log.Warning($"Be advised. Clearing Database {{{dbName}}}.");
			dbClient.DropDatabase(dbName);
			defaultDB = dbClient.GetDatabase(dbName);
			cleanedDB = true;
			return this;
		}

		/// <summary> Get a collection of stuff in the default database  </summary>
		/// <typeparam name="T"> Generic type of collection </typeparam>
		/// <returns> Collection of items, using the name of the type </returns>
		public IMongoCollection<T> Collection<T>() where T : DBEntry {
			return defaultDB.GetCollection<T>(typeof(T).Name);
		}
		/// <summary> Get a collection of stuff of a given type in a given database </summary>
		/// <typeparam name="T"> Generic type of collection </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <returns> Collection of items, using the name of the type </returns>
		public IMongoCollection<T> Collection<T>(string databaseName) where T : DBEntry {
			return dbClient.GetDatabase(databaseName).GetCollection<T>(typeof(T).Name);
		}

		/// <summary> Get a database entry by a general relational guid </summary>
		/// <typeparam name="T"> Generic type of DBEntry Table to get from </typeparam>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> Retrieved result matching the ID, or null </returns>
		public T Get<T>(Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.guid), id);
			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get a database entry by a general relational guid </summary>
		/// <typeparam name="T"> Generic type of DBEntry Table to get from </typeparam>
		/// <param name="idField"> ID Field to look for 'guid' within </param>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> Retrieved result matching the ID, or null </returns>
		public T Get<T>(string idField, Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(idField, id);
			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get an item from the default database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of item to get </typeparam>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> First item matching id, or null. </returns>
		public T Get<T>(string idField, string id) where T : DBEntry {
			// Todo: Benchmark and figure out which of these is faster
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get an item from the default database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of item to get </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> First item matching id, or null. </returns>
		public T Get<T>(string databaseName, string idField, string id) where T: DBEntry {
			// Todo: Benchmark and figure out which of these is faster
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			T result = Collection<T>(databaseName).Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Saves the given item into the default database. Updates the item, or inserts it if one does not exist yet.  </summary>
		/// <typeparam name="T"> Generic type of item to insert  </typeparam>
		/// <param name="item"> Item to insert </param>
		public void Save<T>(T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);

			var coll = Collection<T>();
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
		}

		/// <summary> Saves the given item into the default database. Updates the item, or inserts it if one does not exist yet.  </summary>
		/// <typeparam name="T"> Generic type of item to insert  </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <param name="item"> Item to insert </param>
		public void Save<T>(string databaseName, T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);

			var coll = Collection<T>(databaseName);
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
		}
#endif
	}
	
#if !UNITY
	public static class BsonHelpers {
		/// <summary> Used to evaluate a query to a BsonDocument object which can be used in most places in MongoDB's API </summary>
		/// <param name="query"> Object literal query </param>
		/// <returns> BsonDocument representing query </returns>
		public static BDoc Query(string query) {
			return MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BDoc>(query);
		}
	}
#endif
}
