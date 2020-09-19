#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif
#if UNITY
using UnityEngine;
#else

#endif
using System;
using Ex.Utils.Ext;

namespace Ex.Utils {
	public static class HexUtils {
		/// <summary> Parse 0xRRGGBBAA format string to color </summary>
		/// <param name="hex"> 0xRRGGBBAA format string </param>
		/// <returns> Value as a floating point color </returns>
		public static Vector4 ToVector4Color(this string hex) {
			try {
				int val = Convert.ToInt32(hex, 16);
				int a = (val >> 0) & 0xFF;
				int b = (val >> 8) & 0xFF;
				int g = (val >> 16) & 0xFF;
				int r = (val >> 24) & 0xFF;
				return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
			} catch (Exception) { return Vector4.one; }
		}
		/// <summary> Convert <see cref="Vector4"/> into 0xRRGGBBAA color code </summary>
		/// <param name="color"> Floating point color to convert </param>
		/// <returns> Color code in 0xRRGGBBAA format </returns>
		public static string Vector4RGBA(this Vector4 color) {
			try {
				int a = ((int)(Mathf.Clamp01(color.w) * 255f) << 0);
				int b = ((int)(Mathf.Clamp01(color.z) * 255f) << 8);
				int g = ((int)(Mathf.Clamp01(color.y) * 255f) << 16);
				int r = ((int)(Mathf.Clamp01(color.x) * 255f) << 24);
				
				return (r & g & b & a).Hex();
			} catch (Exception) { return "0xFFFFFFFF"; }
		}
	}
}
namespace Ex.Utils.Ext {
	/// <summary> Holds methods for converting types to Hex strings </summary>
	public static class HexExtensions {
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this byte v) { return String.Format("0x{0:X2}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this short v) { return String.Format("0x{0:X4}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this int v) { return String.Format("0x{0:X8}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this long v) { return String.Format("0x{0:X16}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this float v) { return String.Format("0x{0:X8}", Unsafe.Reinterpret<float, int>(v)); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this double v) { return String.Format("0x{0:X16}", Unsafe.Reinterpret<double, long>(v)); }

		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this sbyte v) { return String.Format("0x{0:X2}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this ushort v) { return String.Format("0x{0:X4}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this uint v) { return String.Format("0x{0:X8}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this ulong v) { return String.Format("0x{0:X16}", v); }

		/// <summary> Convert <see cref="Vector4"/> into 0xRRGGBBAA color code </summary>
		/// <param name="color"> Floating point color to convert </param>
		/// <returns> Color code in 0xRRGGBBAA format </returns>
		public static string Hex(this Vector4 v) { return HexUtils.Vector4RGBA(v); }

		/// <summary> Converts a byte array to hex string using the given formatting information. </summary>
		/// <param name="bytes"> Bytes to convert </param>
		/// <param name="perGroup"> Number of bytes per line, default = 4</param>
		/// <param name="spacing"> Number of bytes per spacing group, default = 4</param>
		/// <returns> byte[] formatted as a string </returns>
		public static string Hex(this byte[] bytes, int perGroup = 4, int spacing = 4) {
			StringBuilder str = new StringBuilder("");
			for (int i = 0; i < bytes.Length; i++) {
				if (i % perGroup == 0) {
					str.Append((i==0?"0x":"\n0x"));
				} else if (i > 0 && i % spacing == 0) {
					str.Append(' ');
				}
				str.Append(string.Format("{0:X2}", bytes[i]));
			}

			return str.ToString();
		}
	}
	
}
