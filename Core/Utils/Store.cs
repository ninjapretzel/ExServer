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

		public V Get(K k) { return values[Enum<K>.indexes[k]]; }
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
		/// <param name="other"></param>
		public Store(Store<K, V> other) : this() {
			foreach (var k in Enum<K>.items) { this[k] = other[k]; }
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

	}
}
