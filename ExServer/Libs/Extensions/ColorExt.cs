using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDColor = System.Drawing.Color;

namespace Core.Libs {
	public static class ColorExt {

		/// <summary> Colors with names. </summary>
		public static readonly Dictionary<string, SDColor> namedColors = new Dictionary<string, SDColor>() {
			{ "blue", SDColor.FromArgb(103,103,177)},
			{ "ltblue", SDColor.FromArgb(103,140,177)},
			{ "cyan", SDColor.FromArgb(103,177,177)},

			{ "purp", SDColor.FromArgb(140,103,177)},
			{ "green", SDColor.FromArgb(103,177,103)},

			{ "blugray", SDColor.FromArgb(102, 116, 123)},
			{ "gray", SDColor.FromArgb(122, 122, 122)},

			{ "ltgreen", SDColor.FromArgb(147,199,99)},
			{ "ltpurp", SDColor.FromArgb(162,169,240)},
			{ "yellow", SDColor.FromArgb(255,205,34)},
			{ "ltyellow", SDColor.FromArgb(232,226,183)},
			{ "orange", SDColor.FromArgb(236,118,0)},
			{ "ltorange", SDColor.FromArgb(255,132,9)},

			// Client logging 
			{ "verbose_connection_info", SDColor.DarkBlue },
			{ "connection_info", SDColor.DarkCyan },
			// Generic module message
			{ "modules", SDColor.FromArgb(103,140,177)},

			{ "notice", SDColor.Crimson },
			{ "error", SDColor.Red },
			{ "info", SDColor.DarkBlue },
			{ "warning", SDColor.Orange },
			{ "connect", SDColor.DarkCyan },
			{ "greentext", SDColor.ForestGreen},
			{ "command", SDColor.FromArgb(147, 199, 99)},
			{ "pepetext", SDColor.FromArgb(20, 136, 0)},
			{ "srvmsg", SDColor.DarkKhaki },
		};

		/// <summary> Internal float color struct for easier manipulation</summary>
		private struct Color {
			public float r, g, b, a;

			public Color(float r, float g, float b) {
				this.r = r;
				this.g = g;
				this.b = b;
				a = 1;
			}
			public Color(float a, float r, float g, float b) {
				this.r = r;
				this.g = g;
				this.b = b;
				this.a = a;
			}

			public static implicit operator SDColor(Color c) {
				int ir = ((int)(c.r * 255));
				int ig = ((int)(c.g * 255));
				int ib = ((int)(c.b * 255));
				int ia = ((int)(c.a * 255));

				return SDColor.FromArgb(ia, ir, ig, ib);
			}

			public static implicit operator Color(SDColor c) {
				float r = c.R / 255.0f;
				float g = c.G / 255.0f;
				float b = c.B / 255.0f;
				float a = c.A / 255.0f;
				return new Color(a, r, g, b);
			}

			public static Color operator +(Color a, Color b) { return new Color(a.a + b.a, a.r + b.r, a.g + b.g, a.b + b.b); }
			public static Color operator -(Color a, Color b) { return new Color(a.a - b.a, a.r - b.r, a.g - b.g, a.b - b.b); }
			
			public static Color operator *(Color a, Color b) { return new Color(a.a * b.a, a.r * b.r, a.g * b.g, a.b * b.b); }
			public static Color operator /(Color a, Color b) { return new Color(a.a / b.a, a.r / b.r, a.g / b.g, a.b / b.b); }
			public static Color operator *(Color a, float b) { return new Color(a.a * b, a.r * b, a.g * b, a.b * b); }
			public static Color operator /(Color a, float b) { return new Color(a.a / b, a.r / b, a.g / b, a.b / b); }

			public static Color Lerp(Color a, Color b, float f) { return a + (b - a) * f; }
		}

		/// <summary> Create a hex string from a Color32, in the form #RRGGBBAA </summary>
		public static string HexString(this SDColor c) {
			string str = "";
			str += c.R.ToHex();
			str += c.G.ToHex();
			str += c.B.ToHex();
			if (c.A < 255) { str += c.A.ToHex(); }
			return str;
		}
		/// <summary> Create a hex string from a Color32, in the form #AARRGGBB </summary>
		public static string HexStringARGB(this SDColor c) {
			string str = "";
			if (c.A < 255) { str += c.A.ToHex(); }
			str += c.R.ToHex();
			str += c.G.ToHex();
			str += c.B.ToHex();
			return str;
		}

		public static SDColor ParseHex(this string s) {
			Color c = new Color();
			try {
				int pos = s.StartsWith("#") ? 1 : 0;
				string r = s.Substring(pos + 0, 2);
				string g = s.Substring(pos + 2, 2);
				string b = s.Substring(pos + 4, 2);
				string a = (s.Length > (pos+6)) ? s.Substring(pos + 6, 2) : "FF";
				c.r = r.ParseByte();
				c.g = g.ParseByte();
				c.b = b.ParseByte();
				c.a = a.ParseByte();
			} catch (Exception) { }


			return c;
		}
		/// <summary> Wraps the parse in a try...catch block and writes to <paramref name="col"/> Writes <see cref="Color.White"/> if parse fails. </summary>
		/// <param name="s"> String to parse </param>
		/// <param name="col"> Color to write to </param>
		/// <returns> true if parse successful, false if parse fails. </returns>
		public static bool TryParse(this string s, out SDColor col) {
			try {
				col = ParseHex(s);
				return true;
			} catch (System.Exception) {
				col = SDColor.White;
				return false;
			}
		}

		private static float Floor(float a) { return (float) Math.Floor(a); }
		private static float Clamp01(float a) { return (a < 0 ? 0 : (a > 1 ? 1 : a));}

		private static float Max(float a, float b) { return a > b ? a : b; }
		private static float Max(float a, float b, float c) {
			return (a > b 
				? (a > c ? a : c)
				: (b > c ? b : c));
		}
		private static float Min(float a, float b) { return a < b ? a : b; }
		private static float Min(float a, float b, float c) {
			return (a < b
				? (a < c ? a : c)
				: (b < c ? b : c));
		}


		#region HSV support
		///Has a number of functions which add support for the HSV color space to unity colors.
		///HSV colors are stored as standard Colors with the same range for values.
		///R maps Hue
		///G maps Saturation
		///B maps Value
		///A carries Alpha just the same.
		///Typical use would be to explicitly mark any variables that should carry HSV information as such
		///And convert to and from HSV space as needed.
		///Some common things (like shifting hue) are supported as extensions.

		///<summary>Construct a new RGB color using given HSV coordinates and alpha value.</summary
		public static SDColor HSV(float h, float s, float v, float a = 1) { return ((SDColor) new Color(h, s, v, a)).HSVtoRGB(); }
		// <summary>Create a RGB color with a randomized hue value, with given saturation and value.</summary>
		// public static Color RandomHue(float s, float v, float a = 1) { return new Color(Random.Range(0, 1), s, v, a).HSVtoRGB(); }

		///<summary>Lerp between colors by HSV coordinates, rather than by RGB coordinates.
		///Returns an RGB color</summary>
		public static SDColor HSVLerp(SDColor a, SDColor b, float val) {
			Color ahsv = a.RGBtoHSV();
			Color bhsv = b.RGBtoHSV();
			return ((SDColor)Color.Lerp(ahsv, bhsv, val)).HSVtoRGB();
		}

		///<summary>Shift the hue of a color by shift percent across the spectrum.
		///Takes an RGB Color
		///Returns an RGB color.</summary>
		public static SDColor ShiftHue(this SDColor c, float shift) {
			Color hsv = c.RGBtoHSV();
			hsv.r = (hsv.r + shift) % 1f;
			return ((SDColor)hsv).HSVtoRGB();
		}

		///<summary>Adds Saturation to an RGB color.
		///Keeps saturation between [0, 1]
		///Returns an RGB color.</summary>
		///<param name="c">Color to modify</param>
		///<param name="saturation">Saturation to add. Range [-1, 1]</param>
		///<returns>Input color with saturation modified</returns>
		public static SDColor Saturate(this SDColor c, float saturation) {
			Color hsv = c.RGBtoHSV();
			hsv.g = Clamp01(hsv.g + saturation);
			return ((SDColor)hsv).HSVtoRGB();
		}
		///<summary>Adds Saturation to an RGB color. Lets saturation escape [0, 1]. Returns an RGB color.</summary>
		///<param name="c">Color to modify</param>
		///<param name="saturation">Saturation to add. Range [-1, 1]</param>
		///<returns>Input color with saturation modified</returns>
		public static SDColor Oversaturate(this SDColor c, float saturation) {
			Color hsv = c.RGBtoHSV();
			hsv.g = hsv.g + saturation;
			return ((SDColor) hsv).HSVtoRGB();
		}

		/// <summary> Returns HSV color matching input RGB color </summary>
		/// <param name="c">RGB color to convert</param>
		/// <returns>HSV version of input</returns>
		public static SDColor RGBtoHSV(this SDColor color) {
			Color c = color;
			Color hsv = new Color(0, 0, 0, c.a);

			float max = Max(c.r, c.g, c.b);
			if (max <= 0) { return hsv; }
			//Value
			hsv.b = max;

			float r, g, b;
			r = c.r;
			g = c.g;
			b = c.b;
			float min = Min(r, g, b);
			float delta = max - min;

			//Saturation
			hsv.g = delta / max;

			//Hue
			float h;
			if (r == max) {
				h = (g - b) / delta;
			} else if (g == max) {
				h = 2 + (b - r) / delta;
			} else {
				h = 4 + (r - g) / delta;
			}

			h /= 6f; // convert h (0...6) space to (0...1) space
			if (h < 0) { h += 1; }

			hsv.r = h;


			return hsv;
		}

		/// <summary> Returns RGB color matching input HSV color </summary>
		/// <param name="c">HSV color to convert</param>
		/// <returns>RGB version of input</returns>
		public static SDColor HSVtoRGB(this SDColor color) {
			Color c = color;
			int i;

			float a = c.a;
			float h, s, v;
			float f, p, q, t;
			h = c.r;
			s = c.g;
			v = c.b;

			if (s == 0) {
				return new Color(v, v, v, a);
			}

			//convert h from (0...1) space to (0...6) space
			h *= 6f;
			i = (int)Floor(h);
			f = h - i;
			p = v * (1 - s);
			q = v * (1 - s * f);
			t = v * (1 - s * (1 - f));

			if (i == 0) {
				return new Color(v, t, p, a);
			} else if (i == 1) {
				return new Color(q, v, p, a);
			} else if (i == 2) {
				return new Color(p, v, t, a);
			} else if (i == 3) {
				return new Color(p, q, v, a);
			} else if (i == 4) {
				return new Color(t, p, v, a);
			}

			return new Color(v, p, q, a);

		}
		#endregion

	}
}
