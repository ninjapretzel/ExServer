using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Ex.Libs {

	public static class Pretty {

		/// <summary> Dictionary of hex codes mapped to their console colors </summary>
		private static Dictionary<char, ConsoleColor> colors = new Dictionary<char, ConsoleColor>() {
			{ '0', ConsoleColor.Black },
			{ '1', ConsoleColor.Red },
			{ '2', ConsoleColor.Green },
			{ '3', ConsoleColor.Yellow },
			{ '4', ConsoleColor.Blue },
			{ '5', ConsoleColor.Cyan },
			{ '6', ConsoleColor.Magenta },
			{ '7', ConsoleColor.White },

			{ '8', ConsoleColor.DarkGray },
			{ '9', ConsoleColor.DarkRed },
			{ 'A', ConsoleColor.DarkGreen },
			{ 'B', ConsoleColor.DarkYellow },
			{ 'C', ConsoleColor.DarkBlue },
			{ 'D', ConsoleColor.DarkCyan },
			{ 'E', ConsoleColor.DarkMagenta },
			{ 'F', ConsoleColor.Gray },
		};
		
		private static Dictionary<string, string> markdownConversion = new Dictionary<string, string>() {
			{ "\\r", $"{INVIS_FG}1"},
			{ "\\o", $"{INVIS_FG}9"},
			{ "\\y", $"{INVIS_FG}3"},
			{ "\\g", $"{INVIS_FG}2"},
			{ "\\b", $"{INVIS_FG}4"},
			{ "\\i", $"{INVIS_FG}D"},
			{ "\\v", $"{INVIS_FG}6"},
			
			{ "\\c", $"{INVIS_FG}5"},
			
			{ "\\w", $"{INVIS_FG}7"},
			{ "\\k", $"{INVIS_FG}0"},
			{ "\\u", $"{INVIS_FG}A"},
			{ "\\h", $"{INVIS_FG}F"},
			{ "\\d", $"{INVIS_FG}8"},
			
			{ "\\1", $"{INVIS_FG}F"},
			{ "\\2", $"{INVIS_FG}9"},
			{ "\\3", $"{INVIS_FG}2"},
			{ "\\4", $"{INVIS_FG}4"},
			{ "\\e", $"{INVIS_FG}B"},
			{ "\\t", $"{INVIS_FG}2"},
			{ "\\p", $"{INVIS_FG}A"},
			{ "\\j", $"{INVIS_FG}A"},
		};
			


		/// <summary> Hex codes packed in an array </summary>
		public static readonly char[] hex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		
		public const char INVIS_BG = (char)0x0E;
		public const char INVIS_FG = (char)0x0F;
		/// <summary> Get a %#^# code for the given bg/fg colors. </summary>
		public static string Code(int bg, int fg) {
			return "" + INVIS_BG + hex[bg % 16] + INVIS_FG + hex[fg % 16];
		}
		
		public static string ConvertMD(string src) {
			foreach (var pair in markdownConversion) {
				src = src.Replace(pair.Key, pair.Value);
			}
			return src;
		}

		public static void Print(string str) {
			PrintDirect(ConvertMD(str));
		}
		public static void PrintDirect(string str) {
			StringBuilder buffer = new StringBuilder();

			for (int i = 0; i < str.Length; i++) {
				// Special character for changing foreground colors
				if (str[i] == INVIS_FG && i + 1 < str.Length) {
					char next = str[i + 1];
					if (colors.ContainsKey(next)) {
						// Write text in previous color 
						Console.Write(buffer.ToString());
						// Clear buffer for text in next color
						buffer.Clear();
						// Set next color
						Console.ForegroundColor = colors[next];
						i++; // consume an extra character
						continue; // Restart loop
					}
				}
				// Special character for changing background colors
				if (str[i] == INVIS_BG && i + 1 < str.Length) {
					char next = str[i + 1];
					if (colors.ContainsKey(next)) {
						// Write text in previous color
						Console.Write(buffer.ToString());
						// Clear buffer for text in next color
						buffer.Clear();
						// Set next color 
						Console.BackgroundColor = colors[next];
						i++; // consume an extra character
						continue; // Restart loop
					}
				}
				buffer.Append(str[i]);
			}

			Console.Write(buffer);

			Console.ResetColor();


		}


	}
}
