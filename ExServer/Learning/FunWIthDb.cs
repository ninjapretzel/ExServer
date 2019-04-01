using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BDoc = MongoDB.Bson.BsonDocument;
using MDB = MongoDB.Driver.IMongoDatabase;
using Coll = MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>;

namespace Learnings {

	public class FunWithDB {
		public static MongoClient client;

		public static void FunsWithDB() {
			if (client == null) {
				client = new MongoClient("mongodb://localhost:27017");
			}
			var db = client.GetDatabase("yeet");
			Coll garbage = db.GetCollection<BDoc>("garbage");
			// Clear all shit
			garbage.DeleteMany(new BDoc());


			var doc = new BDoc {
				{ "name", "MongoDB" },
				{ "type", "database" },
				{ "count", 1 },
				{ "info", new BDoc { {"x", 203 }, {"y", 102 } } }
			};

			garbage.InsertOne(doc);

			var docs = Enumerable.Range(0, 36).Select(i => new BDoc("i", i));
			garbage.InsertMany(docs);

			BDoc empty = new BDoc();
			var count = garbage.CountDocuments(empty);

			Console.WriteLine("Hello world");
			Console.WriteLine($"{count} documents (in a row?).");

			var found = garbage.Find(empty);
			Console.WriteLine($"I have a document it is");
			Console.WriteLine(found.FirstOrDefault().ToString());

			var doclist = garbage.Find(empty).ToList();
			Console.WriteLine("Found Docs:");
			foreach (var d in doclist) {
				Console.WriteLine($"Doc {d.ToString()}");
			}

			var luckyDoc = garbage.Find(new BDoc("i", 7));
			Console.WriteLine($"I FOUND MY LUCKY DOCUMENT, {luckyDoc.FirstOrDefault().ToString()}");
			var findFilter = Builders<BDoc>.Filter.Eq("i", 7);
			var filteredDoc = garbage.Find(findFilter).ToCursor();
			foreach (var d in filteredDoc.ToEnumerable()) {
				Console.WriteLine($"lucky found {d.ToString()}");
			}

			var filter = Builders<BDoc>.Filter.Gt("i", 14) & Builders<BDoc>.Filter.Lte("i", 88);
			var lotsOfDocs = garbage.Find(filter).ToCursor();

			Console.WriteLine($"Filtered Docs: ");
			foreach (var d in lotsOfDocs.ToEnumerable()) {
				Console.WriteLine($"Doc {d.ToString()}");
			}

		}
	}
}
