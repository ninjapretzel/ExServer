#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ex{
	/// <summary> 
	/// Not your fucking safe-space, you easily offended, troglodyte. 
	/// Primary place for putting methods that need to make use of unsafe blocks of code.
	/// </summary>
	public static class Unsafe {
		/// <summary>Extracts the bytes from a generic value type.</summary>
		/// <typeparam name="T">Generic type. </typeparam>
		/// <param name="obj">Instance of generic type <paramref name="T"/> to convert</param>
		/// <returns>Raw byte array of the given object</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] ToBytes<T>(T obj) where T : struct {
			unsafe {
				int size = SizeOf<T>();
				byte[] bytes = new byte[size];
				var objRef = __makeref(obj);
				byte* objPtr = (byte*)*((IntPtr*)&objRef);

				for (int i = 0; i < size; i++) {
					bytes[i] = objPtr[i];
				}

				return bytes;
			}
		}

		/// <summary>Helper class for generic SizeOf&lt;T&gt; method</summary>
		/// <typeparam name="T"></typeparam>
		public static class ArrayOfTwoElements<T> { public static readonly T[] Value = new T[2]; }

		/// <summary> Generic, runtime sizeof() for value types </summary>
		/// <typeparam name="T">Type to check size of</typeparam>
		/// <returns>Size of the type passed, in bytes. Returns the pointer size for </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>() where T : struct {
			unsafe {
				var ref1 = __makeref(ArrayOfTwoElements<T>.Value[0]);
				var ref2 = __makeref(ArrayOfTwoElements<T>.Value[1]);

				IntPtr ptr1 = *((IntPtr*)&ref1);
				IntPtr ptr2 = *((IntPtr*)&ref2);

				return (int)(((byte*)ptr2) - ((byte*)ptr1));

			}
		}


		/// <summary> Reinterprets an object's data from one type to another.</summary>
		/// <typeparam name="TIn">Input struct type</typeparam>
		/// <typeparam name="TOut">Output struct type</typeparam>
		/// <param name="val">Value to convert</param>
		/// <returns><paramref name="val"/>'s bytes converted into a <paramref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TOut Reinterpret<TIn, TOut>(TIn val)
			where TIn : struct
			where TOut : struct {
			unsafe {
				TOut result = default(TOut);
				int sizeBytes = Unsafe.SizeOf<TIn>();
				if (sizeBytes != Unsafe.SizeOf<TOut>()) { return result; }

				TypedReference resultRef = __makeref(result);
				byte* resultPtr = (byte*)*((IntPtr*)&resultRef);

				TypedReference valRef = __makeref(val);
				byte* valPtr = (byte*)*((IntPtr*)&valRef);

				for (int i = 0; i < sizeBytes; ++i) {
					resultPtr[i] = valPtr[i];
				}

				return result;
			}
		}


	}
}
