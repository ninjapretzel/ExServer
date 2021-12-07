using BakaTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Utils {
	/// <summary> Class holding XML parsing and generating </summary>
	public static class XML {

		/// <summary> Class representing the basic structure returned by <see cref="Parse"/> in <see cref="JsonObject"/> form </summary>
		public class Node {
			/// <summary> Any leading whitespace present before the tag, if it was the first tag in the file. </summary>
			public string leadingWhitespace;
			/// <summary> name of the tag </summary>
			public string tag;
			/// <summary> attributes on the tag </summary>
			public JsonObject attr;
			/// <summary> flag if the tag was self closed </summary>
			public bool selfClosed;
			/// <summary> <see cref="string"/>|<see cref="Node"/>s that are the direct children of this object </summary>
			public List<object> children;
			/// <summary> comment contents, if this node is a comment (tag == "!--") </summary>
			public string comment;
		}

		/// <summary> Parse an XML string into a traversable <see cref="JsonObject"/> holding its tree structure </summary>
		/// <param name="xml"> XML to parse </param>
		/// <returns> <see cref="JsonObject"/> holding XML structure mirroring <see cref="Node"/> </returns>
		public static JsonObject Parse(string xml) {
			var result = new Parser(xml).Parse();
			return result as JsonObject;
		}

		public static void Traverse(JsonObject node, Action<JsonObject> action) {
			if (node == null) { return; }
			action(node);
			var children = node["children"] as JsonArray;
			if (children == null) { return; }
			foreach (var child in children) {
				if (child is JsonObject obj) { Traverse(obj, action); }
			}
		}

		/// <summary> Converts a <see cref="JsonValue"/> to an XML string </summary>
		/// <param name="elem"> either <see cref="JsonObject"/> mirroring a <see cref="Node"/> or just a <see cref="JsonString"/>. </param>
		/// <param name="indent"> Current indent level </param>
		/// <param name="indentStr"> Current indent character </param>
		/// <returns></returns>
		public static string ToXML(JsonValue elem, int indent = 0, string indentStr = "\t") {
			if (elem.isString) { return elem.stringVal; }
			if (elem is JsonObject tag) {
				StringBuilder tagstr = new StringBuilder();
				if (tag.Has("leadingWhitespace")) { tagstr.Append(tag["leadingWhitespace"].stringVal); }
				
				for (int k = 0; k < indent; k++) { tagstr.Append(indentStr); }
				tagstr.Append($"<{tag["tag"].stringVal}");
				if (tag["attr"].Count > 0) {
					tagstr.Append(' ');
					foreach (var pair in tag["attr"] as JsonObject) {
						tagstr.Append($"{pair.Key.stringVal}=\"{pair.Value.stringVal}\" ");
					}
				}
				
				if (tag["selfClosed"]) { 
					tagstr.Append("/>"); 
					return tagstr.ToString(); 
				} else if (tag["comment"] != "") { 
					tagstr.Append($"{tag["comment"].stringVal}-->"); 
					return tagstr.ToString(); 
				} else { tagstr.Append(">"); }

				int i = 0;
				bool prevWasText = false;
				foreach (var child in tag["children"] as JsonArray) {
					i++;
					string childStr = ToXML(child, indent+=1, indentStr);

					if (child is JsonString) { prevWasText = true; }
					else {
						if (prevWasText) {
							// Unset flag and remove duplicate indentation
							prevWasText = false;
							childStr = childStr.Substring(indent);
						} else {
							tagstr.Append("\n");
						}
					}

					tagstr.Append(childStr);
				}

				if (!prevWasText) {
					tagstr.Append("\n");
					//for (int k = 0; k < indent; k++) { tagstr.Append(indentStr); }
				}
				tagstr.Append($"</{tag["tag"].stringVal}>");

				return tagstr.ToString();
			}
			return "";
		}

		/// <summary> Internal parser class </summary>
		public class Parser {
			/// <summary> actual index </summary>
			private int _i;
			/// <summary> property that counts lines and columns as index moves forwards </summary>
			private int i {
				get { return _i; }
				set {
					while (_i < value) {
						if (cur == '\n') { line++; col = 0; }
						else { col++; }
						_i++;
					}
					if (value < _i) { _i = value; }
				}
			}
			/// <summary> Current line number </summary>
			private int line;
			/// <summary> Current column </summary>
			private int col;
			/// <summary> Source XML </summary>
			private readonly string src;
			/// <summary> Length of source </summary>
			public int Length { get { return src.Length; } }
			/// <summary> Character before current, '\0' if invalid. </summary>
			public char prev{ get { return (i-1 >= 0 && i-1 < Length) ? src[i-1] : '\0'; } }
			/// <summary> Current character , '\0' if invalid. </summary>
			public char cur { get { return (i >= 0 && i < Length) ? src[i] : '\0'; } }
			/// <summary> Character after current, '\0' if invalid. </summary>
			public char next { get { return (i+1 >= 0 && i+1 < Length) ? src[i+1] : '\0'; } }
			/// <summary> Flag if <see cref="cur"/> is null. </summary>
			public bool eof { get { return cur == '\0'; } }
			/// <summary> Property to quickly generate line:col information. </summary>
			public string lineInfo { get { return $"@{line+1}:{col}"; } }

			public string nearbyText { 
				get {
					StringBuilder str = new StringBuilder();
					int k = i;
					int atLine = line;
					int linesUp = 0;
					while (k > 0 && linesUp < 3) {
						k--;
						if (src[k] == '\n') { linesUp++; atLine--; }
					}

					int linesDown = 0;
					while (k < Length) { 
						str.Append(src[k]);
						if (src[k] == '\n') {
							atLine++;
							linesDown++;
							if (linesDown == 8) { break; }
							string s = ""+ (1 + atLine);
							while (s.Length < 8) { s = (atLine == line ? ">" : " ") + s; }
							str.Append($"{s}: ");
						}
						k++;
					}
					return str.ToString();
				}
			}

			/// <summary> Is a character of the whitespace class?. </summary>
			public static bool IsWhitespace(char c) { return c == ' ' || c == '\t' || c == '\n' || c == '\r'; }
			/// <summary> Is a character of the alpha class?. </summary>
			public static bool IsAlpha(char c) { return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'); }
			/// <summary> Is a character of the alphanum class?. </summary>
			public static bool IsAlphaNum(char c) { return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }
			/// <summary> Is the current character <paramref name="c"/>? </summary>
			public bool At(char c) { return cur == c; }
			/// <summary> Is the current character one of <paramref name="chars"/>? </summary>
			public bool At(string chars) { char c = cur; for (int i = 0; i < chars.Length; i++) { if (chars[i] == c) { return true; } } return false; }

			/// <summary> Constructor </summary>
			/// <param name="src"> Source to parse </param>
			public Parser(string src) {
				_i = 0;
				line = 0;
				col = 0;
				this.src = src;
			}

			/// <summary> Actually parse </summary>
			/// <returns> Parsed structure as a <see cref="JsonObject"/> mirroring <see cref="Node"/>, otherwise the <see cref="string"/> of text. </returns>
			public JsonValue Parse() {
				string leadingWhitespace = SkipWhitespace();

				if (At("<")) {
					i++;


					string tagName = ReadName();
					if (tagName == "!DOCTYPE" || tagName == "?xml") {
						while (!At(">")) { i++; }
						i++;
						return Parse();
					}
					JsonObject tag = new JsonObject();
					JsonObject attr = new JsonObject();
					JsonArray children = new JsonArray();
					if (leadingWhitespace.Length > 0) {
						tag["leadingWhitespace"] = leadingWhitespace;
					}
					tag["tag"] = tagName;
					tag["attr"] = attr;
					tag["selfClosed"] = false;
					tag["children"] = children;
					tag["comment"] = "";

					// Log.Info($"Saw tag TagName: {tagName}");
					if (tagName.StartsWith("!--")) {
						tag["comment"] = ReadComment();
						return tag;
					}
					SkipWhitespace();
					while (!At("/") && !At(">")) {
						if (eof) { return tag; }
						string attrName = ReadName();
						SkipWhitespace();

						if (!At("=")) { throw new Exception($"Expected '=' after attribute name, had '{cur}'. {lineInfo}\n{nearbyText}"); }
						i++;
						SkipWhitespace();

						string attrVal = ReadString();
						SkipWhitespace();
						attr[attrName] = attrVal;
					}

					bool selfClosed = At('/');
					tag["selfClosed"] = selfClosed;
					if (selfClosed) {
						i += 2;
						return tag;
					} 
					i++;

					string innerWhitespace = SkipWhitespace();
					while (!(cur == '<' && next == '/')) {
						if (eof) { return tag; }
						var child = Parse();
						if (child.isString) { child = innerWhitespace + child.stringVal; }
						else if (innerWhitespace.Length > 0) { children.Add(innerWhitespace); }

						children.Add(child);
						innerWhitespace = SkipWhitespace();
					}

					i += 2;
					string endName = ReadName();
					if (endName != tagName) { throw new Exception($"Wrong ending tag for '{tagName}', saw '{endName}'. {lineInfo}\n{nearbyText}"); }
					SkipWhitespace();
					if (!At(">")) { throw new Exception($"Expected end angle bracket '>', got '{cur}' {lineInfo}\n{nearbyText}"); }

					i++;
					return tag;
				}
				// Not tag, but text:
				StringBuilder text = new StringBuilder(leadingWhitespace);
				while (!At("<")) {
					if (eof) { return text.ToString(); }
					text.Append(cur);
					i++;
				}
				return text.ToString();
			}

			/// <summary> Reads a string from the current position </summary>
			/// <returns> read string </returns>
			public string ReadString() {
				StringBuilder val = new StringBuilder();
				char openChar = '\0';
				if (cur == '\'' || cur == '\"') {
					openChar = cur;
					i++;
				}

				while (openChar != cur) {
					if (openChar == '\0' && IsWhitespace(cur)) { break; }
					if (eof) { return val.ToString(); }
					val.Append(cur);
					i++;
				}
				if (openChar != '\0') { i++; }
				
				return val.ToString();
			}
			/// <summary> Reads a name from the current position </summary>
			/// <returns> read string </returns>
			public string ReadName() {
				StringBuilder tag = new StringBuilder();
				while (!(IsWhitespace(cur)) && !At("=") && !At("/") && !At(">")) {
					if (eof) { return tag.ToString(); }
					tag.Append(cur);
					i++;
				}
				return tag.ToString();
			}
			/// <summary> Skips whitespace from the current position </summary>
			/// <returns> read whitespace </returns>
			public string SkipWhitespace() {
				StringBuilder ws = new StringBuilder();
				while (IsWhitespace(cur)) {
					if (eof) { return ws.ToString(); }
					ws.Append(cur);
					i++;
				}
				return ws.ToString();
			}
			/// <summary> Reads a comment from the current position </summary>
			/// <returns> read string </returns>
			public string ReadComment() {
				StringBuilder text = new StringBuilder();
				while (!(prev == '-' && cur == '-' && next == '>')) {
					if (eof) { return text.ToString(); }
					text.Append(cur);
					i++;
				}
				if (At("-")) {
					text.Length -= 1; // remove one '-'
					i += 2; // move past final '->'
				}
				return text.ToString();
			}
			
		}
	}

	public static class XMLParser_Tests {
		public static void TestBasic() {
			string test = @"
<test>
	<!-- THIS IS A COMMENT -->
	<test2/>
	<!-- THIS IS A COMMENT -->
	<test3 a='b' c='d'>
		<!-- THIS IS A COMMENT -->
		this is text: x y z w
		<!-- THIS IS A COMMENT -->
	</test3>
	<test4>
		Some Text
		<test5 />
		Some Text
		<test5 />
		Some Text
	</test4>
	<!-- THIS IS A COMMENT -->
</test>".Replace('\'', '\"');
			JsonObject parsed = XML.Parse(test);
			//Log.Info(parsed.PrettyPrint());

			void check(JsonObject parsed) {
				parsed.ShouldNotBe(null);
				parsed["tag"].ShouldEqual("test");
				parsed["selfClosed"].ShouldEqual(false);
				parsed["children"].Count.ShouldBe(12);
				parsed["attr"].Count.ShouldBe(0);

				parsed["children"][1]["tag"].ShouldEqual("!--");

				parsed["children"][3]["tag"].ShouldEqual("test2");
				parsed["children"][3]["selfClosed"].ShouldEqual(true);


				parsed["children"][5]["tag"].ShouldEqual("!--");

				parsed["children"][7]["tag"].ShouldEqual("test3");
				parsed["children"][7]["selfClosed"].ShouldEqual(false);
				parsed["children"][7]["children"].Count.ShouldBe(4);
				parsed["children"][7]["attr"]["a"].ShouldEqual("b");
				parsed["children"][7]["attr"]["c"].ShouldEqual("d");

				parsed["children"][9]["tag"].ShouldEqual("test4");
				parsed["children"][9]["children"].Count.ShouldBe(5);
				parsed["children"][9]["children"][1]["tag"].ShouldEqual("test5");
				parsed["children"][9]["children"][1]["selfClosed"].ShouldEqual(true);
				parsed["children"][9]["children"][3]["tag"].ShouldEqual("test5");
				parsed["children"][9]["children"][3]["selfClosed"].ShouldEqual(true);
			}

			check(parsed);
			string toStringd = XML.ToXML(parsed);
			// Can't guarantee that the generated XML looks anything like what was originally parsed
			// but we can guarantee there's a similar structure and certain contents.
			JsonObject backToXML = XML.Parse(toStringd);
			check(backToXML);
			
		}
	}
}
