using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Libs {
	/// <summary> Misc extensions for handling different generic types of data. </summary>
	public static class DataExt {

		public static float ParseFloat(this string s) { return float.Parse(s); }
		public static int ParseInt(this string s) { return int.Parse(s); }
		public static byte ParseByte(this string s) { return byte.Parse(s, NumberStyles.HexNumber); }
		public static List<string> ParseStringList(this string s) { return s.ParseStringList(','); }
		public static List<string> ParseStringList(this string s, char delim) { return s.Split(delim).ToList(); }

		/// <summary> Quickly hashes a byte array into an int32 </summary>
		/// <param name="data">Byte array to hash</param>
		/// <returns>int32 hash based off of data </returns>
		public static int SimpleHash(this byte[] data) {
			if (data != null) {
				int result = data.Length * data.Length * 31337;
				for (int i = 0; i < data.Length; ++i) {
					result ^= (data[i] << ((i % 4) * 8));
				}
				return result;
			} else {
				return 0;
			}
		}

		/// <summary> Chops an array into a sub-array (analogous to substring) </summary>
		/// <typeparam name="T">Generic type</typeparam>
		/// <param name="array">Source data to chop</param>
		/// <param name="size">Size of sub-array</param>
		/// <param name="start">Start position in source data</param>
		/// <returns>Array of same type, of Length size, of elements in the source array </returns>
		public static T[] Chop<T>(this T[] array, int size, int start = 0) {
			if (start >= array.Length) { return null; }
			if (size + start > array.Length) {
				size = array.Length - start;
			}
			T[] chopped = new T[size];
			for (int i = 0; i < size; ++i) {
				chopped[i] = array[i + start];
			}
			return chopped;
		}

		/// <summary> 
		/// Converts a byte to its two character hex string.
		/// Ex. 
		/// 255 becomes "FF"
		/// 200 becomes "C8"
		/// 64 becomes "40"
		/// </summary>
		/// <param name="b">byte to convert</param>
		/// <returns>Byte converted to hex string</returns>
		public static string ToHex(this byte b) {
			string s = "";

			byte large = (byte)(b >> 4);
			byte small = (byte)(b % 16);

			char a = 'A';
			char zero = '0';

			if (large >= 10) {
				s += (char)(a + large - 10);
			} else {
				s += (char)(zero + large);
			}

			if (small >= 10) {
				s += (char)(a + small - 10);
			} else {
				s += (char)(zero + small);
			}

			return s;
		}


		/// <summary> Get the last element in a list</summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="list">List to grab the last element of</param>
		/// <returns>Last element of list</returns>
		public static T LastElement<T>(this IList<T> list) { if (list.Count == 0) { return default(T); } return list[list.Count - 1]; }

		/// <summary> Get the nth element from the end of the list </summary>
		/// <typeparam name="T">Generic Type</typeparam> 
		/// <param name="list">List to grab from</param>
		/// <param name="offset">elements from the end to grab. (0 gives the last element) </param>
		/// <returns>Element (offset) elements from the end of the list</returns>
		public static T FromEnd<T>(this IList<T> list, int offset) { return list[list.Count - 1 - offset]; }

		/// <summary> Get the first index of an element satisfying some criteria </summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="list">List to search in</param>
		/// <param name="search">Method to check each element</param>
		/// <returns>Index of first object that satisfies (search), or -1 if no elements do</returns>
		public static int IndexOf<T>(this IList<T> list, Func<T, bool> search) {
			for (int i = 0; i < list.Count; ++i) {
				if (search(list[i])) { return i; }
			}
			return -1;
		}



		/// <summary>Searches an array for the first element that satisfies a condition</summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="list"></param>
		/// <param name="search"></param>
		/// <returns></returns>
		public static T Find<T>(this IEnumerable<T> list, Func<T, bool> search) {
			foreach (T t in list) {
				if (search(t)) { return t; }
			}
			return default(T);
		}

		/// <summary> Calls a given function for each element in an IEnumerable </summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="list">IEnumerable of stuff to loop over</param>
		/// <param name="func">Action to call on each element in list</param>
		public static void Each<T>(this IEnumerable<T> list, Action<T> func) { foreach (T t in list) { func(t); } }

		/// <summary> Calls a given function for each element in an IList, and its index </summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="list">IList of stuff to loop over</param>
		/// <param name="func">Action to call on each element in the list, paired with its index</param>
		public static void Each<T>(this IList<T> list, Action<T, int> func) {
			int i = 0;
			foreach (T t in list) { func(t, i++); }
		}
		/// <summary> Calls a given function for each pair in an IDictionary</summary>
		/// <typeparam name="K">Generic type of Key</typeparam>
		/// <typeparam name="V">Generic type of Value</typeparam>
		/// <param name="dict">IDictionary of pairs to loop over</param>
		/// <param name="func">Action to call on each key,value pair in the dictionary</param>
		public static void Each<K, V>(this IDictionary<K, V> dict, Action<K, V> func) { foreach (var pair in dict) { func(pair.Key, pair.Value); } }

		/// <summary> Map elements from source type to destination type</summary>
		/// <typeparam name="SourceType">Source Generic Type</typeparam>
		/// <typeparam name="DestType">Destination Generic Type</typeparam>
		/// <param name="data">Collection to loop over</param>
		/// <param name="mapper">Function to map elements from SourceType to DestType</param>
		/// <returns>List of elements mapped from SourceType to DestType </returns>
		public static List<DestType> Map<SourceType, DestType>(this IEnumerable<SourceType> data, Func<SourceType, DestType> mapper) {
			List<DestType> mapped = new List<DestType>();
			foreach (var d in data) { mapped.Add(mapper(d)); }
			return mapped;
		}

		/// <summary> Filter elements from an IEnumerable based on a pass/fail filter. </summary>
		/// <typeparam name="T">Generic Type</typeparam>
		/// <param name="data">IEnumerable of elements to filter</param>
		/// <param name="filter">Function to call to see if an element should be filtered or not</param>
		/// <returns>List of all elements that actually pass the filter</returns>
		public static List<T> Filter<T>(this IEnumerable<T> data, Func<T, bool> filter) {
			List<T> passed = new List<T>();
			foreach (var d in data) {
				if (filter(d)) { passed.Add(d); }
			}
			return passed;
		}

		/// <summary>Reduce elements in an IEnumerable collection with a given function </summary>
		/// <typeparam name="T">Generic Type of collection</typeparam>
		/// <typeparam name="TResult">Generic Type of expected result</typeparam>
		/// <param name="data">IEnumerable collection to loop over</param>
		/// <param name="reducer">Function to use to 'reduce' each element in the collection </param>
		/// <param name="startingValue">Value to begin with to reduce the first element with </param>
		/// <returns>Entire collection reduced into one single value</returns>
		public static TResult Reduce<T, TResult>(this IEnumerable<T> data, Func<TResult, T, TResult> reducer, TResult startingValue) {
			var val = startingValue;
			foreach (var d in data) {
				val = reducer(val, d);
			}
			return val;
		}

		#region times methods
		/// <summary> Perform a function n times</summary>
		/// <param name="n">Count of times to perform the function </param>
		/// <param name="func">Function to perform </param>
		public static void Times(this int n, Action func) { for (int i = 0; i < n; i++) { func(); } }

		/// <summary> Perform a function n times, passing in a count each time </summary>
		/// <param name="n">Count of times to perform the function </param>
		/// <param name="func">Function to perform </param>
		public static void Times(this int n, Action<int> func) { for (int i = 0; i < n; i++) { func(i); } }

		/// <summary> Perform a function n times</summary>
		/// <param name="n">Count of times to perform the function </param>
		/// <param name="func">Function to perform </param>
		public static void Times(this long n, Action func) { for (long i = 0; i < n; i++) { func(); } }

		/// <summary> Perform a function n times, passing in a count each time </summary>
		/// <param name="n">Count of times to perform the function </param>
		/// <param name="func">Function to perform </param>
		public static void Times(this long n, Action<long> func) { for (long i = 0; i < n; i++) { func(i); } }
		#endregion

	}


	public static class DataUtils {

		static Encoding utf8 = Encoding.UTF8;
		static Encoding ascii = Encoding.ASCII;
		/// <summary> Turns a string into a byte[] using ASCII </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by ASCII encoding </returns>
		public static byte[] ToBytes(this string s) { return ascii.GetBytes(s); }
		/// <summary> Turns a string into a byte[] using UTF8 </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by UTF8 encoding </returns>
		public static byte[] ToBytesUTF8(this string s) { return utf8.GetBytes(s); }

		/// <summary> Reads a byte[] as if it is an ASCII encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> ASCII string created from the byte[] </returns>
		public static string GetString(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return ascii.GetString(b, 0, length);
		}

		/// <summary> Reads a byte[] as if it is a UTF8 encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> UTF8 string created from the byte[] </returns>
		public static string GetStringUTF8(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return utf8.GetString(b, 0, length);
		}

		public static void LogEach<T>(this T[] array) { array.LogEach(1); }
		public static void LogEach<T>(this T[] array, int perLine) {
			StringBuilder str = new StringBuilder();

			for (int i = 0; i < array.Length; i++) {
				str.Append(array[i].ToString());
				str.Append(", ");
				if ((1 + i) % perLine == 0) { str.Append('\n'); }
			}
			//Debug.Log(str.ToString());
		}

		//Thanks, William
		/// <summary> Takes any byte[] and preforms a simple hash algorithm over it. </summary>
		/// <param name="data"> byte[] to process </param>
		/// <returns> Simple hash value created from the given byte[] </returns>
		public static int SimpleHash(this byte[] data) {
			if (data != null) {
				int res = data.Length * data.Length * 31337;
				for (int i = 0; i < data.Length; i++) {
					res ^= (data[i] << ((i % 4) * 8));
				}
				return res;
			} else {
				return 0;
			}
		}

		/// <summary> Take a copy of a sub-region of a byte[] </summary>
		/// <param name="array"> byte[] to chop </param>
		/// <param name="size"> maximum size of resulting sub-array </param>
		/// <param name="start"> start index </param>
		/// <returns> sub-region from given byte[], of max length <paramref name="size"/> starting from index <paramref name="start"/> in the original <paramref name="array"/>. </returns>
		public static byte[] Chop(this byte[] array, int size, int start = 0) {
			if (start >= array.Length) { return null; }
			if (size + start > array.Length) {
				size = array.Length - start;
			}
			byte[] chopped = new byte[size];
			for (int i = 0; i < size; i++) {
				chopped[i] = array[i + start];
			}
			return chopped;
		}
	}

	public enum Endianness { Big, Little }

	/// <summary> Holds extensions for <see cref="BinaryReader"/></summary>
	public static class BinaryReaderEx {
		/// <summary> Cached value of <see cref="BitConverter.IsLittleEndian"/>. Does not change over runtime. </summary>
		/// <remarks> Keeping this close should help the cache out... probably... </remarks>
		public static Endianness sysEnd = BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;

		/// <summary> Reads an unsigned 64-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static ulong UInt64(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			byte e = reader.ReadByte();
			byte f = reader.ReadByte();
			byte g = reader.ReadByte();
			byte h = reader.ReadByte();
			long i1, i2;

			if (end == Endianness.Big) {
				i1 = (a << 24) | (b << 16) | (c << 8) | (d);
				i2 = (e << 24) | (f << 16) | (g << 8) | (h);

			} else {
				i1 = (h << 24) | (g << 16) | (f << 8) | (e);
				i2 = (d << 24) | (c << 16) | (b << 8) | (a);
			}

			return (ulong)((i1 << 32) | i2);
		}

		/// <summary> Reads a 64-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static long Int64(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			byte e = reader.ReadByte();
			byte f = reader.ReadByte();
			byte g = reader.ReadByte();
			byte h = reader.ReadByte();
			long i1, i2;

			if (end == Endianness.Big) {
				i1 = (a << 24) | (b << 16) | (c << 8) | (d);
				i2 = (e << 24) | (f << 16) | (g << 8) | (h);

			} else {
				i1 = (h << 24) | (g << 16) | (f << 8) | (e);
				i2 = (d << 24) | (c << 16) | (b << 8) | (a);
			}

			return ((i1 << 32) | i2);
		}

		/// <summary> Reads an unsigned 32-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static uint UInt32(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			return (uint)((end == Endianness.Big) ?
				(a << 24) | (b << 16) | (c << 8) | (d) :
				(d << 24) | (c << 16) | (b << 8) | (a));
		}

		/// <summary> Reads a 32-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static int Int32(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			return ((end == Endianness.Big) ?
				(a << 24) | (b << 16) | (c << 8) | (d) :
				(d << 24) | (c << 16) | (b << 8) | (a));
		}

		/// <summary> Reads an unsigned 16-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static ushort UInt16(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			return (ushort)((end == Endianness.Big ?
				(a << 8) | (b) :
				(b << 8) | (a)));

		}

		/// <summary> Reads a 16-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static short Int16(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			return (short)((end == Endianness.Big ?
				(a << 8) | (b) :
				(b << 8) | (a)));

		}

	}


	/// <summary> Implements some methods for <see cref="ConcurrentDictionary{TKey, TValue}"/> which it should have. </summary>
	public static class ConcurrentDictionaryEx {
		/// <summary> Acts the same as a <see cref="Dictionary{TKey, TValue}"/>'s Remove method. </summary>
		/// <typeparam name="TKey"> Key type </typeparam>
		/// <typeparam name="TValue"> Value type </typeparam>
		/// <param name="self"> <see cref="ConcurrentDictionary{TKey, TValue}"/> to remove from </param>
		/// <param name="key"> Key to remove </param>
		/// <returns> True, if the key was present. False if it was not present. </returns>
		public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) {
			return ((IDictionary<TKey, TValue>)self).Remove(key);
		}
		/// <summary> Calls the <see cref="ConcurrentDictionary{TKey, TValue}"/>'s TryRemove method, and discards the out parameter. </summary>
		/// <typeparam name="TKey"> Key type </typeparam>
		/// <typeparam name="TValue"> Value type </typeparam>
		/// <param name="self"> <see cref="ConcurrentDictionary{TKey, TValue}"/> to remove from </param>
		/// <param name="key"> Key to remove </param>
		/// <returns> True, if the key was present. False if it was not present. </returns>
		public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) {
			TValue ignored;
			return self.TryRemove(key, out ignored);
		}
	}


}
