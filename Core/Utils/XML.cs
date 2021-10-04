using BakaTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Utils {
	public static class XML {
		public static JsonObject Parse(string xml) {
			var result = new Parser(xml).Parse();
			return result as JsonObject;
		}
		public class Parser {
			private int _i;
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
			private int line;
			private int col;
			private readonly string src;
			public int Length { get { return src.Length; } }
			public char prev{ get { return (i-1 >= 0 && i-1 < Length) ? src[i-1] : '\0'; } }
			public char cur { get { return (i >= 0 && i < Length) ? src[i] : '\0'; } }
			public char next { get { return (i+1 >= 0 && i+1 < Length) ? src[i+1] : '\0'; } }
			public bool eof { get { return cur == '\0'; } }
			public string lineInfo { get { return $"@{line}:{col}"; } }

			public static bool IsWhitespace(char c) { return c == ' ' || c == '\t' || c == '\n' || c == '\r'; }
			public static bool IsAlpha(char c) { return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'); }
			public static bool IsAlphaNum(char c) { return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }
			public bool At(char c) { return cur == c; }
			public bool At(string chars) { char c = cur; for (int i = 0; i < chars.Length; i++) { if (chars[i] == c) { return true; } } return false; }

			public Parser(string src) {
				_i = 0;
				line = 0;
				col = 0;
				this.src = src;
			}

			public JsonValue Parse() {
				
				string leadingWhitespace = SkipWhitespace();

				if (At("<")) {
					i++;
					string tagName = ReadName();
					SkipWhitespace();
					JsonObject tag = new JsonObject();
					JsonObject attr = new JsonObject();
					JsonArray children = new JsonArray();
					tag["tag"] = tagName;
					tag["attr"] = attr;
					tag["selfClosed"] = false;
					tag["children"] = children;
					tag["comment"] = "";

					if (tagName == "!--") {
						tag["comment"] = ReadComment();
						return tag;
					}
					while (!At("/") && !At(">")) {
						if (eof) { return tag; }
						string attrName = ReadName();
						SkipWhitespace();

						if (!At("=")) { throw new Exception($"Expected '=' after attribute name, had '{cur}'. {lineInfo}"); }
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
					if (endName != tagName) { throw new Exception($"Wrong ending tag for '{tagName}', saw '{endName}'. {lineInfo}"); }
					SkipWhitespace();
					if (!At(">")) { throw new Exception($"Expected end angle bracket '>', got '{cur}' {lineInfo}"); }

					i++;
					return tag;
				}
				// Not tag, but text:
				StringBuilder text = new StringBuilder();
				while (!At("<")) {
					if (eof) { return text.ToString(); }
					text.Append(cur);
					i++;
				}
				return text.ToString();
			}

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

			public string ReadName() {
				StringBuilder tag = new StringBuilder();
				while (!(IsWhitespace(cur)) && !At("=") && !At("/") && !At(">")) {
					if (eof) { return tag.ToString(); }
					tag.Append(cur);
					i++;
				}
				return tag.ToString();
			}

			public string SkipWhitespace() {
				StringBuilder ws = new StringBuilder();
				while (IsWhitespace(cur)) {
					if (eof) { return ws.ToString(); }
					ws.Append(cur);
					i++;
				}
				return ws.ToString();
			}

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
</test>
";
			JsonObject parsed = XML.Parse(test);
			//Log.Info(parsed.PrettyPrint());
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
	}
}
