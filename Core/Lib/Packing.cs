#if UNITY_2017_1_OR_NEWER
#define UNITY
using UnityEngine;
#else

#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using BakaTest;
using System.Reflection;

namespace Ex.Utils {

	public static class WarmPacking {
		//#if UNITY
		public static string Warmup() {
			return "" 
				+ Warm<byte>()+ Warm<sbyte>()+ Warm<short>()+ Warm<ushort>()
				+ Warm<int>()+ Warm<uint>()+ Warm<long>()+ Warm<ulong>()
				+ Warm<decimal>()+ Warm<float>()+ Warm<double>()
				+ Warm<bool>() + Warm<char>()
				+ Warm<Vector2>()+ Warm<Vector3>()+ Warm<Vector4>()
				+ Warm<DateTime>()
				+ Warm<InteropFloat32>()+ Warm<InteropFloat64>()
				+ Warm<InteropString32>()+ Warm<InteropString256>()
			;
		}
		public static string Warm<T>() where T : unmanaged {
			return ""+Unpack.Base64<T>(Pack.Base64(default(T)));
		}
		//#endif
	}

	/// <summary> Class holding code for packing structs </summary>
	public static class Pack {

		/// <summary> Pack a struct into a Base64 string </summary>
		/// <typeparam name="T"> Generic type of parameter </typeparam>
		/// <param name="value"> Parameter to pack </param>
		/// <returns> Base64 from converting struct into byte[], then encoding byte array </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Base64<T>(T value) where T : unmanaged {
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Base64Out<T>(string encoded, out T ret) where T : unmanaged {
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Base64<T>(string encoded) where T : unmanaged {
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
				Unpack.Base64Out(encoded, out decoded);
				decoded.ShouldBe<Vector3>(new Vector3(123,456,789));
				decoded.ShouldEqual(new Vector3(123, 456, 789));
			}

		}
	}

}
