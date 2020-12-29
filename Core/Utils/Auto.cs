using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Utils {
	/// <summary> Class holding general-purpose automation. </summary>
	public static class Auto {
		/// <summary> Recursively initialize every reference-type field in an object of a given type with empty-constructed objects,
		/// much like the default behaviour of a `struct`. </summary>
		/// <param name="type"> <see cref="Type"/> of object to initialize </param>
		/// <returns> Initialized object </returns>
		public static object Init(Type type) {
			object obj = Activator.CreateInstance(type);

			var fields = type.GetFields();
			foreach (var info in fields) {
				if (!typeof(ValueType).IsAssignableFrom(info.FieldType)) {
					info.SetValue(obj, Init(info.FieldType));
				}
			}
			return obj;
		}
		/// <summary> Generic version of <see cref="Init(Type)"/>, 
		/// Recursively initialize every reference-type field in an object of a given type with empty-constructed objects,
		/// much like the default behaviour of a `struct`. </summary>
		/// <typeparam name="T"> Generic type to initialize </typeparam>
		/// <returns> Initialized object </returns>
		public static T Init<T>() { return (T)Init(typeof(T)); }
	}
}
