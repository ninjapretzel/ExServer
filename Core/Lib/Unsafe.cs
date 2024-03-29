﻿#if UNITY_2017_1_OR_NEWER
#define UNITY
using UnityEngine;
#endif

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ex.Utils;
using BakaTest;
using Ex;
using System.Reflection;
using System.Collections.Generic;

namespace Ex {


	#region Util Structs

	/// <summary> Interop struct for packing a float[] into a struct, to allow proper use of network arrays embedded in structs </summary>
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct InteropFloat64 {
		public const int MAX_LENGTH = 64;
		public fixed float fixedBuffer[MAX_LENGTH];
		public float this[int i] {
			get { 
				if (i < 0 || i >= MAX_LENGTH) { return 0; }
				fixed (float* f = fixedBuffer) { return f[i]; } 
			}
			set {
				if (i < 0 || i >= MAX_LENGTH) { return; }
				fixed (float* f = fixedBuffer) { f[i] = value; } 
			}
		}

		public static implicit operator float[](InteropFloat64 f) {
			float[] floats = new float[MAX_LENGTH];
			for (int i = 0; i < MAX_LENGTH; i++) { floats[i] = f[i]; }
			return floats;
		}
		public static implicit operator InteropFloat64(float[] floats) {
			InteropFloat64 f;
			for (int i = 0; i < MAX_LENGTH && i < floats.Length; i++) { f[i] = floats[i]; }
			return f;
		}

		public JsonValue ToJson() {
			JsonArray result = new JsonArray();
			for (int i = 0; i < MAX_LENGTH; i++) { result.Add(this[i]); }
			return result;
		}
		public static InteropFloat64 FromJson(JsonValue value) {
			InteropFloat64 result = new InteropFloat64();
			if (value is JsonArray arr) {
				for (int i = 0; i < MAX_LENGTH; i++) {
					result[i] = arr.Pull(i, 0f);
				}
			}
			return result;
		}
	}


	/// <summary> Interop struct for packing a float[] into a struct, to allow proper use of network arrays embedded in structs </summary>
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct InteropFloat32 {
		public const int MAX_LENGTH = 32;
		public fixed float fixedBuffer[MAX_LENGTH];
		public float this[int i] {
			get {
				if (i < 0 || i >= MAX_LENGTH) { return 0; }
				fixed (float* f = fixedBuffer) { return f[i]; }
			}
			set {
				if (i < 0 || i >= MAX_LENGTH) { return; }
				fixed (float* f = fixedBuffer) { f[i] = value; }
			}
		}
		
		public static implicit operator float[](InteropFloat32 f) {
			float[] floats = new float[MAX_LENGTH];
			for (int i = 0; i < MAX_LENGTH; i++) { floats[i] = f[i]; }
			return floats;
		}
		public static implicit operator InteropFloat32(float[] floats) {
			InteropFloat32 f;
			for (int i = 0; i < MAX_LENGTH && i < floats.Length; i++) { f[i] = floats[i]; }
			return f;
		}
		public JsonValue ToJson() {
			JsonArray result = new JsonArray();
			for (int i = 0; i < MAX_LENGTH; i++) { result.Add(this[i]); }
			return result;
		}
		public static InteropFloat32 FromJson(JsonValue value) {
			InteropFloat32 result = new InteropFloat32();
			if (value is JsonArray arr) {
				for (int i = 0; i < MAX_LENGTH; i++) {
					result[i] = arr.Pull(i, 0f);
				}
			}
			return result;
		}
	}


	/// <summary> Interop struct for packing a string into a struct, to allow proper use of network strings embedded in structs </summary>
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct InteropString32 {
		public const int MAX_LENGTH = 32;
		/// <summary> Embedded char array </summary>
		public fixed char fixedBuffer[MAX_LENGTH];
		
		/// <summary> Get or set the string value of this struct </summary>
		public string value { 
			get {
				fixed (char* c = fixedBuffer) {
					return new string(c);
				}

			}
			set {
				fixed (char* c = fixedBuffer) {
					int len = value.Length;
					for (int i = 0; i < MAX_LENGTH - 1; i++) {
						if (i < len) {
							c[i] = value[i];
						} else {
							c[i] = '\0';
						}
					}
					// Ensure final char in buffer is always null.
					c[MAX_LENGTH - 1] = '\0';
				}
			}
		}

		public override string ToString() { return value; }
		public override int GetHashCode() { return value.GetHashCode(); }
		public override bool Equals(object obj) {
			if (obj is InteropString32) { return value.Equals(((InteropString32)obj).value); }
			// may be bad...
			if (obj is string) { return ToString().Equals(obj.ToString()); }
			return false;
		}

		public static implicit operator string (InteropString32 s) { return s.value; }
		public static implicit operator InteropString32(string str) { InteropString32 s; s.value = str; return s; }

		public JsonValue ToJson() { return (JsonString) (string) this; }
		public static InteropString32 FromJson(JsonValue value) {
			InteropString32 result = new InteropString32();
			if (value is JsonString str) {
				result.value = str;
			} else if (value is JsonArray arr) {
				for (int i = 0; i < MAX_LENGTH-1; i++) {
					result.fixedBuffer[i] = arr.Pull(i, '\0');
				}
			}
			return result;
		}
	}
	/// <summary> Interop struct for packing a string into a struct, to allow proper use of network strings embedded in structs </summary>
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct InteropString256 {
		public const int MAX_LENGTH = 256;
		/// <summary> Embedded char array </summary>
		public fixed char fixedBuffer[MAX_LENGTH];

		/// <summary> Get or set the string value of this struct </summary>
		public string value {
			get {
				fixed (char* c = fixedBuffer) {
					return new string(c);
				}
			}
			set {
				fixed (char* c = fixedBuffer) {
					int len = value.Length;
					for (int i = 0; i < MAX_LENGTH - 1; i++) {
						if (i < len) {
							c[i] = value[i];
						} else {
							c[i] = '\0';
						}
					}
					// Ensure final char in buffer is always null.
					c[MAX_LENGTH - 1] = '\0';
				}
			}
		}

		public override string ToString() { return value; }
		public override int GetHashCode() { return value.GetHashCode(); }
		public override bool Equals(object obj) {
			if (obj is InteropString256) { return value.Equals(((InteropString256)obj).value); }
			// may be bad...
			if (obj is string) { return ToString().Equals(obj.ToString()); }
			return false;
		}

		public static implicit operator string(InteropString256 s) { return s.value; }
		public static implicit operator InteropString256(string str) { InteropString256 s; s.value = str; return s; }

		public JsonValue ToJson() { return (JsonString)(string)this; }
		public static InteropString32 FromJson(JsonValue value) {
			InteropString32 result = new InteropString32();
			if (value is JsonString str) {
				result.value = str;
			} else if (value is JsonArray arr) {
				for (int i = 0; i < MAX_LENGTH - 1; i++) {
					result.fixedBuffer[i] = arr.Pull(i, '\0');
				}
			}
			return result;
		}
	}

	#endregion
	
	/// <summary> 
	/// Not your safe-space. 
	/// Primary place for putting methods that need to make use of unsafe blocks of code.
	/// Modified code from http://benbowen.blog/post/fun_with_makeref/
	/// </summary>
	public static class Unsafe {
		/// <summary> Are we running on the Mono Runtime? </summary>
		public static readonly bool MonoRuntime = Type.GetType("Mono.Runtime") != null;

		public static class Info<T> where T : unmanaged {
			public static readonly int size = SizeOf<T>();
		}

		private struct Two<T> where T : unmanaged { public T first, second; public static readonly Two<T> instance = default(Two<T>); }
		/// <summary> Generic, runtime sizeof() for value types with the added restriction of unmanaged interopability. </summary>
		/// <typeparam name="T"> Type to check size of </typeparam>
		/// <returns> Size of the type passed, in bytes. Returns the pointer size for </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static int SizeOf<T>() where T : unmanaged {
			Two<T> two = Two<T>.instance;
			void* a = &two.first;
			void* b = &two.second;
			return (int)b - (int)a;
		}
		/// <summary>Extracts the bytes from a generic value type.</summary>
		/// <typeparam name="T">Generic type. </typeparam>
		/// <param name="obj">Instance of generic type <paramref name="T"/> to convert</param>
		/// <returns>Raw byte array of the given object</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe byte[] ToBytes<T>(T t) where T : unmanaged {
			byte[] bytes = new byte[Info<T>.size];
			byte* pt = (byte*)&t;
			for (int i = 0; i < bytes.Length; ++i) { bytes[i] = pt[i]; }
			return bytes;
		}
		/// <summary> Extracts bytes from a struct value into an existing byte[] array, starting at a position </summary>
		/// <typeparam name="T"> Generic type of value parameter </typeparam>
		/// <param name="value"> Value to extract data from </param>
		/// <param name="bytes"> byte[] to place data into </param>
		/// <param name="start"> starting index </param>
		/// <returns> Modified byte[] </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe byte[] ToBytes<T>(T t, byte[] bytes, int start) where T : unmanaged {
			byte* pt = (byte*)&t;
			for (int i = 0; i + start < bytes.Length && i < Info<T>.size; ++i) {
				bytes[i + start] = pt[i];
			}
			return bytes;
		}

		/// <summary> Extracts an arbitrary struct from a byte array, at a given position </summary>
		/// <typeparam name="T"> Generic type of struct to extract </typeparam>
		/// <param name="source"> Source byte[] </param>
		/// <param name="start"> Index struct exists at </param>
		/// <returns> Struct built from byte array, starting at index </returns>
		public static unsafe T FromBytes<T>(byte[] source, int start) where T : unmanaged {
			int sizeOfT = Info<T>.size;
			if (start < 0) {
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): start index must be 0 or greater, was {start}");
			}
			if (sizeOfT + start > source.Length) {
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): Source is {source.Length} bytes, start at {start}, and target is {sizeOfT} bytes in size, out of range.");
			}
			T result = default(T);
			byte* pt = (byte*)&result;
			for (int i = 0; i < sizeOfT; i++) { pt[i] = source[i + start]; }

			return result;
		}

		/// <summary>Converts a byte[] back into a struct.</summary>
		/// <typeparam name="T">Generic type</typeparam>
		/// <param name="source">Data source</param>
		/// <returns>Object of type T assembled from bytes in source</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe T FromBytes<T>(byte[] source) where T : unmanaged {
			int sizeOfT = Info<T>.size;
			if (sizeOfT != source.Length) {
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): Source is {source.Length} bytes, but expected type is {sizeOfT} bytes in size.");
			}
			T result = default(T);
			byte* pt = (byte*)&result;
			for (int i = 0; i < sizeOfT; i++) { pt[i] = source[i]; }

			return result;
		}

		/// <summary>Converts a byte[] back into a struct.</summary>
		/// <typeparam name="T">Generic type</typeparam>
		/// <param name="source">Data source</param>
		/// <returns>Object of type T assembled from bytes in source</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void FromBytes<T>(byte[] source, out T ret) where T : unmanaged {
			int sizeOfT = Info<T>.size;
			if (sizeOfT != source.Length) {
				throw new Exception($"Unsafe.FromBytes<{typeof(T)}>(): Source is {source.Length} bytes, but expected type is {sizeOfT} bytes in size.");
			}
			T result = default(T);
			byte* pt = (byte*)&result;
			for (int i = 0; i < sizeOfT; i++) { pt[i] = source[i]; }
			ret = result;
		}
		/// <summary> Reinterprets an object's data from one type to another.</summary>
		/// <typeparam name="TIn">Input struct type</typeparam>
		/// <typeparam name="TOut">Output struct type</typeparam>
		/// <param name="val">Value to convert</param>
		/// <returns><paramref name="val"/>'s bytes converted into a <paramref name="TOut"/></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe TOut Reinterpret<TIn, TOut>(TIn val)
			where TIn : unmanaged
			where TOut : unmanaged {

			int sizeIn = Info<TIn>.size;
			int sizeOut = Info<TOut>.size;
			if (sizeIn != sizeOut) {
				throw new Exception($"Unsafe.Reinterpret<{typeof(TIn)}, {typeof(TOut)}>(): Size of these types are different- {sizeIn} vs {sizeOut}.");
			}

			int size = sizeIn;
			TOut result = default(TOut);
			byte* pt = (byte*)&result;
			byte* pv = (byte*)&val;
			for (int i = 0; i < size; i++) { pt[i] = pv[i]; }

			return result;
		}
		
		/// <summary> Inspect the raw memory around a pointer </summary>
		/// <param name="p"> Pointer to inspect </param>
		/// <param name="length"> Total number of bytes to inspect </param>
		/// <param name="stride"> Number of bytes to put on a single line </param>
		/// <returns> String holding hexdump of the memory at the given location </returns>
		public static unsafe string InspectMemory(IntPtr p, int length = 16, int stride = 8) {
			return InspectMemory((void*)p, length, stride);
		}


		/// <summary> Inspect the raw memory around a pointer </summary>
		/// <param name="p"> Pointer to inspect </param>
		/// <param name="length"> Total number of bytes to inspect </param>
		/// <param name="stride"> Number of bytes to put on a single line </param>
		/// <returns> String holding hexdump of the memory at the given location </returns>
		public static unsafe string InspectMemory(void* p, int length = 16, int stride = 8) {
			StringBuilder str = "";
			byte* bp = (byte*) p;
			for (int i = 0; i < length; i++) {
				if (i%stride == 0) {
					str += (i==0?"0x":"\n0x");
				}
				str += String.Format("{0:X2}", bp[i]);
			}
			return str.ToString();
		}

		/// <summary> Inspect the raw memory around a pointer </summary>
		/// <param name="p"> Pointer to inspect </param>
		/// <param name="length"> Total number of bytes to inspect </param>
		/// <param name="stride"> Number of bytes to put on a single line </param>
		/// <returns> String holding hexdump of the memory at the given location </returns>
		public static unsafe string InspectMemoryReverse(void* p, int length = 16, int stride = 8) {
			StringBuilder str = "";
			byte* bp = (byte*)p;
			int lines = length / stride;

			for (int i = 0; i < lines; i++) {
				str += (i == 0 ? "0x" : "\n0x");
				for (int k = 0; k < stride; k++) {
					str += String.Format("{0:X2}", bp[i * stride + stride - k - 1]);

				}
				
			}
			return str.ToString();
		}

		
	}

	public class Unsafe_Tests {
		
		[Serializable]
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
			//Unsafe.Info<int>.size.ShouldBe(4);

			Unsafe.SizeOf<byte>().ShouldBe(1);  // DUURRRR
			Unsafe.Info<byte>.size.ShouldBe(1);
			Unsafe.SizeOf<sbyte>().ShouldBe(1);  
			Unsafe.Info<sbyte>.size.ShouldBe(1); 
			Unsafe.SizeOf<bool>().ShouldBe(1);  
			Unsafe.Info<bool>.size.ShouldBe(1); 

			Unsafe.SizeOf<short>().ShouldBe(2);  
			Unsafe.Info<short>.size.ShouldBe(2);
			Unsafe.SizeOf<ushort>().ShouldBe(2);
			Unsafe.Info<ushort>.size.ShouldBe(2);
			Unsafe.SizeOf<char>().ShouldBe(2);  // Yes, seriously. char is a short (Unicode-16 endpoint)
			Unsafe.Info<char>.size.ShouldBe(2);

			Unsafe.SizeOf<int>().ShouldBe(4); 
			Unsafe.Info<int>.size.ShouldBe(4);
			Unsafe.SizeOf<uint>().ShouldBe(4); 
			Unsafe.Info<uint>.size.ShouldBe(4);
			Unsafe.SizeOf<float>().ShouldBe(4); 
			Unsafe.Info<float>.size.ShouldBe(4);

			Unsafe.SizeOf<long>().ShouldBe(8);
			Unsafe.Info<long>.size.ShouldBe(8);
			Unsafe.SizeOf<ulong>().ShouldBe(8);
			Unsafe.Info<ulong>.size.ShouldBe(8);
			Unsafe.SizeOf<double>().ShouldBe(8);
			Unsafe.Info<double>.size.ShouldBe(8);

			Unsafe.SizeOf<Vector3>().ShouldBe(12);
			Unsafe.Info<Vector3>.size.ShouldBe(12);
			Unsafe.SizeOf<Vector3Int>().ShouldBe(12);
			Unsafe.Info<Vector3Int>.size.ShouldBe(12);
			Unsafe.SizeOf<TestBlah>().ShouldBe(12);
			Unsafe.Info<TestBlah>.size.ShouldBe(12);
			Unsafe.SizeOf<FourBytes>().ShouldBe(4);
			Unsafe.Info<FourBytes>.size.ShouldBe(4);
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

		public static void TestToFromBytesSubarray() {
			{
				byte[] bytes = new byte[sizeof(float) * 4];
				Vector3 v3a = new Vector3(8.0f,12.0f,16.0f);
				Unsafe.ToBytes(4.0f, bytes, 0);
				Unsafe.ToBytes(v3a, bytes, 1 * sizeof(float) );

				float a = Unsafe.FromBytes<float>(bytes, 0 * sizeof(float));
				float b = Unsafe.FromBytes<float>(bytes, 1 * sizeof(float));
				float c = Unsafe.FromBytes<float>(bytes, 2 * sizeof(float));
				float d = Unsafe.FromBytes<float>(bytes, 3 * sizeof(float));
				
				a.ShouldBe(4.0f);
				b.ShouldBe(8.0f);
				c.ShouldBe(12.0f);
				d.ShouldBe(16.0f);
				
				Vector3 v3b = Unsafe.FromBytes<Vector3>(bytes, 1 * sizeof(float));
				v3b.ShouldEqual(v3a);

				Vector3 v3a2 = new Vector3(4.0f, 8.0f, 12.0f);
				Vector3 v3b2 = Unsafe.FromBytes<Vector3>(bytes, 0 * sizeof(float));
				v3b2.ShouldEqual(v3a2);

				Vector4 v4a = new Vector4(4.0f, 8.0f, 12.0f, 16.0f);
				Vector4 v4b = Unsafe.FromBytes<Vector4>(bytes, 0); 
				v4a.ShouldEqual(v4b);
			}
			{
				InteropFloat32 if32;
				for (int i = 0; i < InteropFloat32.MAX_LENGTH; i++) { if32[i] = i * 10.0f; }
				byte[] bytes = Unsafe.ToBytes(if32);
				bytes.Length.ShouldBe(InteropFloat32.MAX_LENGTH * sizeof(float));

				float tenInwards = Unsafe.FromBytes<float>(bytes, 10 * sizeof(float));
				tenInwards.ShouldBe(10 * 10.0f);

			}
		}

		public static void TestFromBytesShouldThrowForWrongSize() {
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

		public static void TestInteropFloatArrays() {
			{ 
				Unsafe.Info<InteropFloat32>.size.ShouldBe(32 * sizeof(float));
				Unsafe.Info<InteropFloat64>.size.ShouldBe(64 * sizeof(float));

				InteropFloat32 floats;
				for (int i = 0; i < 32; i++) { floats[i] = i * 10f; }
				for (int i = 0; i < 32; i++) { floats[i].ShouldBe(i * 10f); }
			}

			{
				float[] floats = new float[32];
				for (int i = 0; i < floats.Length; i++) { floats[i] = i * 10.0f; }

				InteropFloat32 f = floats;

				f[10].ShouldBe(10 * 10.0f);
			}

		}

		struct StringAndStuff {
			public int x;
			public InteropString32 str;
			public float y;
			public StringAndStuff(int x, string str, float y) { this.x = x; this.y = y; this.str = str; }
			public override bool Equals(object obj) {
				if (obj is StringAndStuff) {
					StringAndStuff s = (StringAndStuff) obj;
					bool nullStr = str == null;
					bool snullStr = s.str == null;
					return s.x == x && s.y == y && (nullStr ? (snullStr) : (str.Equals(s.str)));
				}
				return false;
			}
			public override int GetHashCode() {
				return (x.GetHashCode() << 3) ^ (str.GetHashCode()) ^ (y.GetHashCode() >> 3);
			}
		}

		public static void TestInteropStrings() {
			{
				Unsafe.Info<InteropString32>.size.ShouldBe(32 * sizeof(char));
				Unsafe.Info<InteropString256>.size.ShouldBe(256 * sizeof(char));

				InteropString32 str = "a short string";
				string converted = str;
				str.ShouldEqual("a short string");
			}

			{
				string source = "aStringThatIsExactly31CharsLong";
				source.Length.ShouldBe(31);
				InteropString32 str = source;
				string converted = str;
				converted.ShouldEqual(source);
			}

			{
				string source = "a string that is much longer than thirty one characters long, but still less than two hundred and fifty six characters long.";
				(source.Length > 31).ShouldBeTrue();
				(source.Length < 256).ShouldBeTrue();

				InteropString32 str = source;
				string converted = str;
				converted.ShouldNotEqual(source);

				InteropString256 str256 = source;
				string converted256 = str256;
				converted256.ShouldEqual(source);
			}



		}

		public static void TestDeserializeInteropString() {
			{
				StringAndStuff s = new StringAndStuff(123, "omg wtf lol bbq", 123.456f);
				Unsafe.Info<StringAndStuff>.size.ShouldBe(sizeof(int) + Unsafe.Info<InteropString32>.size + sizeof(float) );
				s.x = 123;
				s.y = 123.456f;
				s.str = "omg wtf lol bbq";
				byte[] bytes = Unsafe.ToBytes(s);

				Unsafe.FromBytes<StringAndStuff>(bytes).ShouldEqual(new StringAndStuff(123, "omg wtf lol bbq", 123.456f));

			}
		}

		public static void TestGUIDPacking() {
			byte[] bytes = new byte[] {
				0x3f, 0xd5, 0xf2, 0xfe,							// 0,1,2,3,
				0x51, 0x99,										// 4,5,
				0x4f, 0x03,										// 6,7,
				0x96, 0xc1, 0x4a, 0xea, 0xca, 0xdf, 0xe4, 0xe3	// 8,9,10,11,12,13,14,15
			};
			Guid guid = new Guid(bytes);

			byte[] returned = Unsafe.ToBytes(guid);
			for (int i = 0; i < returned.Length; i++) {
				bytes.ShouldBeSame(returned);
			}
		}

	}

}

