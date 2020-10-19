using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BakaTest;
using static BakaTest.BakaTests;

public static class Json_Tests {

	private static Func<IDictionary<JsonString,  JsonValue>> prev;
	public static void BEFORE() {
		prev = JsonObject.DictionaryGenerator;
		JsonObject.DictionaryGenerator = () => { return new Dictionary<JsonString, JsonValue>(); };	
	}
	public static void AFTER() {
		JsonObject.DictionaryGenerator = prev;
		prev = null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////
	// Test Code
	public class TestReflecting {
		private class PrimitivesModel {
			public float value1 = 1;
			public float value2 = 2;
			public double dvalue = 55;
			public string str = "defaultString";
			public bool flag1 = false;
			public bool flag2 = true;
			public override bool Equals(object obj) {
				if (!(obj is PrimitivesModel)) { return false; }
				var o = obj as PrimitivesModel;
				return value1 == o.value1 && value2 == o.value2 && dvalue == o.dvalue && str == o.str && flag1 == o.flag1 && flag2 == o.flag2;
			}
			public override int GetHashCode() { return -1; }
		}
		public static void TestObjectParse1() {
			{
				PrimitivesModel model = new PrimitivesModel();
				PrimitivesModel fromEmpty = Json.GetValue<PrimitivesModel>(new JsonObject());
				model.ShouldEqual(fromEmpty);
			}

			{
				PrimitivesModel model = new PrimitivesModel();
				model.str = "otherString";
				model.flag1 = true;
				model.value2 = 50;
				JsonObject delta = new JsonObject("str", "otherString", "flag1", true, "value2", 50);
				PrimitivesModel fromDelta = Json.GetValue<PrimitivesModel>(delta);
				model.ShouldEqual(fromDelta);
			}
		}

		private class ArraysModel {
			public float[] floats = new float[] { 3 };
			public string[] strs = new string[] { "nope" };
			public bool[] flags = new bool[] { false };
		}
		public static void TestArrayReflection() {
			{
				JsonObject obj = new JsonObject();
				obj["floats"] = new JsonArray(0, 1, 2, 3, 4, 5, 6);
				obj["strs"] = new JsonArray("oh", "bob", "saget");
				obj["flags"] = new JsonArray(true, true, false, true, false, false);

				ArraysModel model = new ArraysModel();
				model.floats = new float[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f };
				model.strs = new string[] { "oh", "bob", "saget" };
				model.flags = new bool[] { true, true, false, true, false, false };

				ArraysModel reflected = Json.GetValue<ArraysModel>(obj);
				reflected.floats.ShouldBeSame(model.floats);
				reflected.strs.ShouldBeSame(model.strs);
				reflected.flags.ShouldBeSame(model.flags);
			}
		}

		private class JsonValuesModels {
			public JsonArray arr = new JsonArray(12);
			public JsonObject obj = new JsonObject("nope", 5);
			public JsonString str = "yep";
			public JsonNumber num = 200;
			public JsonBool flg = false;
			public override bool Equals(object other) {
				if (!(other is JsonValuesModels)) { return false; }
				var o = other as JsonValuesModels;
				return arr.Equals(o.arr) && obj.Equals(o.obj) && str == o.str && num == o.num && flg == o.flg;
			}
			public override int GetHashCode() { return -1; }
		}


		public static void TestJsonValueReflection() {
			{
				JsonObject obj = new JsonObject();
				obj["arr"] = new JsonArray(1, 2, 3, 4);
				obj["obj"] = new JsonObject("yeah", 20);
				obj["str"] = "naw";
				obj["num"] = 300;
				obj["flg"] = true;

				JsonValuesModels model = new JsonValuesModels();
				model.arr = new JsonArray(1, 2, 3, 4);
				model.obj = new JsonObject("yeah", 20);
				model.str = "naw";
				model.num = 300;
				model.flg = true;

				JsonValuesModels reflected = Json.GetValue<JsonValuesModels>(obj);
				reflected.ShouldEqual(model);
			}
		}
	}

	/// <summary> Test holding JsonObject test functions </summary>
	public class TestJsonObject {
		public static void TestObjectAdd() {
			{
				JsonObject obj = new JsonObject();

				obj.Count.ShouldBe(0);

				obj.Add("what", "huh")
					.Add("okay", "alright");

				obj.Count.ShouldBe(2);
			}
		}
		public static void TestEmpty() {
			{
				JsonObject empty = new JsonObject();
				JsonObject emptyConcurrent = new JsonObject(new ConcurrentDictionary<JsonString, JsonValue>());

				empty.IsEmpty.ShouldBeTrue();
				emptyConcurrent.IsEmpty.ShouldBeTrue();

				JsonObject notEmpty = new JsonObject("ayy", "lmao");
				JsonObject notEmptyConcurrent = new JsonObject(new ConcurrentDictionary<JsonString, JsonValue>());
				notEmptyConcurrent.Add("ayy", "lmao");

				notEmpty.IsEmpty.ShouldBeFalse();
				notEmptyConcurrent.IsEmpty.ShouldBeFalse();

			}
		}
		public static void TestObjectVectOps() {
			{
				JsonObject v1 = new JsonObject("x", 5, "y", 3, "z", 2);
				JsonObject v2 = new JsonObject("x", 3, "y", 1, "z", 4);

				var v3 = v1.Multiply(v2);
				v3["x"].numVal.ShouldBe(15);
				v3["y"].numVal.ShouldBe(3);
				v3["z"].numVal.ShouldBe(8);

				var v4 = v1.AddNumbers(v2);
				v4["x"].numVal.ShouldBe(8);
				v4["y"].numVal.ShouldBe(4);
				v4["z"].numVal.ShouldBe(6);
			}

			{
				JsonObject matrix = new JsonObject()
					.Add("maxHP", new JsonObject("str", 2, "vit", 5))
					.Add("maxMP", new JsonObject("int", 2, "wis", 2));

				JsonObject stats = new JsonObject("str", 10, "dex", 10, "vit", 10, "int", 10, "wis", 10);

				var result = stats.Multiply(matrix);

				result["maxHP"].numVal.ShouldBe(70);
				result["maxMP"].numVal.ShouldBe(40);
			}
		}
		public static void TestObjectEqual() {

			{
				JsonObject a = new JsonObject("x", 3);
				JsonObject b = new JsonObject("x", 3);
				JsonObject c = new JsonObject("x", 2);

				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject("x", null, "y", false, "z", true);
				JsonObject b = new JsonObject()
					.Add("x", null)
					.Add("y", false)
					.Add("z", true);
				JsonObject c = new JsonObject("x", "something", "y", true, "z", false);


				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject()))
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyObject", new JsonObject())
					.Add("emptyArray", new JsonArray());

				JsonObject b = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("emptyObject", new JsonObject())
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyArray", new JsonArray())
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject()));

				JsonObject c = new JsonObject("x", 5, "y", 12, "z", 15, "tag", "blah")
					.Add("emptyObject", new JsonObject())
					.Add("array", new JsonArray("a", "b", "c", 1, 2, 3))
					.Add("emptyArray", new JsonArray())
					.Add("nested", new JsonObject("x", 3, "nestedNested", new JsonObject("x", 5)));

				a.Equals(b).ShouldBeTrue();
				b.Equals(a).ShouldBeTrue();

				a.Equals(c).ShouldBeFalse();
			}

			{
				JsonObject a = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject b = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject c = new JsonObject()
					.Add("name", "bobby bob bobberton")
					.Add("paperTowels", "three hundred")
					.Add("hasBalls", "yes");

				a.ShouldEqual(b);
				a.ShouldNotBe(b);
				a.ShouldNotBe(c);
				a.ShouldNotEqual(c);

				a.Add("son", c);
				b.Add("son", c.Clone());

				a.ShouldEqual(b);
				a.ShouldNotBe(b);

				a["son"].ShouldBe(c);
				a["son"].ShouldEqual(c);

				b["son"].ShouldNotBe(c);
				b["son"].ShouldEqual(c);
			}

		}
		public static void TestObjectIndex() {
			{
				Dictionary<string, float> data = new Dictionary<string, float>() {
					{"str", 5},
					{"dex", 12},
					{"vit", 8},
				};
				JsonObject obj = new JsonObject();
				foreach (var pair in data) { obj[pair.Key] = pair.Value; }

				obj.Count.ShouldBe(3);
				obj["str"].numVal.ShouldBe(5);
				obj["vit"].numVal.ShouldBe(8);
			}
		}
		public static void TestObjectSet() {
			{
				JsonObject a = new JsonObject("x", 1, "y", 2, "z", 3);
				JsonObject b = new JsonObject("x", 4, "y", 5, "z", 6);

				a.Set(b);

				a["x"].numVal.ShouldBe(4);
				a["y"].numVal.ShouldBe(5);
				a["z"].numVal.ShouldBe(6);
			}

			{
				JsonObject a = new JsonObject()
					.Add("nested", new JsonObject("a", 1, "b", 2, "c", 3));

				JsonObject b = new JsonObject()
					.Add("nested", new JsonObject("x", 1, "y", 2, "z", 3, "c", 621));

				a.Set(b);

				a["nested"].Count.ShouldBe(4);
				a["nested"].ContainsKey("a").ShouldBeFalse();
				a["nested"].ContainsKey("x").ShouldBeTrue();
			}

			{
				JsonObject a = new JsonObject()
					.Add("nested", new JsonObject("a", 1, "b", 2, "c", 3));

				JsonObject b = new JsonObject()
					.Add("nested", new JsonObject("x", 1, "y", 2, "z", 3, "c", 621));

				a.SetRecursively(b);

				a["nested"].Count.ShouldBe(6);
				a["nested"].ContainsKey("a").ShouldBeTrue();
				a["nested"].ContainsKey("x").ShouldBeTrue();

			}

		}
		public static void TestObjectPrintParse() {
			{
				JsonObject obj = new JsonObject();

				string str = obj.ToString();
				str.ShouldBe("{}");

				string pp = obj.PrettyPrint();
				pp.ShouldBe<string>("{\n}");

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;

				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

			}

			{
				JsonObject obj = new JsonObject("x", 5, "y", 20, "str", "someString", "z", false);

				string str = obj.ToString();
				string expectedToString = "{'x':5,'y':20,'str':'someString','z':false}".Replace('\'', '\"');
				str.ShouldBe(expectedToString);

				string pp = obj.PrettyPrint();
				string expectedPrettyPrint = @"{
	'x':5,
	'y':20,
	'str':'someString',
	'z':false
}".Replace('\'', '\"');

				pp.ShouldBe<string>(expectedPrettyPrint);
				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;
				
				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));
			}

			{
				JsonObject obj = new JsonObject();
				obj["x"] = new JsonObject();
				obj["x"]["y"] = new JsonObject();
				obj["x"]["y"]["z"] = new JsonObject();

				string str = obj.ToString();
				string expectedToString = "{'x':{'y':{'z':{}}}}".Replace('\'', '\"');
				str.ShouldBe<string>(expectedToString);

				string pp = obj.PrettyPrint();
				string expectedPrettyPrint = @"{
	'x':
	{
		'y':
		{
			'z':
			{
			}
		}
	}
}".Replace('\'', '\"');
				pp.ShouldBe<string>(expectedPrettyPrint);

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;

				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

			}

			{
				JsonObject obj = new JsonObject();
				obj["ayy:lmao"] = 5;
				string str = obj.ToString();

				JsonObject parsed = Json.Parse<JsonObject>(str);
				true.ShouldBe(obj.Equals(parsed));

			}

		}
	}
	/// <summary> Test holding JsonArray test functions </summary>
	public static class TestJsonArray {
		public static void TestArrayGeneral() {
			{
				JsonArray x = new JsonArray();
				x.Count.ShouldBe(0);

				x[0] = "Test1";
				x["1"] = "test2";
				x[2.0] = "test3";

				x.Count.ShouldBe(3);
				x[1].stringVal.ShouldBe<string>("test2");
				x["2"].stringVal.ShouldBe<string>("test3");

			}

			{
				JsonArray x = new object[] { 1, 2, 3 };
				JsonArray y = new JsonArray() { 1, 2, 3 };
				JsonArray z = new int[] { 1, 2, 3 };

				x.Count.ShouldBe(y.Count);
				x.Equals(y).ShouldBeTrue();
				z.Equals(z).ShouldBeTrue();
				y.Equals(z).ShouldBeTrue();
			}

		}
		public static void TestArrayAdd() {
			{
				JsonArray x = new JsonArray().Add(1).Add(2).Add(3);
				x.Count.ShouldBe(3);
			}

			{
				JsonArray x = new JsonArray().Add(1).Add(2).Add(3);
				JsonArray y = new JsonArray().Add(x);

				y.Count.ShouldBe(1);
				y[0].Count.ShouldBe(3);
			}

			{
				int[] nums = { 1, 2, 3 };
				JsonArray x = new JsonArray().Add((JsonArray)nums);
				x.Count.ShouldBe(1);
				x[0].Count.ShouldBe(3);
			}

		}
		public static void TestArrayAddAll() {
			{
				JsonArray x = new JsonArray();
				JsonArray y = new JsonArray();
				x.AddAll(y);
				x.Count.ShouldBe(0);
			}

			{
				JsonArray x = new JsonArray();
				JsonArray y = new JsonArray() { 1, 2, 3 };

				x.AddAll(y);
				x.Count.ShouldBe(3);
			}

			{
				JsonArray x = new JsonArray();
				int[] y = { 1, 2, 3 };

				x.AddAll(y);

			}
		}
		public static void TestArrayPrintParse() {
			{
				JsonArray arr = new JsonArray();
				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				str.ShouldBe<string>("[]");
				pp.ShouldBe<string>("[\n]");

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}

			{
				JsonArray arr = new JsonArray(1, 2, 3, 4, 5, 6);

				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				string strExpected = "[1,2,3,4,5,6]";
				string ppExpected = @"[
	1,
	2,
	3,
	4,
	5,
	6
]".Replace('\'', '\"');

				str.ShouldBe<string>(strExpected);
				pp.ShouldBe<string>(ppExpected);

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}

			{
				JsonArray arr = new JsonArray();
				arr.Add(new JsonArray());
				arr.Add(new JsonArray().Add(new JsonArray()));
				arr.Add(new JsonArray().Add(new JsonArray().Add(new JsonArray())));

				string str = arr.ToString();
				string pp = arr.PrettyPrint();

				string strExpected = "[[],[[]],[[[]]]]";
				string ppExpected = @"[
	[
	],
	[
		[
		]
	],
	[
		[
			[
			]
		]
	]
]";
				str.ShouldBe<string>(strExpected);
				pp.ShouldBe<string>(ppExpected);

				JsonArray strParse = Json.Parse(str) as JsonArray;
				JsonArray ppParse = Json.Parse(pp) as JsonArray;

				true.ShouldBe(arr.Equals(strParse));
				true.ShouldBe(arr.Equals(ppParse));

			}
		}
		public static void TestNestedPrintParse() {
			JsonObject obj = new JsonObject(
				"value", 20,
				"level", 5,
				"name", "Sword of Boom",
				"desc", "It goes boom.",
				"damage", new JsonArray(new JsonObject("power", 25, "type", "fire"), new JsonObject("power", 10, "type", "elec")),
				"proc", new JsonObject(
					"chance", .1,
					"scripts", new JsonArray("explode", new JsonObject("name", "stun", "chance", .35, "duration", 3))
				)

			);

			string str = obj.ToString();
			string pp = obj.PrettyPrint();

			string strExpected = "{'value':20,'level':5,'name':'Sword of Boom','desc':'It goes boom.','damage':[{'power':25,'type':'fire'},{'power':10,'type':'elec'}],'proc':{'chance':0.1,'scripts':['explode',{'name':'stun','chance':0.35,'duration':3}]}}".Replace('\'', '\"');
			string ppExpected = @"{
	'value':20,
	'level':5,
	'name':'Sword of Boom',
	'desc':'It goes boom.',
	'damage':
	[
		{
			'power':25,
			'type':'fire'
		},
		{
			'power':10,
			'type':'elec'
		}
	],
	'proc':
	{
		'chance':0.1,
		'scripts':
		[
			'explode',
			{
				'name':'stun',
				'chance':0.35,
				'duration':3
			}
		]
	}
}".Replace('\'', '\"');

			str.ShouldBe<string>(strExpected);
			//Debug.Log(pp);
			//Debug.Log(ppExpected);
			pp.ShouldBe<string>(ppExpected);

			JsonObject strParse = Json.Parse(str) as JsonObject;
			JsonObject ppParse = Json.Parse(pp) as JsonObject;

			true.ShouldBe(obj.Equals(strParse));
			true.ShouldBe(obj.Equals(ppParse));
		}
		public static void TestListReflecting() {
			{
				List<string> list = new List<string>();
				for (int i = 0; i < 10; i++) { list.Add("" + (char)('a' + i)); }

				var reflect = Json.Reflect(list);
				//Debug.Log(reflect);

				true.ShouldBe(reflect.isArray);

				List<string> reflectBack = Json.GetValue<List<string>>(reflect);

				10.ShouldBe(reflectBack.Count);
				for (int i = 0; i < 10; i++) {
					string expect = "" + (char)('a' + i);
					reflectBack[i].ShouldBe<string>(expect);
				}


			}

		}
		public static void TestEquality() {
			{
				JsonArray a = new JsonArray().Add("Heeeello").Add("nurse").Add(42);
				JsonArray b = new JsonArray().Add("Heeeello").Add("nurse").Add(42);
				JsonArray c = new JsonArray().Add("yes").Add("no").Add("maybe").Add("could you repeat the question?");

				// .Equals
				a.ShouldEqual(b);
				// ==
				a.ShouldNotBe(b);
				a.ShouldNotBe(c);
				a.ShouldNotEqual(c);

			}
		}
	}

	/// <summary> Tests for non-standard json features, other than comments. </summary>
	public static class TestJsonExt {

		public static void TestObjectSetParse() {
			{
				string raw = @"
{
	a, b, c, one, two, three,
}".Replace('\'', '\"');

				JsonObject parsed = Json.Parse<JsonObject>(raw);

				parsed.Count.ShouldBe(6);
				parsed.Get<bool>("a").ShouldBe(true);
				parsed.Get<bool>("b").ShouldBe(true);
				parsed.Get<bool>("c").ShouldBe(true);
				parsed.Get<bool>("one").ShouldBe(true);
				parsed.Get<bool>("two").ShouldBe(true);
				parsed.Get<bool>("three").ShouldBe(true);

				JsonObject expected = new JsonObject("a", true, "b", true, "c", true, "one", true, "two", true, "three", true);

				parsed.ShouldEqual(expected);
			}
		}

		public static void TestObjectsetParseStrict() {
			{
				string raw = @"
{
	a, b, c, one, two, three,
}".Replace('\'', '\"');
				Exception caught = null;
				try {
					JsonObject fail = Json.ParseStrict<JsonObject>(raw);
				} catch (Exception e) { caught = e; }

				caught.ShouldNotBe(null);
			}
		}

		public static void TestArraySetParse() {
			{
				string raw = @"
[
	a, b, c, one, two, three
]".Replace('\'', '\"');

				JsonArray parsed = Json.Parse<JsonArray>(raw);

				parsed.Count.ShouldBe(6);
				parsed.Get<string>(0).ShouldBe("a");
				parsed.Get<string>(1).ShouldBe("b");
				parsed.Get<string>(2).ShouldBe("c");
				parsed.Get<string>(3).ShouldBe("one");
				parsed.Get<string>(4).ShouldBe("two");
				parsed.Get<string>(5).ShouldBe("three");

				JsonArray expected = new JsonArray("a", "b", "c", "one", "two", "three");

				parsed.ShouldEqual(expected);
			}
		}

		public static void TestArraySetParseStrict() {
			{
				string raw = @"
[
	a, b, c, one, two, three
]".Replace('\'', '\"');
				Exception caught = null;
				try {
					JsonArray fail = Json.ParseStrict<JsonArray>(raw);
				} catch (Exception e) { caught = e; }

				caught.ShouldNotBe(null);
			}
		}
	}

	/// <summary> Test holding General JsonValue test functions </summary>
	public static class TestGeneral {
		public static void TestEscapes() {
			{
				JsonObject obj = new JsonObject();
				string key = "keyWithoutEscapes";
				string val = "\"what\" are you doing \"here\" you \"gentleman\" and \"scholar\"";
				obj[key] = val;

				1.ShouldBe(obj.Count);
				true.ShouldBe(obj.ContainsKey(key));
				true.ShouldBe(obj[key] == val);

				string str = obj.ToString();
				string pp = obj.PrettyPrint();
				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;
				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

				JsonObject strStrict = Json.ParseStrict<JsonObject>(str);
				JsonObject ppStrict = Json.ParseStrict<JsonObject>(pp);
				true.ShouldBe(obj.Equals(strStrict));
				true.ShouldBe(obj.Equals(ppStrict));
			}
			{
				JsonObject obj = new JsonObject();

				string key = "scv:\"wark\"";
				string val = "balls:\"borf\"";
				obj[key] = val;
				1.ShouldBe(obj.Count);
				true.ShouldBe(obj.ContainsKey(key));
				true.ShouldBe(obj[key] == val);

				string str = obj.ToString();
				string pp = obj.PrettyPrint();

				JsonObject strParse = Json.Parse(str) as JsonObject;
				JsonObject ppParse = Json.Parse(pp) as JsonObject;
				true.ShouldBe(obj.Equals(strParse));
				true.ShouldBe(obj.Equals(ppParse));

				JsonObject strStrict = Json.ParseStrict<JsonObject>(str);
				JsonObject ppStrict = Json.ParseStrict<JsonObject>(pp);
				true.ShouldBe(obj.Equals(strStrict));
				true.ShouldBe(obj.Equals(ppStrict));
			}
		}
		public static void TestBoolConversion() {
			{ // JsonNull should always be a false
				if (JsonNull.instance) { ShouldNotRun(); }
				JsonValue empty = null;
				if (empty) { ShouldNotRun(); }
			}

			{ // JsonBools should behave directly
				JsonBool yes = true;
				JsonBool no = false;
				if (yes) { /* good */ } else { ShouldNotRun(); }
				if (!yes) { ShouldNotRun(); }

				if (no) { ShouldNotRun(); }
				if (!no) { /* good */ } else { ShouldNotRun(); }
			}

			{ // JsonNumber are only false when 0
				JsonNumber zero = 0;
				JsonNumber five = 5;
				var zeroCon = five - five;

				if (zero) { ShouldNotRun(); }
				if (five) { /* good */ } else { ShouldNotRun(); }
				if (zeroCon) { ShouldNotRun(); }
			}

			{ // JsonString are only false when empty
				JsonString empty = "";
				JsonString other = "other";
				if (empty) { ShouldNotRun(); }
				if (other) { /* good */ } else { ShouldNotRun(); }
			}
			{ // JsonObject are always true if not null
				JsonObject empty = new JsonObject();
				JsonObject obj = new JsonObject().Add("ayy", "lmao");
				if (empty) { /* good */ } else { ShouldNotRun(); }
				if (obj) { /* good */ } else { ShouldNotRun(); }
			}
			{ // JsonArray are always true if not null
				JsonArray empty = new JsonArray();
				JsonArray arr = new JsonArray().Add("ayy").Add("lmao");
				if (empty) { /* good */ } else { ShouldNotRun(); }
				if (arr) { /* good */ } else { ShouldNotRun(); }
			}
		}
		public static void TestNumberConversion() {
			{
				JsonString five = "5";
				float fiveF = five; fiveF.ShouldBe(5);
				double fiveD = five; fiveD.ShouldBe(5);
				int fiveI = five; fiveI.ShouldBe(5);
				long fiveL = five; fiveL.ShouldBe(5);
				decimal fiveDC = five; fiveDC.ShouldBe(5);
			}
			{
				JsonBool yes = true;
				float yesF = yes; yesF.ShouldBe(1);
				double yesD = yes; yesD.ShouldBe(1);
				int yesI = yes; yesI.ShouldBe(1);
				long yesL = yes; yesL.ShouldBe(1);
				decimal yesDC = yes; yesDC.ShouldBe(1);

				JsonBool no = false;
				float noF = no; noF.ShouldBe(0);
				double noD = no; noD.ShouldBe(0);
				int noI = no; noI.ShouldBe(0);
				long noL = no; noL.ShouldBe(0);
				decimal noDC = no; noDC.ShouldBe(0);
			}
		}
		public static void TestEqualities() {
			{ // JsonNull
				(JsonNull.instance == null).ShouldBeTrue();
				JsonNull.instance.ShouldEqual(null);

			}

			{ // JsonNumber
				JsonNumber a = 5;
				JsonNumber b = 5;
				JsonNumber c = 10;
				JsonNumber zeroA = 0;
				JsonNumber zeroB = Json.Parse("{z:0}")["z"] as JsonNumber;

				(a == b).ShouldBeTrue();
				(a == 5).ShouldBeTrue();
				(a != c).ShouldBeTrue();
				(c == 10).ShouldBeTrue();
				(zeroA == zeroB).ShouldBeTrue();

				a.ShouldEqual(b);
				a.ShouldEqual(5);
				a.ShouldNotEqual(c);
				c.ShouldEqual(10);
				zeroA.ShouldEqual(0);
				zeroB.ShouldEqual(0);

			}

			{ // Infinity and NaN
				JsonValue jminf = double.NegativeInfinity;
				JsonValue jpinf = double.PositiveInfinity;
				JsonValue jnan = double.NaN;

				double minf = double.NegativeInfinity;
				double pinf = double.PositiveInfinity;
				double nan = double.NaN;

				(jpinf == pinf).ShouldBeTrue();
				(jminf == minf).ShouldBeTrue();
				(jnan == nan).ShouldBeTrue();

				jpinf.ShouldEqual(pinf);
				jminf.ShouldEqual(minf);
				jnan.ShouldEqual(nan);

				jpinf.ShouldNotBe(jminf);
				jpinf.ShouldNotBe(minf);
				jpinf.ShouldNotBe(jnan);
				jpinf.ShouldNotBe(nan);

				jminf.ShouldNotBe(jpinf);
				jminf.ShouldNotBe(pinf);
				jminf.ShouldNotBe(jnan);
				jminf.ShouldNotBe(nan);

				jnan.ShouldNotBe(jpinf);
				jnan.ShouldNotBe(pinf);
				jnan.ShouldNotBe(jminf);
				jnan.ShouldNotBe(minf);
			}

			{ // JsonStrings
				JsonString a = "hullo";
				JsonString b = "hullo";
				JsonString c = "bob saget";

				(a == b).ShouldBeTrue();
				(a == "hullo").ShouldBeTrue();
				(a != c).ShouldBeTrue();
				(c == "bob saget").ShouldBeTrue();

				a.ShouldEqual(b);
				a.ShouldEqual("hullo");
				a.ShouldNotEqual(c);
				c.ShouldEqual("bob saget");
			}

			{ // JsonBool
				JsonBool a = true;
				JsonBool c = false;

				BakaTests.ShouldBeTrue(a);
				(a != c).ShouldBeTrue();
				BakaTests.ShouldBeFalse(c);

			}

			{ // JsonObject
				JsonObject a = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject b = new JsonObject()
					.Add("name", "bob saget")
					.Add("paperTowels", 50)
					.Add("hasBalls", true);

				JsonObject c = new JsonObject()
					.Add("name", "bobby bob bobberton")
					.Add("paperTowels", "three hundred")
					.Add("hasBalls", "yes");

				a.ShouldEqual(b);
				a.ShouldNotBe(b);
				a.ShouldNotBe(c);
				a.ShouldNotEqual(c);

				a.Add("son", c);
				b.Add("son", c.Clone());

				a.ShouldEqual(b);
				a.ShouldNotBe(b);

				a["son"].ShouldBe(c);
				a["son"].ShouldEqual(c);

				b["son"].ShouldNotBe(c);
				b["son"].ShouldEqual(c);
			}
		}

	}
	////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////
	public static class TestExt {

		public class NullableModel {
			public float? nullFloat;
			public int? nullInt;
			public int?[] nullIntArray;
		}

		public static void TestReflectNullables() {
			{
				JsonObject obj = new JsonObject();
				NullableModel model = Json.GetValue<NullableModel>(obj);

				model.nullFloat.HasValue.ShouldBeFalse();
				model.nullInt.HasValue.ShouldBeFalse();
				model.nullIntArray.ShouldBeNull();
			}

			{
				JsonObject obj = new JsonObject();
				obj["nullFloat"] = 5.5;
				obj["nullInt"] = 5;
				JsonArray arr = new JsonArray(1, 2, 3, null, 5, 6);
				obj["nullIntArray"] = arr;

				NullableModel model = Json.GetValue<NullableModel>(obj);
				model.nullFloat.HasValue.ShouldBeTrue();
				model.nullFloat.Value.ShouldBe(5.5f);
				model.nullInt.HasValue.ShouldBeTrue();
				model.nullInt.Value.ShouldBe(5);

				int?[] checkArr = new int?[] { 1, 2, 3, null, 5, 6 };

				model.nullIntArray.ShouldBeSame(checkArr);
				/*(a,b) => {
					if (!a.HasValue == b.HasValue) { return false; }
					if (a.HasValue) {
						if (a.Value != b.Value) {
							return false;
						}
					}
					return true;
				});*/

			}
		}

	}

	public static class TestComments {
		public static void TestObjects(string[] jsonLits, JsonObject expected) {
			for (int i = 0; i < jsonLits.Length; i++) {
				string json = jsonLits[i].Replace('\'', '\"');
				JsonObject value = null;
				try {
					value = Json.Parse<JsonObject>(json.Replace('\'', '\"'));
					value.ShouldNotBe(null);
					value.ShouldEqual(expected);

				} catch (Exception e) {
					throw new Exception($"Element {i} failed, json was:\n{json}\n...Failed to parse above json.\n\tparsed: {value}\n\texpected: {expected}", e);
				}

				Exception caught = null;
				try {
					Json.ParseStrict<JsonObject>(json.Replace('\'', '\"'));
				} catch (Exception e) { caught = e; }
				caught.ShouldNotBe(null);
			}
		}
		public static void TestArrays(string[] jsonLits, JsonArray expected) {
			for (int i = 0; i < jsonLits.Length; i++) {
				string json = jsonLits[i].Replace('\'', '\"');
				JsonArray value = null;
				try {
					value = Json.Parse<JsonArray>(json);
					value.ShouldNotBe(null);
					value.ShouldEqual(expected);

				} catch (Exception e) {
					throw new Exception($"Element {i} failed, json was:\n{json}\n...Failed to parse above json.\n\tparsed: {value}\n\texpected: {expected}", e);
				}

				Exception caught = null;
				try {
					Json.ParseStrict<JsonArray>(json.Replace('\'', '\"'));
				} catch (Exception e) { caught = e; }
				caught.ShouldNotBe(null);
			}
		}
		public static void TestLineComments() {
			string[] empties = new string[] {
					@"{}//Comment after closing",
					@"{} //Comment after closing",

					@"
{ // empty object with comment inside
}",
				@"//empty with comment before
{//inside
	//indented
}//after
//and far after
",
				};
			JsonObject empty = new JsonObject();
			TestObjects(empties, empty);

			string[] smalls = new string[] {
					@"{thing:""value""}//yep",
					@"//
{//
//
thing//
//
://
//
""value""//
//
}//
//",
				};
			JsonObject small = new JsonObject("thing", "value");
			TestObjects(smalls, small);


		}

		public static void TestLineCommentsArrays() {
			string[] empties = new string[] {
					@"[]//",
					@"[] //",
					@"//
[//
//
]//
//",
				};

			JsonArray empty = new JsonArray();
			TestArrays(empties, empty);

			string[] smalls = new string[] {
					@"['a','b','c',1,2,3]//Junk",
					@"
//
[//begin
'a','b','c',//letters
1,2,3,//numbers

]//and
//done",
				@"
//
[//begin
'a','b','c',//letters
1,2,3//numbers

]//and
//done",
				};
			JsonArray small = new JsonArray("a", "b", "c", 1, 2, 3);
			TestArrays(smalls, small);
			
		}
		public static void TestBS() {

			{
				string data = @"
{ // empty object with comment
}
	".Replace('\'', '\"');

				JsonObject obj = Json.Parse(data) as JsonObject;

				obj.ShouldNotBe(null);
				obj.Count.ShouldBe(0);
			}

			{
				string data = @"
{
	thing: 'value', // Explanation
}
	".Replace('\'', '\"');
				JsonObject obj = Json.Parse(data) as JsonObject;

				obj.ShouldNotBe(null);
				obj.Count.ShouldBe(1);
				obj["thing"].stringVal.ShouldBe("value");
			}

			{
				string data = @"
{
	thing: 'value' 
	// Explanation
}
	".Replace('\'', '\"');
				JsonObject obj = Json.Parse(data) as JsonObject;

				obj.ShouldNotBe(null);
				obj.Count.ShouldBe(1);
				obj["thing"].stringVal.ShouldBe("value");
			}
			{
				string data = @"
{
	thing: 'value',
	// Explanation
}
	".Replace('\'', '\"');
				JsonObject obj = Json.Parse(data) as JsonObject;

				obj.ShouldNotBe(null);
				obj.Count.ShouldBe(1);
				obj["thing"].stringVal.ShouldBe("value");
			}

			{
				string data = @"
{
	// Explanation
	thing: 'value' 
}
	".Replace('\'', '\"');
				JsonObject obj = Json.Parse(data) as JsonObject;

				obj.ShouldNotBe(null);
				obj.Count.ShouldBe(1);
				obj["thing"].stringVal.ShouldBe("value");
			}

		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	///////////////////////////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////////////////////////




}
