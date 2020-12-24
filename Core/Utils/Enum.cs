using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Ex.Utils {

	/// <summary> Helper class that caches information about <see cref="System.Enum"/>s </summary>
	/// <typeparam name="T"> Generic <see cref="System.Enum"/> type. </typeparam>
	public class Enum<T> where T : System.Enum {
		/// <summary> Baked enum value dictionary </summary>
		public static readonly Dictionary<T, string> values = Prepare(); // Get all static fields populated on class load.
		/// <summary> Array of <typeparamref name="T"/> values </summary>
		public static T[] items { get; private set; }
		/// <summary> Array of <see cref="string"/> names of each, in parallel to <see cref="items"/></summary>
		public static string[] names { get; private set; }
		/// <summary> <see cref="IDictionary{TKey, TValue}"/> of indexes of <typeparamref name="T"/> values in <see cref="items"/> </summary>
		public static IDictionary<T, int> indexes { get; private set; }
		/// <summary> <see cref="IDictionary{TKey, TValue}"/> of ordinal values of <typeparamref name="T"/> values in <see cref="items"/> </summary>
		public static IDictionary<T, int> ordinals { get; private set; }
		/// <summary> <see cref="IDictionary{TKey, TValue}"/> of <typeparamref name="T"/> values by <see cref="string"/> names</summary>
		public static IDictionary<string, T> byName { get; private set; }
		/// <summary> Get the number of items in the enum </summary>
		public static int Count { get { return items.Length; } }

		/// <summary> Bakes enum value dictionary </summary>
		private static Dictionary<T, string> Prepare() {
			Dictionary<T, string> result = new Dictionary<T, string>();
			indexes = new Dictionary<T, int>();
			ordinals = new Dictionary<T, int>();
			byName = new Dictionary<string, T>();

			names = Enum.GetNames(typeof(T));
			items = (T[])Enum.GetValues(typeof(T));

			for (int i = 0; i < names.Length; i++) {
				string name = names[i];
				T value = items[i];
				indexes[value] = i;
				ordinals[value] = Convert.ToInt32(value);
				result[value] = name;
				byName[name] = value;
			}
			indexes = new ReadOnlyDictionary<T, int>(indexes);
			ordinals = new ReadOnlyDictionary<T, int>(ordinals);
			byName = new ReadOnlyDictionary<string, T>(byName);
			return result;
		}

	}
	/// <summary> Extension methods for <see cref="Enum{T}"/></summary>
	public static class EnumExt {
		/// <summary> Convert a given <see cref="Enum"/> value to the integer value that backs it. </summary>
		/// <typeparam name="T"> Generic enum type </typeparam>
		/// <param name="t"> Value to convert to int </param>
		/// <returns> int value </returns>
		public static int Ord<T>(this T t) where T : Enum { return Convert.ToInt32(t); }

		/// <summary> Gets the string name of an Enum </summary>
		/// <typeparam name="T"> Generic <see cref="System.Enum"/> type </typeparam>
		/// <param name="t"> Value to get name of </param>
		/// <returns> String representation of <paramref name="t"/> </returns>
		public static string Name<T>(this T t) where T : System.Enum {
			return Enum<T>.values.ContainsKey(t) ? Enum<T>.values[t] : null;
		}

		/// <summary> Get if an Enum value is a valid value </summary>
		/// <typeparam name="T"> Generic <see cref="System.Enum"/> type </typeparam>
		/// <param name="t"> Value to see if is valid. </param>
		/// <returns> True if the <paramref name="t"/> contains a value defined in the enum, otherwise false.</returns>
		public static bool IsValid<T>(this T t) where T : System.Enum {
			return Enum<T>.values.ContainsKey(t);
		}
		/// <summary> Get the successor to the given <paramref name="t"/> enum value </summary>
		/// <typeparam name="T"> Generic <see cref="System.Enum"/> type (given it's valid) </typeparam>
		/// <param name="t"> Value to get next of </param>
		/// <returns> Next value defined in the enum. </returns>
		public static T Next<T>(this T t) where T : System.Enum {
			int ind = Enum<T>.indexes[t] + 1;
			return (ind >= Enum<T>.Count) ? Enum<T>.items[0] : Enum<T>.items[ind];
		}
	}

}
