using BakaTest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BakaDB {

	/// <summary> Access point/locator static class for self-contained database module </summary>
	public static class DB {

		/// <summary> Local In-Memory databases cached by path. </summary>
		public static IDictionary<string, LocalDB> dbs = new ConcurrentDictionary<string, LocalDB>();
		/// <summary> Raw <see cref="LocalDB"/>s cached by <see cref="Type"/> </summary>
		public static IDictionary<Type, LocalDB> dbsByType = new ConcurrentDictionary<Type, LocalDB>();
		/// <summary> Wrapper <see cref="LocalDB{T}"/>s cached by <see cref="Type"/> </summary>
		public static IDictionary<Type, object> genericDBsByType = new ConcurrentDictionary<Type, object>();

		/// <summary> Helper function to locate a database by name, and make sure multiple instances of the same local database are not created. </summary>
		/// <param name="path"> Path to open </param>
		/// <param name="compress"> Whether or not to compress data (if the database must be created. Default = false) </param>
		/// <returns> Database associated with path </returns>
		public static LocalDB Local(string path = "documents", string extension = "wtf", bool readOnly = false, bool compress = false) {
			if (dbs.ContainsKey(path)) { return dbs[path]; }
			LocalDB db = new LocalDB(path, extension, readOnly, compress);
			dbs[path] = db;
			return db;
		}

		/// <summary> Helper function to locate a database by type, and make sure multiple instances of the same local database are not created. </summary>
		/// <typeparam name="T"> Generic type to use to locate database. </typeparam>
		/// <param name="extension"> Optional extension to use when creating the database.  </param>
		/// <param name="readOnly"> Whether or not to create the database in read-only mode (if it does not exist. Default = false) </param>
		/// <param name="compress"> Whether or not to compress data (if the database must be created. Default = false) </param>
		/// <returns></returns>
		public static LocalDB<T> Local<T>(string extension = "wtf", bool readOnly = false, bool compress = false) {
			Type t = typeof(T);
			if (genericDBsByType.ContainsKey(t)) { return (LocalDB<T>)genericDBsByType[t]; }
			string path = NameOf(t);
			if (dbs.ContainsKey(path)) { return (LocalDB<T>)(genericDBsByType[t] = new LocalDB<T>((dbsByType[t] = dbs[path]))); }
			LocalDB db = new LocalDB(path, extension, readOnly, compress);
			dbs[path] = db;
			return (LocalDB<T>)(genericDBsByType[t] = new LocalDB<T>(db));
		}

		/// <summary> Reduces a generic type to a string. </summary>
		/// <typeparam name="T"> Generic type to reduce </typeparam>
		/// <returns> String representing the type </returns>
		public static string NameOf<T>() { return NameOf(typeof(T)); }
		/// <summary> Reduces a generic type to a string. </summary>
		/// <param name="t"> Type to reduce </typeparam>
		/// <returns> String representing the type </returns>
		public static string NameOf(Type t) {
			if (!t.IsGenericType) { return t.Name; }
			if (t.IsArray) { return t.Name + "[]"; }

			Type[] innerTypes = t.GetGenericArguments();
			StringBuilder str = new StringBuilder();
			string shortName = t.Name;
			if (shortName.Contains('`')) {
				shortName = shortName.Substring(0, shortName.IndexOf('`'));
			}
			str.Append(shortName);
			str.Append('{');
			for (int i = 0; i < innerTypes.Length; i++) {
				str.Append(NameOf(innerTypes[i]));
				if (i < innerTypes.Length - 1) { str.Append(", "); }
			}
			str.Append('}');
			return str.ToString();
		}

		/// <summary> Remove all references to a given database. </summary>
		/// <param name="path"> Path of database to drop </param>
		public static void Drop(string path) {
			LocalDB db = Local(path);
			db.Close(false);

			if (Directory.Exists(db.dbPath)) {
				Directory.Delete(db.dbPath, true);
				Directory.CreateDirectory(db.dbPath);
			}
		}

		/// <summary> Remove all references to a given database. </summary>
		/// <typeparam name="T"> Generic type of database to find and drop. </typeparam>
		public static void Drop<T>() {
			LocalDB db = Local<T>().wrapped;
			db.Close(false);

			if (Directory.Exists(db.dbPath)) {
				Directory.Delete(db.dbPath, true);
				Directory.CreateDirectory(db.dbPath);
			}
		}
		/// <summary> Helper static class for locating databases based on a static type. </summary>
		/// <typeparam name="T"> Generic type to use. </typeparam>
		public static class Of<T> {
			/// <summary> True if the database contains arrays, otherwise false. </summary>
			public static readonly bool isArrayType = Json.ExpectedReflectedType(typeof(T)) == JsonType.Array;
			/// <summary> Statically located database for the given type. </summary>
			public static LocalDB<T> db { get; private set; } = Local<T>();

			public static void Save() { db.Save(); }
			public static void Save(string path) { db.Save(path); }
			public static void Save(string path, T val) { db.Save(path, val); }
			public static bool IsOpen(string path) { return db.IsOpen(path); }
			public static T Check(string path, out bool existed) { return db.Check(path, out existed); }
			public static bool Check(string path, out T record) { return  db.Check(path, out record); }

			public static T Get(string path) { return db.Get(path); }
			public static T Open(string path) { return db.Open(path); }
			public static T Open(string path, out bool existed) { return db.Open(path, out existed); }
			public static T ReOpen(string path) { return db.ReOpen(path); }
			public static T HotOpen(string path) { return db.HotOpen(path); }
			public static bool Set(string path, T value, bool forceSave = false) { return db.Set(path, value, forceSave); }

			public static void Close(bool save = true) { db.Close(save); }
			public static void Close(string path, bool save = true) { db.Close(path, save); }
			public static bool Exists(string path) { return db.Exists(path); }
			public static void Delete(string path) { db.Delete(path); }

		}

	}
	
	/// <summary> Class that wraps a <see cref="LocalDB"/> and bakes <typeparamref name="T"/> into all generic method calls. </summary>
	/// <typeparam name="T"> Generic type to bake into all calls </typeparam>
	public class LocalDB<T> {
		/// <summary> Internal wrapped <see cref="LocalDB"/> object </summary>
		public LocalDB wrapped { get; private set; }
		/// <summary> Constructor </summary>
		/// <param name="wrapped"> <see cref="LocalDB"/> to wrap </param>
		public LocalDB(LocalDB wrapped) { this.wrapped = wrapped; }
		
		public void Save() { wrapped.Save(); }
		public void Save(string path) { wrapped.Save(path); }
		public void Save(string path, T val) { wrapped.Save(path, Json.Reflect(val)); }
		public bool IsOpen(string path) { return wrapped.IsOpen(path); }
		public T Check(string path, out bool existed) {
			bool ex;
			JsonValue v = wrapped.Check(path, out ex); existed = ex;
			if (ex) { return Json.GetValue<T>(v); }
			return default(T);
		}
		public bool Check(string path, out T record) {
			JsonValue v;
			bool ex = wrapped.Check(path, out v);
			if (ex) { record = Json.GetValue<T>(v); } else { record = default(T); }
			return ex;
		}

		public T Get(string path) { return wrapped.Get<T>(path); }
		public T Open(string path) { return wrapped.Open<T>(path); }
		public T Open(string path, out bool existed) { return wrapped.Open<T>(path, out existed); }
		public T ReOpen(string path) { return wrapped.ReOpen<T>(path); }
		public T HotOpen(string path) { return wrapped.HotOpen<T>(path); }
		public bool Set(string path, T value, bool forceSave = false) { return wrapped.Set(path, Json.Reflect(value), forceSave); }

		public void Close(bool save = true) { wrapped.Close(save); }
		public void Close(string path, bool save = true) { wrapped.Close(path, save); }
		public bool Exists(string path) { return wrapped.Exists(path); }
		public void Delete(string path) { wrapped.Delete(path); }

	}


	/// <summary> Class representing a local, In-Memory database for raw data that has a well-defined structure. </summary>
	public class LocalDB {

		/// <summary> For ease of use. </summary>
		private static readonly Encoding UTF8 = Encoding.UTF8;

		/// <summary> Directory the program is running from. Shorthand for Directory.GetCurrentDirectory() </summary>
		public static string CurrentDirectory { get { return Directory.GetCurrentDirectory(); } }

		/// <summary> Convert a file or folder path to only contain forward slashes '/' instead of backslashes '\'. </summary>
		/// <param name="path"> Path to convert </param>
		/// <returns> <paramref name="path"/> with all '\' characters replaced with '/' </returns>
		private static string ForwardSlashPath(string path) {
			string s = path.Replace('\\', '/');
			return s;
		}

		/// <summary> Convert a file path to a folder path. Assumed that path is a forward slash path. </summary>
		/// <param name="path"> Path to file </param>
		/// <returns> Path to a folder </returns>
		private static string PathOfFolder(string path) {
			return path.Substring(0, path.LastIndexOf('/'));
		}

		/// <summary> Takes a relative path and prepends the database path to it </summary>
		/// <param name="relativePath"> Relative path to a document </param>
		/// <returns> Absolute path to a document file </returns>
		private string FullPath(string relativePath) { return dbPath + ProcessPath(relativePath); }

		/// <summary> Method that checks for an existing extension, and adds it if it is not present </summary>
		/// <param name="path"> Path to check </param>
		/// <returns> either <paramref name="path"/> if there was an extension, 
		/// otherwise the given string with the <see cref="extension"/> added.</returns>
		private string ProcessPath(string path) {
			path = ForwardSlashPath(path);
			int lastSlash = path.LastIndexOf('/');
			int dot = (lastSlash > 0) ? path.IndexOf('.', lastSlash) : path.IndexOf('.');
			return (dot > 0) ? (path) : (path + '.' + extension);
		}

		/// <summary> Relative path for this database. </summary>
		public string directory { get; private set; }

		/// <summary> File extension to use (unless otherwise specified) </summary>
		public string extension { get; private set; }

		/// <summary> Absolute path for this database. </summary>
		public string dbPath { get { return CurrentDirectory + "/db/" + directory; } }

		/// <summary> Is data on disk compressed (Gzipped)? </summary>
		public bool compress { get; private set; }

		/// <summary> Should this DB be treated as read-only? </summary>
		public bool readOnly { get; private set; }

		/// <summary> Are JsonObject's PrettyPrint'd into their files? </summary>
		public bool pretty { get; set; }

		/// <summary> Collection of open files, and their live data. 
		/// Values are either JsonObject or JsonArray only. </summary>
		private ConcurrentDictionary<string, JsonValue> documents;

		/// <summary> Last timestamp when files were loaded/saved. </summary>
		private ConcurrentDictionary<string, DateTime> times;

		/// <summary> Indexer into this LocalDB object </summary>
		/// <param name="key"> Key, or path of target object </param>
		/// <returns> Copy of the requested object </returns>
		/// <remarks> 
		///		Uses the <see cref="ConcurrentDictionary{TKey, TValue}"/> backing this database, and performs a get/set by copy. 
		///		Meaning, upon set, the object in the database is a copy of the object that was used for the set.
		///		And upon get, the resulting return value is a copy of the data object in the database.
		///		Nothing in the database is directly worked on using this accessor.
		/// </remarks>
		public JsonValue this[string key] {
			get {
				JsonValue doc = null;
				documents.TryGetValue(key, out doc);
				if (doc == null) { return null; }
				return doc.DeepCopy();
			}
			set {
				if (value.JsonType != JsonType.Object && value.JsonType != JsonType.Array) {
					throw new NotSupportedException("LocalDB.this[string]: Only JsonObject and JsonArray may be stored as documents in this database!");
				}
				JsonValue copy = value.DeepCopy();
				if (documents.ContainsKey(key)) {
					documents[key] = copy;
				}
			}
		}


		/// <summary> Constructor </summary>
		/// <param name="localPath"> Relative path of the database, from the program directory. </param>
		/// <param name="compress"> Is the data in this database GZip compressed? Keep this consistant- don't change it on the next startup after a database actually has data in it. </param>
		public LocalDB(string localPath = "documents", string extension = "wtf", bool readOnly = false, bool compress = false, bool pretty = true) {
			this.compress = compress;
			this.pretty = pretty;
			this.extension = extension;
			this.readOnly = readOnly;
			directory = localPath.Replace('\\', '/') + '/';
			if (!Directory.Exists(dbPath)) { Directory.CreateDirectory(dbPath); }
			documents = new ConcurrentDictionary<string, JsonValue>();
			times = new ConcurrentDictionary<string, DateTime>();
		}

		/// <summary> Saves all open documents to disk. </summary>
		public void Save() {
			foreach (var pair in documents) { Save(pair.Key); }
		}

		/// <summary> Saves a specific document to disk. </summary>
		/// <param name="path"> Relative path of document to save. </param>
		public void Save(string path) {
			JsonValue doc = this[path];

			SaveInternal(path, doc);
		}

		/// <summary> Sets the value of a specific document, then saves it to disk. </summary>
		/// <param name="path"> Relative path of document to overwrite/save. </param>
		/// <param name="doc"> Data to save at the given <paramref name="path"/> </param>
		public void Save(string path, JsonValue doc) {
			if (!IsOpen(path)) { JustOpen(path); }
			this[path] = doc;
			SaveInternal(path, doc);
		}

		/// <summary> Sets the value of a specific document, then saves it to disk. </summary>
		/// <typeparam name="T"> Generic type </typeparam>
		/// <param name="path"> Relative path of document to overwrite/save. </param>
		/// <param name="t"> Instance to save </param>
		public void Save<T>(string path, T t) { Save(path, Json.Reflect(t)); }


		/// <summary> Common logic for saving a document </summary>
		/// <param name="path"> Path to save at </param>
		/// <param name="doc"> Document to save </param>
		private void SaveInternal(string path, JsonValue doc) {
			if (doc != null) {
				string filePath = FullPath(path);
				//string fileDir = filePath.Substring(0, filePath.LastIndexOf('/'));
				//if (!Directory.Exists(fileDir)) {
				//	Directory.CreateDirectory(fileDir);
				//}
				string json = pretty ? doc.PrettyPrint() : doc.ToString();
				byte[] data = compress ? GZip.Compress(json) : UTF8.GetBytes(json);
				File.WriteAllBytes(filePath, data);
				times[path] = DateTime.UtcNow;
			}
		}

		/// <summary> See if a given document is currently open </summary>
		/// <param name="path"> Path of document to check for </param>
		/// <returns> True if the document is open, false otherwise. </returns>
		public bool IsOpen(string path) { return documents.ContainsKey(path); }

		/// <summary> Check for a given document existing, and get it if it does  </summary>
		/// <param name="path"> Path to check for document at </param>
		/// <param name="existed"> out parameter. True after the method ends if the object existed, false if it did not. </param>
		/// <returns> A copy of the given object if it did exist, or JsonNull.instance if it did not. </returns>
		public JsonValue Check(string path, out bool existed) {
			existed = Exists(path);
			if (existed) { return Open(path); }
			return JsonNull.instance;
		}

		/// <summary> Check for a given document existing, and get it if it does  </summary>
		/// <param name="path"> Path to check for document at </param>
		/// <param name="record"> out parameter. Set to the record after the method ends if the record existed, and JsonNull.instance if it did not. </param>
		/// <returns> True if it did exist, or false if it did not. </returns>
		public bool Check(string path, out JsonValue record) {
			if (Exists(path)) {
				record = Open(path);
				return true;
			}
			record = JsonNull.instance;
			return false;
		}

		/// <summary> Check for a given document existing, as a JsonObject, and get it if it does </summary>
		/// <param name="path"> Path to check for document at </param>
		/// <param name="record"> Out parameter. Set to the record after the method ends if the record existed and was a JsonObject, and null otherwise. </param>
		/// <returns> True, if the object existed, false otherwise. </returns>
		public bool Check(string path, out JsonObject record) {
			if (Exists(path)) {
				record = Open(path) as JsonObject;
				return true;
			}
			record = null;
			return false;
		}

		/// <summary> Check for a given document existing, as a JsonArray, and get it if it does </summary>
		/// <param name="path"> Path to check for document at </param>
		/// <param name="record"> Out parameter. Set to the record after the method ends if the record existed and was a JsonArray, and null otherwise. </param>
		/// <returns> True, if the array existed, false otherwise.</returns>
		public bool Check(string path, out JsonArray record) {
			if (Exists(path)) {
				record = Open(path) as JsonArray;
				return true;
			}
			record = null;
			return false;
		}

		/// <summary> Intended to use to access documents in a read only manner.
		/// Returns the reference to the document that is open for the given name
		/// Anything that uses this should adhere to the contract.
		/// Hence the __ to mark the function as spooky. </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public JsonValue __readonly(string path) {
			if (!IsOpen(path)) { JustOpen(path); }
			return documents[path];
		}

		/// <summary> Just makes sure a document is open, to not incurr the cost of duplication of data. </summary>
		/// <param name="path"> Relative path of document to open from the root of this LocalDB instance </param>
		/// <param name="type"> Type of document to create, if not found. Must be either JsonType.Object or JsonType.Array. Default is Object. </param>
		/// <returns> True if the object existed before it was opened, false if a new document was created. </returns>
		public bool JustOpen(string path, JsonType type = JsonType.Object) {
			var existed = Exists(path);
			if (!documents.ContainsKey(path)) {
				if (type != JsonType.Object && type != JsonType.Array) {
					throw new NotSupportedException("LocalDB.Open: Type must be Object or Array - no other JsonType can be made into a single document.");
				}

				string filePath = FullPath(path);
				string fileFolder = PathOfFolder(filePath);
				if (!Directory.Exists(fileFolder)) { Directory.CreateDirectory(fileFolder); }

				if (File.Exists(filePath)) {
					times[path] = DateTime.UtcNow;
					byte[] data = File.ReadAllBytes(filePath);
					string json = compress ? GZip.DecompressString(data) : UTF8.GetString(data);
					var doc = Json.Parse(json);
					if (doc.isArray || doc.isObject) {
						documents[path] = doc;
						//lock (documents) { documents[path] = doc; }

					}

				} else {
					times[path] = DateTime.UtcNow;
					JsonValue doc; // This is a stupid hack, but it works. Assigning to a variable to cast it to the correct type!
					doc = (type == JsonType.Object) ? (doc = new JsonObject()) : (doc = new JsonArray());
					documents[path] = doc;

					//lock (documents) { documents[path] = doc; }
				}

			}
			return existed;
		}

		/// <summary> If a document exists, open it, otherwise get the default (null) value. </summary>
		/// <typeparam name="T"> Generic type expected for result </typeparam>
		/// <param name="path"> Relative path of document to open from the root of this <see cref="LocalDB"/> instance </param>
		/// <returns> Copy of opened document. </returns>
		public T Get<T>(string path) {
			return Exists(path) ? Open<T>(path) : default(T);
		}

		/// <summary> Open a document based on a given <paramref name="path"/> </summary>
		/// <param name="path"> Relative path of document to open from the root of this <see cref="LocalDB"/ instance </param>
		/// <param name="existed"> out parameter. True after the method ends if the object existed before it was opened. </param>
		/// <param name="type"> Type of document to create, if not found. Must be either <see cref="JsonType.Object"/> or <see cref="JsonType.Array"/>. Default is <see cref="JsonType.Object"/>. </param>
		/// <returns> Copy of opened document, same as this[<paramref name="path"/> </returns>
		public JsonValue Open(string path, out bool existed, JsonType type = JsonType.Object) {
			existed = Exists(path);
			return Open(path, type);
		}

		/// <summary> Open a document based on a given <paramref name="path"/> </summary>
		/// <typeparam name="T"> Generic type expected for result </typeparam>
		/// <param name="path"> Relative path of document to open from the root of this <see cref="LocalDB"/ instance </param>
		/// <param name="existed"> out parameter. True after the method ends if the object existed before it was opened. </param>
		/// <param name="type"> Type of document to create, if not found. Must be either <see cref="JsonType.Object"/> or <see cref="JsonType.Array"/>. Default is <see cref="JsonType.Object"/>. </param>
		/// <returns> Copy of opened document, reflected to type <typeparamref name="T"/>. </returns>
		public T Open<T>(string path, out bool existed) {
			existed = Exists(path);
			return Open<T>(path);
		}


		/// <summary> Open a document based on a given path </summary>
		/// <param name="path"> Relative path of document to open from the root of this <see cref="LocalDB"/ instance </param>
		/// <param name="type"> Type of document to create, if not found. Must be either <see cref="JsonType.Object"/> or <see cref="JsonType.Array"/>. Default is <see cref="JsonType.Object"/>. </param>
		/// <returns> Copy of opened document, ready for work </returns>
		public JsonValue Open(string path, JsonType type = JsonType.Object) {
			JustOpen(path, type);
			return this[path];
		}


		/// <summary> Open a document based on a given path </summary>
		/// <typeparam name="T"> Generic type expected for result </typeparam>
		/// <param name="path"> Relative path of document to open from the root of this <see cref="LocalDB"/> instance </param>
		/// <returns> Copy of opened document as a <typeparamref name="T"/>, ready for work </returns>
		public T Open<T>(string path) {
			JsonType type = Json.ExpectedReflectedType(typeof(T));
			JustOpen(path, type);
			return Json.GetValue<T>(this[path]);
		}

		/// <summary> Forcefully reload/reopen a document based on a given path. Discards any changes that are currently on the document if used. </summary>
		/// <param name="path"> Relative path of document to open from the root of this  <see cref="LocalDB"/> instance </param>
		/// <param name="type"> Type of document to create, if not found. Must be either <see cref="JsonType.Object"/> or <see cref="JsonType.Array"/>. Default is <see cref="JsonType.Object"/>. </param>
		/// <returns> Copy of the opened document, ready for work. </returns>
		public JsonValue ReOpen(string path, JsonType type = JsonType.Object) {
			Close(path, false);
			return Open(path, type);
		}

		/// <summary> Forcefully reload/reopen a document based on a given path. Discards any changes that are currently on the document if used. </summary>
		/// <typeparam name="T"> Generic type expected for result </typeparam>
		/// <param name="path"> Relative path of document to open from the root of this  <see cref="LocalDB"/> instance </param>
		/// <returns> Copy of the opened document, ready for work. </returns>
		public T ReOpen<T>(string path) {
			Close(path, false);
			return Open<T>(path);
		}

		/// <summary> Opens a document, and reloads it if it has been changed since the last access. </summary>
		/// <param name="path"> Relative path of document to open from the root of this LocalDB instance </param>
		/// <param name="type"> Type of document to create, if not found. Must be either <see cref="JsonType.Object"/> or <see cref="JsonType.Array"/>. Default is <see cref="JsonType.Object"/>. </param>
		/// <returns> Copy of the opened document, ready for work. </returns>
		public JsonValue HotOpen(string path, JsonType type = JsonType.Object) {
			if (times.ContainsKey(path)) {
				string filePath = FullPath(path);
				DateTime time = File.GetLastWriteTimeUtc(filePath);
				if (time > times[path]) {
					return ReOpen(path);
				}
			}
			return Open(path);
		}

		/// <summary> Opens a document, and reloads it if it has been changed since the last access. </summary>
		/// <typeparam name="T"> Generic type expected for result </typeparam>
		/// <param name="path"> Relative path of document to open from the root of this LocalDB instance </param>
		/// <returns> Copy of the opened document, ready for work. </returns>
		public T HotOpen<T>(string path) {
			JsonType type = Json.ExpectedReflectedType(typeof(T));
			JsonValue val = HotOpen(path, type);
			return Json.GetValue<T>(val);
		}

		/// <summary> Sets the document at path to be equal to the given value. Document must be open for this operation to be successful. </summary>
		/// <param name="path"> Relative path of document to set </param>
		/// <param name="value"> Value to set as the document's content </param>
		/// <param name="forceSave"> Should there be a save performed after the operation? </param>
		/// <returns> True if set was successful, false if it failed. </returns>
		public bool Set(string path, JsonValue value, bool forceSave = false) {
			if (!value.isArray && !value.isObject) {
				throw new NotSupportedException("LocalDB.Set: Type of passed JsonValue must be Object or Array - no other JsonType can be made into a single document");
			}

			if (documents.ContainsKey(path)) {
				//lock (documents) { documents[path] = value; }
				documents[path] = value;
				if (forceSave) { Save(path); }
				return true;
			}
			return false;
		}

		/*
		 * This shit can be done later
		 * 
		/// <summary> Get a copy of a part or whole of a given document from the database. </summary>
		/// <param name="path"> Relative Path to the document </param>
		/// <param name="subIndexes"> Sub-paths inside of that object to navigate, in order. </param>
		/// <returns> A copy of the object pointed to by path and subindexes. </returns>
		public JsonValue Get(string path, params JsonValue[] subIndexes) {
			JsonValue val;
			// Has to lock for thread safety, since we're traversing the actual object inside the DB
			lock (documents) {
				JsonValue ptr;
				ptr = documents[path];
				for (int i = 0; i < subIndexes.Length; i++) {
					if (ptr.isObject || ptr.isArray) {
						ptr = ptr[subIndexes[i]];
					} else { ptr = JsonNull.instance; break; }
				}
				val = ptr.DeepCopy();
			}
			return val;
		}

		/// <summary> Sets a </summary>
		/// <param name="path"></param>
		/// <param name="val"></param>
		/// <param name="subIndexes"></param>
		public void Set(string path, JsonValue val, params JsonValue[] subIndexes) {

		}
		//*/




		/// <summary> Immediately close ALL open documents, optionally saving them. </summary>
		/// <param name="save"> If all documents should be saved or not. </param>
		public void Close(bool save = true) {
			foreach (var pair in documents) {
				Close(pair.Key, save);
			}
		}

		/// <summary> Close a given document. </summary>
		/// <param name="path"> relative path of document to close </param>
		/// <param name="save"> True to save, false to not save. </param>
		public void Close(string path, bool save = true) {
			JsonValue __;
			DateTime ___;
			if (documents.ContainsKey(path)) {
				if (save) { Save(path); }
				while (!documents.TryRemove(path, out __)) { }
			}
			if (times.ContainsKey(path)) {
				while (!times.TryRemove(path, out ___)) { }
			}
		}

		/// <summary> Check if a document exists at a given path. Returns true if a file exists at the path, or if that file is currently open. </summary>
		/// <param name="path"> Path to check </param>
		/// <returns> True if a document exists at that path, false otherwise. </returns>
		public bool Exists(string path) {
			if (documents.ContainsKey(path)) { return true; }
			string fullPath = FullPath(path);
			return File.Exists(fullPath);
		}

		/// <summary> Delete a document, if it exists at a given path. </summary>
		/// <param name="path"> Relative path to delete at. </param>
		public void Delete(string path) {
			if (Exists(path)) {
				File.Delete(FullPath(path));
				Close(path, false);
			}
		}

	}

	#region Self-Contained Utilities
	/// <summary> Holds some easy useful access to compression/decompression methods </summary>
	public static class GZip {

		/// <summary> Compresses a string using GZip. Uses UTF8 encoding to convert the string to a byte[] </summary>
		/// <param name="data">string to compress. </param>
		/// <returns>GZip Compressed version of data</returns>
		public static byte[] Compress(string data) { return Compress(Encoding.UTF8.GetBytes(data)); }
		/// <summary> Compresses a byte[] using GZip </summary>
		/// <param name="data">byte[] to compress</param>
		/// <returns>GZip Compressed version of data</returns>
		public static byte[] Compress(byte[] data) {
			byte[] compressed;
			using (var outStream = new MemoryStream(data.Length / 4)) {
				using (var gz = new GZipStream(outStream, CompressionMode.Compress)) {
					gz.Write(data, 0, data.Length);
				}
				compressed = outStream.ToArray();
			}
			return compressed;
		}

		/// <summary> Decompresses a GZip byte[], and attempts to convert the output into a UTF8 string. </summary>
		/// <param name="data">GZip byte[] to decompress</param>
		/// <returns>A string containing the UTF8 representation of the decompressed data </returns>
		public static string DecompressString(byte[] data) { return Encoding.UTF8.GetString(Decompress(data)); }

		/// <summary> Decompresses a GZip byte[] </summary>
		/// <param name="data">GZip byte[] to decompress</param>
		/// <returns>Decompressed version of the input data</returns>
		public static byte[] Decompress(byte[] data) {
			using (var inStream = new MemoryStream(data)) {
				using (var gz = new GZipStream(inStream, CompressionMode.Decompress)) {
					const int size = 4096;
					byte[] buffer = new byte[size];
					using (MemoryStream mem = new MemoryStream(size)) {
						int count = 0;
						do {
							count = gz.Read(buffer, 0, size);
							if (count > 0) {
								mem.Write(buffer, 0, count);
							}
						} while (count > 0);
						return mem.ToArray();
					}
				}
			}

		}



	}
	#endregion


	public static class DB_Tests {

		public static readonly string TEST_DB_NAME = "TestDatabase";
		private static readonly string TEST_FILE = "yo.wtf";

		public static LocalDB testDB { get { return DB.Local(TEST_DB_NAME); } }

		public static void Clean() {
			// Drop closes the database, and then deletes the directory.
			DB.Drop(TEST_DB_NAME);


		}

		public static void SetupEmpty() {

		}

		public static void TestBasics() {
			SetupEmpty();

			bool exists;
			bool existed = testDB.Exists(TEST_FILE);
			existed.ShouldBe(false);

			// Accessing indexer of unopened document should yield a null.
			JsonObject nothing = testDB[TEST_FILE] as JsonObject;
			(null==nothing).ShouldBe(true);

			// Opening a new document should yield an empty object
			JsonObject first = testDB.Open(TEST_FILE, out existed) as JsonObject;
			first.Count.ShouldBe(0);
			existed.ShouldBe(false);
			exists = testDB.Exists(TEST_FILE);
			exists.ShouldBe(true);

			// Accessing database of open object should yield a copy.
			JsonObject first_again = testDB[TEST_FILE] as JsonObject;
			first_again.Count.ShouldBe(0);
			// Equal but not the same object
			first.ShouldEqual(first_again);
			first.ShouldNotBe(first_again);

			// Set something into the object
			first["data"] = "value";

			// Apply and save to database.
			testDB[TEST_FILE] = first;
			testDB.Save();
			// Close everything
			testDB.Close();

			// File should still exist, even if it is not opened.
			exists = testDB.Exists(TEST_FILE);
			exists.ShouldBe(true);
			// Closed existing document should still be null when accessed by indexer.
			nothing = testDB[TEST_FILE] as JsonObject;
			(null==nothing).ShouldBe(true);

			// Open document again
			JsonObject second = testDB.Open(TEST_FILE, out existed) as JsonObject;
			existed.ShouldBe(true);
			// Data should be retained
			second.Count.ShouldBe(1);
			// But they should be separate objects
			second.ShouldNotBe(first);
			exists = testDB.Exists(TEST_FILE);
			exists.ShouldBe(true);

			// Access document again
			JsonObject second_again = testDB.Open(TEST_FILE) as JsonObject;
			// Should have same data, but be a different reference
			second_again.Count.ShouldBe(1);
			second.ShouldEqual(second_again);
			second.ShouldNotBe(second_again);

			// Add more data to blob object
			second["moreData"] = "anotherValue";
			testDB.Set(TEST_FILE, second);
			// Explicitly close the document
			testDB.Close(TEST_FILE);

			// Can directly open a file 
			existed = testDB.JustOpen(TEST_FILE);
			existed.ShouldBe(true);
			// And then withdraw the data separately.
			JsonObject third = testDB[TEST_FILE] as JsonObject;
			// Open() just wraps these operations
			// With an optional check to see if the document was on disk first.
			third.Count.ShouldBe(2);

			// No objects should be == eachother, should all be separate objects.
			first.ShouldNotBe(second);
			first.ShouldNotBe(third);
			second.ShouldNotBe(third);


		}

		public static void TestCheck() {
			SetupEmpty();

			bool existed;
			JsonObject record;
			JsonArray wrongTypeOfRecord;
			JsonValue generalRecord;

			existed = testDB.Check(TEST_FILE, out record);
			existed.ShouldBe(false);
			(null==record).ShouldBe(true);

			existed = testDB.Check(TEST_FILE, out wrongTypeOfRecord);
			existed.ShouldBe(false);
			(null==wrongTypeOfRecord).ShouldBe(true);

			existed = testDB.Check(TEST_FILE, out generalRecord);
			existed.ShouldBe(false);
			(null==generalRecord).ShouldBe(true);

			generalRecord = testDB.Check(TEST_FILE, out existed);
			existed.ShouldBe(false);
			(null==generalRecord).ShouldBe(true);

			testDB.Open(TEST_FILE);

			existed = testDB.Check(TEST_FILE, out record);
			existed.ShouldBe(true);
			record.ShouldNotBe(null);

			existed = testDB.Check(TEST_FILE, out wrongTypeOfRecord);
			existed.ShouldBe(true);
			(null==wrongTypeOfRecord).ShouldBe(true);

			existed = testDB.Check(TEST_FILE, out generalRecord);
			existed.ShouldBe(true);
			generalRecord.ShouldNotEqual(null);

			generalRecord = testDB.Check(TEST_FILE, out existed);
			existed.ShouldBe(true);
			generalRecord.ShouldNotEqual(null);

		}


	}

}
