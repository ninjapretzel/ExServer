#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
using UnityEngine;
#else

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
			if (sizeOfT != source.Length) { 
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): Source is {source.Length} bytes, but expected type is {sizeOfT} bytes in size."); 
			}

			T result = default(T);
			TypedReference resultRef = __makeref(result);
			// has exactly the same idea behind it as the similar line in the ToBytes method- 
			// we're getting the pointer to result.
			byte* resultPtr = (byte*)*((IntPtr*)&resultRef);

			for (int i = 0; i < sizeOfT; ++i) {
				resultPtr[i] = source[i];
			}

			return result;
		}

		/// <summary>Converts a byte[] back into a struct.</summary>
		/// <typeparam name="T">Generic type</typeparam>
		/// <param name="source">Data source</param>
		/// <returns>Object of type T assembled from bytes in source</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void FromBytes<T>(byte[] source, out T ret) where T : struct {
			int sizeOfT = StructInfo<T>.size;
			if (sizeOfT != source.Length) {
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): Source is {source.Length} bytes, but expected type is {sizeOfT} bytes in size.");
			}

			T result = default(T);
			TypedReference resultRef = __makeref(result);
			// has exactly the same idea behind it as the similar line in the ToBytes method- 
			// we're getting the pointer to result.
			byte* resultPtr = (byte*)*((IntPtr*)&resultRef);

			for (int i = 0; i < sizeOfT; ++i) {
				resultPtr[i] = source[i];
			}
			ret = result;
		}

		/// <summary>Helper class for generic SizeOf&lt;T&gt; method</summary>
		/// <typeparam name="T"></typeparam>
		private static class ArrayOfTwoElements<T> { 
			public static readonly T[] Value = new T[2];
		}
		/// <summary>Helper class for generic SizeOf&lt;T&gt; method</summary>
		/// <typeparam name="T"></typeparam>
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct Two<T> {
			public T first, second;
			public static readonly Two<T> instance = default(Two<T>);
		}

		/// <summary> Generic, runtime sizeof() for value types </summary>
		/// <typeparam name="T">Type to check size of</typeparam>
		/// <returns>Size of the type passed, in bytes. Returns the pointer size for </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SizeOf<T>() where T : struct {
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
					// static refs to structs should not need to be pinned...
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
		public struct FourBytes { public byte a,b,c,d; }

		public static void TestReinterpret() {
			{
				TestBlah zero1 = new TestBlah() { a = 0, b = 0, c = 0 };
				Vector3 zero2 = Unsafe.Reinterpret<TestBlah, Vector3>(zero1);
				zero2.ShouldBe<Vector3>(Vector3.zero);
				zero2.ShouldEqual(Vector3.zero);

				TestBlah yeet = new TestBlah() { a = 123, b = 456, c = 789 };
				Vector3 yeah = Unsafe.Reinterpret<TestBlah, Vector3>(yeet);
				yeah.ShouldBe<Vector3>(new Vector3(123,456,789));
				yeah.ShouldEqual(new Vector3(123,456,789));
			}

			{
				int IEEEonePointOh = 0x3F80_0000;
				float onePointOhEff = 1.0f;
				int IEEEthreePointFive = 0x4060_0000;
				float threePointFiveEff = 3.5f;

				Unsafe.Reinterpret<int, float>(IEEEonePointOh).ShouldBe(1.0f);
				Unsafe.Reinterpret<int, float>(IEEEthreePointFive).ShouldBe(3.5f);
				Unsafe.Reinterpret<float, int>(onePointOhEff).ShouldBe(0x3F80_0000);
				Unsafe.Reinterpret<float, int>(threePointFiveEff).ShouldBe(0x4060_0000);
			}

			{
				int it = (unchecked( (int) 0xDEADBEEF ));
				FourBytes f = Unsafe.Reinterpret<int, FourBytes>(it);
				if (!BitConverter.IsLittleEndian) {
					f.a.ShouldBe(0xDE); f.b.ShouldBe(0xAD); f.c.ShouldBe(0xBE); f.d.ShouldBe(0xEF);
				} else {
					f.d.ShouldBe(0xDE); f.c.ShouldBe(0xAD); f.b.ShouldBe(0xBE); f.a.ShouldBe(0xEF);
				}
			}
		}
		
		public static void TestSizeOf() {
			// Most of these should not change per platform, unless .net fundamentally changes...
			// Haha, cool, they also line up, I didn't even know they would!
			//Unsafe.SizeOf<int>().ShouldBe(4); 
			//StructInfo<int>.size.ShouldBe(4);

			Unsafe.SizeOf<byte>().ShouldBe(1);  // DUURRRR
			StructInfo<byte>.size.ShouldBe(1);
			Unsafe.SizeOf<sbyte>().ShouldBe(1);  
			StructInfo<sbyte>.size.ShouldBe(1); 
			Unsafe.SizeOf<bool>().ShouldBe(1);  
			StructInfo<bool>.size.ShouldBe(1); 

			Unsafe.SizeOf<short>().ShouldBe(2);  
			StructInfo<short>.size.ShouldBe(2);
			Unsafe.SizeOf<ushort>().ShouldBe(2);
			StructInfo<ushort>.size.ShouldBe(2);
			Unsafe.SizeOf<char>().ShouldBe(2);  // Yes, seriously. char is a short (Unicode-16 endpoint)
			StructInfo<char>.size.ShouldBe(2);

			Unsafe.SizeOf<int>().ShouldBe(4); 
			StructInfo<int>.size.ShouldBe(4);
			Unsafe.SizeOf<uint>().ShouldBe(4); 
			StructInfo<uint>.size.ShouldBe(4);
			Unsafe.SizeOf<float>().ShouldBe(4); 
			StructInfo<float>.size.ShouldBe(4);

			Unsafe.SizeOf<long>().ShouldBe(8);
			StructInfo<long>.size.ShouldBe(8);
			Unsafe.SizeOf<ulong>().ShouldBe(8);
			StructInfo<ulong>.size.ShouldBe(8);
			Unsafe.SizeOf<double>().ShouldBe(8);
			StructInfo<double>.size.ShouldBe(8);

			Unsafe.SizeOf<Vector3>().ShouldBe(12);
			StructInfo<Vector3>.size.ShouldBe(12);
			Unsafe.SizeOf<Vector3Int>().ShouldBe(12);
			StructInfo<Vector3Int>.size.ShouldBe(12);
			Unsafe.SizeOf<TestBlah>().ShouldBe(12);
			StructInfo<TestBlah>.size.ShouldBe(12);
			Unsafe.SizeOf<FourBytes>().ShouldBe(4);
			StructInfo<FourBytes>.size.ShouldBe(4);
		}

		public static void Reverse(byte[] bytes) {
			Reverse(bytes, 0, bytes.Length);
		}
		public static void Reverse(byte[] bytes, int start, int end) {
			int len = end-start;
			for (int i = 0; i < len / 2; i++) {
				byte temp = bytes[i+start];
				bytes[i+start] = bytes[end - 1 - i];
				bytes[end - 1 - i] = temp;
			}
		}
		
		public static void TestToFromBytes() {
			{
				int i = (unchecked((int)0xDEADBEEF));

				byte[] bytes = Unsafe.ToBytes(i);
				byte[] expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
				bytes.Length.ShouldBe(4);
				if (BitConverter.IsLittleEndian) { Reverse(expected); } 
				bytes.ShouldBeSame(expected);

				Unsafe.FromBytes<int>(bytes).ShouldBe((unchecked((int)0xDEADBEEF)));
				Unsafe.FromBytes<float>(bytes).ShouldBe(-6.2598534E18f);
			}
			{
				Vector3 v = new Vector3(123,456,789);
				// 0x42F60000, 0x43E40000, 0x44454000
				byte[] bytes = Unsafe.ToBytes(v);
				byte[] expected = new byte[] {
					0x42, 0xF6, 0x00, 0x00,
					0x43, 0xE4, 0x00, 0x00,
					0x44, 0x45, 0x40, 0x00
				};
				bytes.Length.ShouldBe(12);
				if (BitConverter.IsLittleEndian) {
					Reverse(expected, 0, 4);
					Reverse(expected, 4, 8);
					Reverse(expected, 8, 12);
				}
				bytes.ShouldBeSame(expected);
				
				var a = new Vector3(123, 456, 789);
				var b = new Vector3(123, 456, 789);
				Unsafe.FromBytes<Vector3>(bytes).ShouldEqual(new Vector3(123, 456, 789));
				Unsafe.FromBytes<Vector3>(bytes).ShouldBe<Vector3>(new Vector3(123, 456, 789));
				Unsafe.FromBytes<TestBlah>(bytes).ShouldEqual(new TestBlah() { a=123, b=456, c=789 });

				Vector3 vout;
				Unsafe.FromBytes(bytes, out vout);
				vout.ShouldEqual(new Vector3(123, 456, 789));
				vout.ShouldBe<Vector3>(new Vector3(123, 456, 789));
				
				TestBlah tbout;
				Unsafe.FromBytes(bytes, out tbout);
				tbout.ShouldEqual(new TestBlah() { a=123, b=456, c=789 });
			}
		}

		public static void TestFromBytesShouldThrow() {
			{
				byte[] bytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
				Exception e = null;
				try {
					float f;
					Unsafe.FromBytes(bytes, out f);
				} catch (Exception ee) { e = ee; }

				e.ShouldNotBe(null);
			}
		}

	}

}
