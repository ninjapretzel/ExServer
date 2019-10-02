#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
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
using System.IO;
using Ex.Data;
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
		// Todo: find these and register them automatically
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
			
			BsonSerializer.RegisterSerializer<InteropFloat64>(new InteropFloat64Serializer());
			BsonSerializer.RegisterSerializer<InteropFloat32>(new InteropFloat32Serializer());
			BsonSerializer.RegisterSerializer<InteropString32>(new InteropString32Serializer());
			BsonSerializer.RegisterSerializer<InteropString256>(new InteropString256Serializer());

			BsonSerializer.RegisterSerializer<UberData>(new UberDataSerializer());
			BsonSerializer.RegisterSerializer<SimplexNoise>(new SimplexNoiseSerializer());
		}
		
		/// <summary> MongoDB Connection </summary>
		public MongoClient dbClient { get; private set; }
		/// <summary> Database to use by default </summary>
		public MDB defaultDB { get; private set; }
		public string dbName { get; private set; } = "debug";
		public bool cleanedDB { get; private set; } = false;
		
		public override void OnEnable() {
		}

		private static string ForwardSlashPath(string str) {
			return str.Replace('\\', '/');
		}

		/// <summary> Reseeds the DB service, given it is connected, with instructions in a given directory. </summary>
		/// <param name="dir"> Directory to reseed from </param>
		public void Reseed(string dir) {
			dir = ForwardSlashPath(dir);
			if (!dir.EndsWith("/")) { dir += "/"; }
			try {
				string json = File.ReadAllText(dir + "seed.json");
				JsonValue v = Json.Parse(json);
				
				if (v is JsonArray) {
					Reseed(v as JsonArray, dir);
				} else if (v is JsonObject) {
					Reseed(v as JsonObject, dir);
				}
				
			} catch (Exception e) {
				Log.Error($"Error while seeding database from [{dir}]", e);
			}
		}

		/// <summary> Reseed using all of the given descriptors. </summary>
		/// <param name="descriptors"> JsonArray of JsonObjects describing how to reseed the database. </param>
		/// <param name="dir"> Base directory to reseed from. </param>
		public void Reseed(JsonArray descriptors, string dir = null) {
			foreach (var it in descriptors) {
				if (it is JsonObject) { Reseed(it as JsonObject, dir); }
			}
		}

		/// <summary> Reseeds a database using the given descriptor. </summary>
		/// <param name="descriptor"> JsonObject containing description of how to reseed the database. </param>
		public void Reseed(JsonObject descriptor, string dir = null) {
			if (descriptor.Has<JsonArray>("drop")) { Reseed_Drop(descriptor.Get<JsonArray>("drop")); }
			if (descriptor.Has<JsonObject>("index")) { Reseed_Index(descriptor.Get<JsonObject>("index")); }
			if (descriptor.Has<JsonObject>("insert")) { Reseed_Insert(descriptor.Get<JsonObject>("insert"), dir); }
			
		}

		private void Reseed_Drop(JsonArray databases) {
			foreach (var dbname in databases) {
				if (dbname.isString) {
					dbClient.DropDatabase(dbname.stringVal);
				}
			}

		}
		private void Reseed_Index(JsonObject descriptor) {
			string database = descriptor.Pull("database", dbName);
			string collection = descriptor.Pull("collection", "Garbage");
			if (database == "$default") { database = dbName; }

			JsonObject fields = descriptor.Pull<JsonObject>("fields");
			MDB db = dbClient.GetDatabase(database);
			
			List<CreateIndexModel<BDoc>> indexes = new List<CreateIndexModel<BDoc>>();
			IndexKeysDefinition<BDoc> index = null;
			foreach (var pair in fields) {
				string fieldName = pair.Key;
				int order = pair.Value;
				
				if (order > 0) {
					index = index?.Ascending(fieldName) ?? Builders<BDoc>.IndexKeys.Ascending(fieldName);
				} else {
					index = index?.Descending(fieldName) ?? Builders<BDoc>.IndexKeys.Descending(fieldName);
				}
			}
			
			var model = new CreateIndexModel<BDoc>(index);
			db.GetCollection<BDoc>(collection).Indexes.CreateOne(model);
		}

		private void Reseed_Insert(JsonObject descriptor, string dir) {
			string database = descriptor.Pull("database", dbName);
			string collection = descriptor.Pull("collection", "Garbage");
			
			string[] files = descriptor.Pull<string[]>("files");
			if (files == null) { files = new string[] { collection }; }

			dir = ForwardSlashPath(dir);
			if (!dir.EndsWith("/")) { dir += "/"; }

			foreach (var file in files) {
				string json = null;
				string fpath = dir + file;

				if (file.EndsWith("/**")) {
					string directory = fpath.Replace("/**", "");

					Reseed_Insert_Glob(database, collection, directory);

				} else {
					try { json = json ?? File.ReadAllText(fpath); } catch (Exception) { }
					try { json = json ?? File.ReadAllText(fpath + ".json"); } catch (Exception) { }
					try { json = json ?? File.ReadAllText(fpath + ".wtf"); } catch (Exception) { }

					if (json == null) {
						Log.Warning($"Seeder could not find file {{{file}}} under {{{dir}}}");
						continue;
					}

					JsonValue data = Json.Parse(json);
					if (data == null || !(data is JsonObject) && !(data is JsonArray)) {
						Log.Warning($"Seeder cannot use {{{file}}} under {{{dir}}}, it is not an object or array.");
						continue;
					}

					if (data is JsonObject) {
						InsertData(database, collection, data as JsonObject);
					} else if (data is JsonArray) {
						InsertData(database, collection, data as JsonArray);
					}
				}


			}
		}

		private void Reseed_Insert_Glob(string database, string collection, string directory) {
			List<string> files = AllFilesInDirectory(directory);
			foreach (string file in files) {
				string json = null;
				try {
					json = json ?? File.ReadAllText(file);
				} catch (Exception e) {
					Log.Warning($"Seeder could not find {{{file}}}.", e);
				}

				JsonValue data = Json.Parse(json);
				if (data == null || !(data is JsonObject) && !(data is JsonArray)) {
					Log.Warning($"Seeder cannot use {{{file}}}, it is not an object or array.");
					continue;
				}

				if (data is JsonObject) {
					InsertData(database, collection, data as JsonObject);
				} else if (data is JsonArray) {
					InsertData(database, collection, data as JsonArray);
				}
			}
		}

		private List<string> AllFilesInDirectory(string directory, List<string> collector = null) {
			collector = collector ?? new List<string>();
			var files = Directory.GetFiles(directory);
			collector.AddRange(files);
			//collector.AddRange(files.Select(it => ForwardSlashPath(it)));
			
			var dirs = Directory.GetDirectories(directory);
			foreach (var dir in dirs) {
				AllFilesInDirectory(dir, collector);
			}

			return collector;
		}

		/// <summary> Creates a <see cref="BDoc"/> out of every <see cref="JsonObject"/> in <paramref name="data"/>, and inserts each as a new record in the given <paramref name="database"/> and <paramref name="collection"/>. </summary>
		/// <param name="database"> Database to add data to </param>
		/// <param name="collection"> Collection to add data to </param>
		/// <param name="data"> Data to insert insert </param>
		public void InsertData(string database, string collection, JsonArray vals) {
			List<BDoc> docs = new List<BDoc>(vals.Count);

			foreach (var data in vals) {
				if (data is JsonObject) {
					docs.Add(ToBson(data as JsonObject));
					// InsertData(database, collection, data as JsonObject);
				}
			}

			MDB db = dbClient.GetDatabase(database);
			db.GetCollection<BDoc>(collection).InsertMany(docs);
		}

		/// <summary> Creates a <see cref="BDoc"/> out of <paramref name="data"/>, and inserts a new record in the given <paramref name="database"/> and <paramref name="collection"/>. </summary>
		/// <param name="database"> Database to add data to </param>
		/// <param name="collection"> Collection to add data to </param>
		/// <param name="data"> Data to turn into a BDoc and insert </param>
		public void InsertData(string database, string collection, JsonObject data) {
			BDoc doc = ToBson(data);
			
			MDB db = dbClient.GetDatabase(database);
			db.GetCollection<BDoc>(collection).InsertOne(doc);

		}

		/// <summary> Converts a <see cref="JsonObject"/> into a <see cref="BDoc"/></summary>
		/// <param name="data"> Data object to convert </param>
		/// <returns> Converted data </returns>
		private BDoc ToBson(JsonObject data) {
			BDoc doc = new BDoc();

			foreach (var pair in data) {
				string key = pair.Key.stringVal;
				JsonValue value = pair.Value;

				if (value.isNumber) { doc[key] = value.doubleVal; }
				else if (value.isString) { doc[key] = value.stringVal; }
				else if (value.isBool) { doc[key] = value.boolVal; }
				else if (value is JsonObject) { doc[key] = ToBson(value as JsonObject); } 
				else if (value is JsonArray) { doc[key] = ToBson(value as JsonArray); }
				else if (value.isNull) { doc[key] = BsonNull.Value; }

			}

			return doc;
		}

		/// <summary> Converts a <see cref="JsonArray"/> into a <see cref="BsonArray"/></summary>
		/// <param name="data"> Data object to convert </param>
		/// <returns> Converted data </returns>
		private BsonArray ToBson(JsonArray data) {
			BsonArray arr = new BsonArray(data.Count);

			foreach (var value in data) {
				
				if (value.isNumber) { arr.Add(value.doubleVal); }
				else if (value.isString) { arr.Add(value.stringVal); }
				else if (value.isBool) { arr.Add(value.boolVal); }
				else if (value is JsonObject) { arr.Add(ToBson(value as JsonObject)); } 
				else if (value is JsonArray) { arr.Add(ToBson(value as JsonArray)); }
				else if (value.isNull) { arr.Add(BsonNull.Value); }

			}
			
			return arr;
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
		public T Get<T>(string databaseName, string idField, string id) where T : DBEntry {
			// Todo: Benchmark and figure out which of these is faster
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			T result = Collection<T>(databaseName).Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get all items from the default database, where the given ID field matches the given ID. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="idField"> ID Field to look for ID within </param>
		/// <param name="id"> ID to look for ID </param>
		/// <returns> All elements matching the given ID </returns>
		/// <remarks> For example, if `Item` has a field `owner:string`, this can be used to find all `Item`s owned by a given entity. </remarks>
		public List<T> GetAll<T>(string idField, Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(idField, id);
			List<T> result = Collection<T>().Find(filter).ToList();
			return result;
		}

		/// <summary> Get all items from the default database, where the given ID field matches the given ID. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="idField"> ID Field to look for 'guid' within </param>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> All elements matching the given ID </returns>
		/// <remarks> For example, if `Item` has a field `owner:Guid`, this can be used to find all `Item`s owned by a given entity. </remarks>

		public List<T> GetAll<T>(string idField, string id) where T : DBEntry {
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			List<T> result = Collection<T>().Find(filter).ToList();
			return result;
		}

		/// <summary> Get an Enumerable from the given database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="databaseName"> Name of database to sample  </param>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> All items with matching id, or an empty list </returns>
		public List<T> GetAll<T>(string databaseName, string idField, string id) where T: DBEntry {
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			List<T> result = Collection<T>(databaseName).Find(filter).ToList();
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
