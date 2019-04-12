using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Ex.Utils {

	/// <summary> Class holding code for packing structs </summary>
	public static class Pack {

		/// <summary> Pack a struct into a Base64 string </summary>
		/// <typeparam name="T"> Generic type of parameter </typeparam>
		/// <param name="value"> Parameter to pack </param>
		/// <returns> Base64 from converting struct into byte[], then encoding byte array </returns>
		public static string Base64<T>(T value) where T : struct {
			byte[] copy = Unsafe.ToBytes(value);
			return Convert.ToBase64String(copy);
		}

	}

	/// <summary> Class holding code for unpacking structs </summary>
	public static class Unpack {
		
		/// <summary> Unpack Base64 encoded struct into a struct by output parameter </summary>
		/// <typeparam name="T"> Generic type of parameter to unpack </typeparam>
		/// <param name="encoded"> Encoded Base64 string </param>
		/// <param name="ret"> Return location </param>
		/// <returns> True if successful, false if failure, and sets ret to the resulting unpacked data, or default, respectively.  </returns>
		public static bool Base64<T>(string encoded, out T ret) where T : struct {
			try {
				byte[] bytes = Convert.FromBase64String(encoded);
				ret = Unsafe.FromBytes<T>(bytes);
				return true;
			} catch (Exception) {
				ret = default(T);
				return false;
			}
		}

		/// <summary> Unpack Base 64 </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="encoded"></param>
		/// <returns></returns>
		public static T Base64<T>(string encoded) where T : struct {
			try {
				byte[] bytes = Convert.FromBase64String(encoded);
				return Unsafe.FromBytes<T>(bytes);
			} catch (Exception) {
				return default(T);
			}
		}

	}
}
