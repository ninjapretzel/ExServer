using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using BakaTest;

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
				Unsafe.FromBytes(bytes, out ret);
				return true;
			} catch (Exception) {
				ret = default(T);
				return false;
			}
		}

		/// <summary> Unpack Base 64 </summary>
		/// <typeparam name="T"> Generic type to unpack </typeparam>
		/// <param name="encoded"> Encoded Base64 string </param>
		/// <returns> Unpacked data, or default value if anything failed (data size mismatch). </returns>
		public static T Base64<T>(string encoded) where T : struct {
			try {
				byte[] bytes = Convert.FromBase64String(encoded);
				return Unsafe.FromBytes<T>(bytes);
			} catch (Exception) {
				return default(T);
			}
		}

	}

	public static class PackUnpack_Tests {
		public static void TestEncodeDecode() {

			{
				Vector3 v = new Vector3(123,456,789);
				string encoded = Pack.Base64(v);
				if (BitConverter.IsLittleEndian) {
					encoded.ShouldBe("AAD2QgAA5EMAQEVE");
				} else {
					encoded.ShouldBe("QvYAAEPkAABERUAA");
				}
				Vector3 decoded = Unpack.Base64<Vector3>(encoded);
				decoded.ShouldBe<Vector3>(new Vector3(123, 456, 789));
				decoded.ShouldEqual(new Vector3(123, 456, 789));

				decoded = default(Vector3);
				Unpack.Base64(encoded, out decoded);
				decoded.ShouldBe<Vector3>(new Vector3(123,456,789));
				decoded.ShouldEqual(new Vector3(123, 456, 789));
			}

		}
	}

}
