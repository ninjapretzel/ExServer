using BakaTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Utils {
	/// <summary> Stores values by <see cref="Enum"/> keys, easily reflectable into <see cref="JsonObject"/> </summary>
	/// <typeparam name="K"> Key type. Holds a value for each key defined in its enum definition. </typeparam>
	/// <typeparam name="V"> Value type. </typeparam>
	public class Store<K, V> where K : Enum where V : struct {
		/// <summary> Internal array of values. </summary>
		private V[] values;
		/// <summary> Public indexer. </summary>
		/// <param name="k"> <typeparamref name="K"/> value to get/set mapped <typeparamref name="V"/> value for </param>
		/// <returns> <typeparamref name="V"/> value mapped to <typeparamref name="K"/> value. </returns>
		public V this[K k] {
			get { return values[k.Ord()]; }
			set { values[k.Ord()] = value; }
		}

		/// <summary> Same as public GET indexer. <see cref="this[K]"/></summary>
		/// <param name="k"> <typeparamref name="K"/> key to read. </param>
		/// <returns> Value mapped to <paramref name="k"/> </returns>
		public V Get(K k) { return values[Enum<K>.indexes[k]]; }
		/// <summary> Same as public SET indexer. <see cref="this[K]"/> </summary>
		/// <param name="k"> <typeparamref name="K"/> key to read. </param>
		/// <param name="v"> <typeparamref name="V"/> value to set </param>
		public void Set(K k, V v) { values[Enum<K>.indexes[k]] = v; }
		/// <summary> Indexer by string. </summary>
		/// <param name="k"> <see cref="string"/> key </param>
		/// <returns> value stored with the given string, or `default(<typeparamref name="V"/>)` if it's invalid. </returns>
		public V this[string k] {
			get { return (Enum<K>.byName.ContainsKey(k)) ? this[Enum<K>.byName[k]] : default(V); }
			set { if (Enum<K>.byName.ContainsKey(k)) { this[Enum<K>.byName[k]] = value; } }
		}
		/// <summary> Valid <see cref="string"/> values for use with <see cref="this[string]"/> </summary>
		public IEnumerable<string> Keys { get { return Enum<K>.names; } }

		/// <summary> Basic constructor.</summary>
		public Store() { values = new V[Enum<K>.Count]; }
		/// <summary> Copy constructor </summary>
		/// <param name="other"> Other Store to copy </param>
		public Store(Store<K, V> other) : this() {
			foreach (var k in Enum<K>.items) { this[k] = other[k]; }
		}
		/// <summary> Copy constructor, by array </summary>
		/// <param name="values"> Array to copy </param>
		public Store(params V[] values) {
			if (values.Length != Enum<K>.Count) {
				throw new ArgumentException($"Not enough values provided for {GetType()}.  Expected {Enum<K>.Count} but got {values.Length}");
			}
			this.values = new V[Enum<K>.Count];
			Array.Copy(values, this.values, Enum<K>.Count);
		}

		/// <summary> Deserialization constructor from <see cref="JsonObject"/> </summary>
		/// <param name="data"> Data <see cref="JsonObject"/> to deserialize </param>
		public Store(JsonObject data) : this() {
			foreach (var pair in data) {
				string k = pair.Key;
				if (Enum<K>.byName.ContainsKey(k)) {
					this[Enum<K>.byName[k]] = Json.GetValue<V>(pair.Value);
				}
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (obj is Store<K, V> other) {
				foreach (var k in Enum<K>.items) {
					if (!this[k].Equals(other[k])) { return false; }
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		/// <summary> Directly serializes this <see cref="Store{K, V}"/> into a <see cref="JsonObject"/></summary>
		/// <returns> <see cref="JsonObject"/> holding all reflected <typeparamref name="K"/>-><typeparamref name="V"/> mappings </returns>
		public JsonObject ToJsonObject() {
			JsonObject obj = new JsonObject();
			foreach (var k in Enum<K>.items) {
				obj[Enum<K>.values[k]] = Json.Reflect(this[k]);
			}
			return obj;
		}
		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder();
			str.Append("{");
			K[] items = Enum<K>.items;
			for (int i = 0; i < items.Length; i++) {
				str.Append($"\"{items[i]}\": {this[items[i]]}");
				if (i < items.Length - 1) { str.Append(','); }
			}
			str.Append("}");
			return str.ToString();
		}
		/// <summary> Combines two stores by ratios. Works with <typeparamref name="V"/> of float/double/decimal between 0 and 1. </summary>
		/// <param name="a"> First Store to combine </param>
		/// <param name="b"> Second Store to combine </param>
		/// <returns> Store derived from two parameters. </returns>
		public static Store<K, V> Combine(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { 
				dynamic aa = a[k]; dynamic bb = b[k];
				result[k] = (V) (1.0 - ((1.0 - aa) * (1.0 - bb)));
			}
			return result;
		}

		#region Operators
		public static Store<K, V> operator -(Store<K, V> a) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = -(dynamic)a[k]; }
			return result;
		}
		public static Store<K, V> operator !(Store<K, V> a) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = !(dynamic)a[k]; }
			return result;
		}
		public static Store<K, V> operator ~(Store<K, V> a) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = ~(dynamic)a[k]; }
			return result;
		}

		public static Store<K, V> operator +(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] + (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator -(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] - (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator *(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] * (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator /(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] / (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator %(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] % (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator &(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] & (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator |(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] | (dynamic)b[k]); }
			return result;
		}
		public static Store<K, V> operator ^(Store<K, V> a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] ^ (dynamic)b[k]); }
			return result;
		}





		public static Store<K, V> operator +(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] + b); }
			return result;
		}
		public static Store<K, V> operator -(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] - b); }
			return result;
		}
		public static Store<K, V> operator *(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] * b); }
			return result;
		}
		public static Store<K, V> operator /(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] / b); }
			return result;
		}
		public static Store<K, V> operator %(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] % b); }
			return result;
		}
		public static Store<K, V> operator &(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] + b); }
			return result;
		}
		public static Store<K, V> operator |(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] + b); }
			return result;
		}
		public static Store<K, V> operator ^(Store<K, V> a, dynamic b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a[k] + b); }
			return result;
		}

		public static Store<K, V> operator +(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a + b[k]); }
			return result;
		}
		public static Store<K, V> operator -(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a - b[k]); }
			return result;
		}
		public static Store<K, V> operator *(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a * b[k]); }
			return result;
		}
		public static Store<K, V> operator /(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a / b[k]); }
			return result;
		}
		public static Store<K, V> operator %(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a % b[k]); }
			return result;
		}
		public static Store<K, V> operator &(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a + b[k]); }
			return result;
		}
		public static Store<K, V> operator |(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a + b[k]); }
			return result;
		}
		public static Store<K, V> operator ^(dynamic a, Store<K, V> b) {
			Store<K, V> result = new Store<K, V>();
			foreach (var k in Enum<K>.items) { result[k] = (V)(a + b[k]); }
			return result;
		}
		#endregion

	}

	/// <summary> Stores floats by <see cref="Enum"/> keys, easily reflectable into <see cref="JsonObject"/> </summary>
	/// <typeparam name="K"> Key type. Holds a value for each key defined in its enum definition. </typeparam>
	public class Store<K> where K : Enum {
		/// <summary> Internal array of values. </summary>
		private float[] values;
		/// <summary> Public indexer. </summary>
		/// <param name="k"> <typeparamref name="K"/> value to get/set mapped <typeparamref name="V"/> value for </param>
		/// <returns> <typeparamref name="V"/> value mapped to <typeparamref name="K"/> value. </returns>
		public float this[K k] {
			get { return values[k.Ord()]; }
			set { values[k.Ord()] = value; }
		}

		/// <summary> Same as public GET indexer. <see cref="this[K]"/></summary>
		/// <param name="k"> <typeparamref name="K"/> key to read. </param>
		/// <returns> Value mapped to <paramref name="k"/> </returns>
		public float Get(K k) { return values[Enum<K>.indexes[k]]; }
		/// <summary> Same as public SET indexer. <see cref="this[K]"/> </summary>
		/// <param name="k"> <typeparamref name="K"/> key to read. </param>
		/// <param name="v"> <typeparamref name="V"/> value to set </param>
		public void Set(K k, float v) { values[Enum<K>.indexes[k]] = v; }
		/// <summary> Indexer by string. </summary>
		/// <param name="k"> <see cref="string"/> key </param>
		/// <returns> value stored with the given string, or `default(<typeparamref name="V"/>)` if it's invalid. </returns>
		public float this[string k] {
			get { return (Enum<K>.byName.ContainsKey(k)) ? this[Enum<K>.byName[k]] : 0f; }
			set { if (Enum<K>.byName.ContainsKey(k)) { this[Enum<K>.byName[k]] = value; } }
		}
		/// <summary> Valid <see cref="string"/> values for use with <see cref="this[string]"/> </summary>
		public IEnumerable<string> Keys { get { return Enum<K>.names; } }

		/// <summary> Basic constructor.</summary>
		public Store() { values = new float[Enum<K>.Count]; }
		/// <summary> Copy constructor </summary>
		/// <param name="other"> Other Store to copy </param>
		public Store(Store<K, float> other) : this() {
			foreach (var k in Enum<K>.items) { this[k] = other[k]; }
		}
		/// <summary> Copy constructor, by array </summary>
		/// <param name="values"> Array to copy </param>
		public Store(params float[] values) {
			if (values.Length != Enum<K>.Count) {
				throw new ArgumentException($"Not enough values provided for {GetType()}.  Expected {Enum<K>.Count} but got {values.Length}");
			}
			this.values = new float[Enum<K>.Count];
			Array.Copy(values, this.values, Enum<K>.Count);
		}

		/// <summary> Deserialization constructor from <see cref="JsonObject"/> </summary>
		/// <param name="data"> Data <see cref="JsonObject"/> to deserialize </param>
		public Store(JsonObject data) : this() {
			foreach (var pair in data) {
				string k = pair.Key;
				if (Enum<K>.byName.ContainsKey(k)) {
					this[Enum<K>.byName[k]] = Json.GetValue<float>(pair.Value);
				}
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (obj is Store<K> other) {
				foreach (var k in Enum<K>.items) {
					if (!this[k].Equals(other[k])) { return false; }
				}
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		/// <summary> Directly serializes this <see cref="Store{K, V}"/> into a <see cref="JsonObject"/></summary>
		/// <returns> <see cref="JsonObject"/> holding all reflected <typeparamref name="K"/>-><typeparamref name="V"/> mappings </returns>
		public JsonObject ToJsonObject() {
			JsonObject obj = new JsonObject();
			foreach (var k in Enum<K>.items) {
				obj[Enum<K>.values[k]] = Json.Reflect(this[k]);
			}
			return obj;
		}
		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder();
			str.Append("{");
			K[] items = Enum<K>.items;
			for (int i = 0; i < items.Length; i++) {
				str.Append($"\"{items[i]}\": {this[items[i]]}");
				if (i < items.Length - 1) { str.Append(','); }
			}
			str.Append("}");
			return str.ToString();
		}

		/// <summary> Combines two stores by ratios. Works with <typeparamref name="V"/> of float/double/decimal between 0 and 1. </summary>
		/// <param name="a"> First Store to combine </param>
		/// <param name="b"> Second Store to combine </param>
		/// <returns> Store derived from two parameters. </returns>
		public static Store<K> CombineAsRatios(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) {
				float aa = a[k]; float bb = b[k];
				result[k] = (1.0f - ((1.0f - aa) * (1.0f - bb)));
			}
			return result;
		}

		public Store<K> CombineAsRatios(Store<K> other) { return CombineAsRatios(this, other); }

		#region Operators
		public static Store<K> operator -(Store<K> a) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = -a[k]; }
			return result;
		}
		public static Store<K> operator +(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] + b[k]); }
			return result;
		}
		public static Store<K> operator -(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] - b[k]); }
			return result;
		}
		public static Store<K> operator *(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] * b[k]); }
			return result;
		}
		public static Store<K> operator /(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] / b[k]); }
			return result;
		}
		public static Store<K> operator %(Store<K> a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] % b[k]); }
			return result;
		}




		public static Store<K> operator +(Store<K> a, float b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] + b); }
			return result;
		}
		public static Store<K> operator -(Store<K> a, float b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] - b); }
			return result;
		}
		public static Store<K> operator *(Store<K> a, float b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] * b); }
			return result;
		}
		public static Store<K> operator /(Store<K> a, float b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] / b); }
			return result;
		}
		public static Store<K> operator %(Store<K> a, float b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a[k] % b); }
			return result;
		}

		public static Store<K> operator +(float a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a + b[k]); }
			return result;
		}
		public static Store<K> operator -(float a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a - b[k]); }
			return result;
		}
		public static Store<K> operator *(float a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a * b[k]); }
			return result;
		}
		public static Store<K> operator /(float a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a / b[k]); }
			return result;
		}
		public static Store<K> operator %(float a, Store<K> b) {
			Store<K> result = new Store<K>();
			foreach (var k in Enum<K>.items) { result[k] = (a % b[k]); }
			return result;
		}
		#endregion

	}

	public static class StoreKV_Tests {
		public enum Keys { A, B, C, D }

		public static void TestBasics() {
			Store<Keys, int> store = new Store<Keys, int>();
			store[Keys.A] = 1;
			store[Keys.B] = 2;
			store[Keys.C] = 3;
			store[Keys.D] = 4;

			store["A"].ShouldBe(1);
			store["B"].ShouldBe(2);
			store["C"].ShouldBe(3);
			store["D"].ShouldBe(4);
		}

		public static void TestOperators() {
			Store<Keys, int> a = new Store<Keys, int>(1, 2, 3, 4);
			Store<Keys, int> b = new Store<Keys, int>(5, 6, 7, 8);

			var c = -a;
			c.ShouldEqual(new Store<Keys, int>(-1, -2, -3, -4));
			var d = a + b;
			d.ShouldEqual(new Store<Keys, int>(6, 8, 10, 12));
			var e = a - b;
			e.ShouldEqual(new Store<Keys, int>(-4, -4, -4, -4));
			var f = a * b;
			f.ShouldEqual(new Store<Keys, int>(5, 12, 21, 32));
			var g = b / a;
			g.ShouldEqual(new Store<Keys, int>(5, 3, 2, 2));
		}

		public static void TestCombine() {
			Store<Keys, float> a = new Store<Keys, float>(.50f, 1f/3f, .25f, .20f);
			Store<Keys, float> b = new Store<Keys, float>(.20f, .10f, .20f, .20f);

			var result = Store<Keys, float>.Combine(a, b);
			result.ShouldEqual(new Store<Keys, float>(.6f, .4f, .4f, .36f));

		}

	}

	public static class StoreK_Tests {
		public enum Keys { A, B, C, D }

		public static void TestBasics() {
			Store<Keys> store = new Store<Keys>();
			store[Keys.A] = 1f;
			store[Keys.B] = 2f;
			store[Keys.C] = 3f;
			store[Keys.D] = 4f;

			store["A"].ShouldBe(1);
			store["B"].ShouldBe(2);
			store["C"].ShouldBe(3);
			store["D"].ShouldBe(4);
		}

		public static void TestOperators() {
			Store<Keys> a = new Store<Keys>(1, 2, 3, 4);
			Store<Keys> b = new Store<Keys>(5, 6, 7, 8);

			var c = -a;
			c.ShouldEqual(new Store<Keys>(-1, -2, -3, -4));
			var d = a + b;
			d.ShouldEqual(new Store<Keys>(6, 8, 10, 12));
			var e = a - b;
			e.ShouldEqual(new Store<Keys>(-4, -4, -4, -4));
			var f = a * b;
			f.ShouldEqual(new Store<Keys>(5, 12, 21, 32));
			var g = b / a;
			g.ShouldEqual(new Store<Keys>(5, 3, 2f+(1f/3f), 2));
		}

		public static void TestCombine() {
			Store<Keys, float> a = new Store<Keys, float>(.50f, 1f / 3f, .25f, .20f);
			Store<Keys, float> b = new Store<Keys, float>(.20f, .10f, .20f, .20f);

			var result = Store<Keys, float>.Combine(a, b);
			result.ShouldEqual(new Store<Keys, float>(.6f, .4f, .4f, .36f));

		}

	}
}
