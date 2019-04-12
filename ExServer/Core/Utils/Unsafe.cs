#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BakaTest;
using Ex.Utils;

namespace Ex{

	/// <summary> Static generic template-like class to cache information about structs </summary>
	/// <typeparam name="T"></typeparam>
	public static class StructInfo<T> where T : struct {
		public static int size = Unsafe.SizeOf<T>();
	}

	/// <summary> 
	/// Not your safe-space. 
	/// Primary place for putting methods that need to make use of unsafe blocks of code.
	/// Modified code from http://benbowen.blog/post/fun_with_makeref/
	/// </summary>
	public static class Unsafe {
		/// <summary>Extracts the bytes from a generic value type.</summary>
		/// <typeparam name="T">Generic type. </typeparam>
		/// <param name="obj">Instance of generic type <paramref name="T"/> to convert</param>
		/// <returns>Raw byte array of the given object</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe byte[] ToBytes<T>(T value) where T : struct {
			byte[] bytes = new byte[StructInfo<T>.size];
			TypedReference valueRef = __makeref(value);
			// Unsafe Abuse
			// First of all we're getting a pointer to valueref (so that's a reference to our reference), 
			// and treating it as a pointer to an IntPtr instead of a pointer to a TypedReference. 
			// This works because the first 4/8 bytes in the TypedReference struct are an IntPtr 
			// specifically the pointer to value. Then we dereference that IntPtr pointer to a regular old IntPtr, 
			// and finally cast that IntPtr to a byte* so we can use it in the copy code below.
			byte* valuePtr = (byte*)*((IntPtr*)&valueRef);

			for (int i = 0; i < bytes.Length; ++i) {
				bytes[i] = valuePtr[i];
			}
			return bytes;
		}

		/// <summary>Converts a byte[] back into a struct.</summary>
		/// <typeparam name="T">Generic type</typeparam>
		/// <param name="source">Data source</param>
		/// <returns>Object of type T assembled from bytes in source</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T FromBytes<T>(byte[] source) where T : struct {
			int sizeOfT = StructInfo<T>.size;

			T result = default(T);
			TypedReference resultRef = __makeref(result);
			// has exactly the same idea behind it as the similar line in the above method- 
			// we're getting the pointer to result.
			byte* resultPtr = (byte*)*((IntPtr*)&resultRef);

			for (int i = 0; i < sizeOfT; ++i) {
				resultPtr[i] = source[i];
			}

			return result;
		}


		/// <summary>Helper class for generic SizeOf&lt;T&gt; method</summary>
		/// <typeparam name="T"></typeparam>
		private static class ArrayOfTwoElements<T> { 
			public static readonly T[] Value = new T[2];
		}
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct Two<T> {
			public T first, second;
			public static readonly Two<T> instance = default(Two<T>);
		}

		/// <summary> Generic, runtime sizeof() for value types </summary>
		/// <typeparam name="T">Type to check size of</typeparam>
		/// <returns>Size of the type passed, in bytes. Returns the pointer size for </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>() where T : struct {
			Type type = typeof(T);

			TypeCode typeCode = Type.GetTypeCode(type);
			switch (typeCode) {
				case TypeCode.Boolean:
					return sizeof(bool);
				case TypeCode.Char:
					return sizeof(char);
				case TypeCode.SByte:
					return sizeof(sbyte);
				case TypeCode.Byte:
					return sizeof(byte);
				case TypeCode.Int16:
					return sizeof(short);
				case TypeCode.UInt16:
					return sizeof(ushort);
				case TypeCode.Int32:
					return sizeof(int);
				case TypeCode.UInt32:
					return sizeof(uint);
				case TypeCode.Int64:
					return sizeof(long);
				case TypeCode.UInt64:
					return sizeof(ulong);
				case TypeCode.Single:
					return sizeof(float);
				case TypeCode.Double:
					return sizeof(double);
				case TypeCode.Decimal:
					return sizeof(decimal);
				default: unsafe {
					
#if !USE_ARRAY
					Two<T> two = Two<T>.instance;
					TypedReference ref0 = __makeref(two.first);
					TypedReference ref1 = __makeref(two.second);
					
					IntPtr p0 = *((IntPtr*)&ref0);
					IntPtr p1 = *((IntPtr*)&ref1);
					
					return (int)(((byte*)p1) - ((byte*)p0));
#else
					T[] array = ArrayOfTwoElements<T>.Value;
					GCHandle pin = GCHandle.Alloc(array, GCHandleType.Pinned);
					try {
						var ref1 = __makeref(array[0]);
						var ref2 = __makeref(array[1]);

						IntPtr ptr1 = *((IntPtr*)&ref1);
						IntPtr ptr2 = *((IntPtr*)&ref2);

						return (int)(((byte*)ptr2) - ((byte*)ptr1));
					} finally { pin.Free(); }
#endif
				}
			}
		}


		/// <summary> Reinterprets an object's data from one type to another.</summary>
		/// <typeparam name="TIn">Input struct type</typeparam>
		/// <typeparam name="TOut">Output struct type</typeparam>
		/// <param name="val">Value to convert</param>
		/// <returns><paramref name="val"/>'s bytes converted into a <paramref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe TOut Reinterpret<TIn, TOut>(TIn val)
			where TIn : struct
			where TOut : struct {
			
			TOut result = default(TOut);
			int sizeBytes = StructInfo<TIn>.size;
			if (sizeBytes != StructInfo<TOut>.size) { return result; }

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
	
	public class Unsafe_Tests {

		public struct TestBlah { public float a,b,c; }

		public static void TestReinterpret() {
			TestBlah zero1 = new TestBlah() { a = 0, b = 0, c = 0 };
			Vector3 zero2 = Unsafe.Reinterpret<TestBlah, Vector3>(zero1);

			zero2.Equals(Vector3.zero).ShouldBeTrue();
			
		}

	}

}
