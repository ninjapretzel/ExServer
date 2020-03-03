using Ex.Libs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

public static class RichTextBoxHelpers {

	/// <summary> Appends all messages to the box. </summary>
	public static void Append(this RichTextBox box, IEnumerable<RichTextBoxMessage> messages) {
		bool wasReadOnly = box.ReadOnly;
		box.ReadOnly = false;
		foreach (var message in messages) { box.AppendInternal(message); }
		box.ReadOnly = wasReadOnly;
	}
	/*
	/// <summary> Appends all messages to the box. </summary>
	public static void Append(this RichTextBox box, RichTextBoxMessage[] messages) {
		foreach (var message in messages) { box.Append(message); }
	}
	//*/
	/// <summary> Appends the message to the box </summary>
	public static void Append(this RichTextBox box, RichTextBoxMessage message) {
		bool wasReadOnly = box.ReadOnly;
		box.ReadOnly = false;
		box.Append(message.message, message.color);
		box.ReadOnly = wasReadOnly;
	}

	/// <summary> Appends the text to the box, optionally setting its color. </summary>
	public static void Append(this RichTextBox box, string text, Color? color = null) {
		bool wasReadOnly = box.ReadOnly;
		box.ReadOnly = false;
		box.AppendInternal(text, color);
		box.ReadOnly = wasReadOnly;
	}
	/// <summary> Appends the text to the box, then appends a newline. </summary>
	public static void AppendLn(this RichTextBox box, string text, Color? color = null) {
		bool wasReadOnly = box.ReadOnly;
		box.ReadOnly = false;
		AppendInternal(box, text, color);
		AppendInternal(box, "\n");
		box.ReadOnly = wasReadOnly;
	}

	public static void AppendInternal(this RichTextBox box, RichTextBoxMessage message) {
		box.Append(message.message, message.color);
	}
	private static void AppendInternal(this RichTextBox box, string text, Color? color = null) {
		box.SelectionStart = box.TextLength;
		box.SelectionLength = 0;
		if (color != null) { box.SelectionColor = color.Value; }
		box.SelectedText = text;
	}

	/// <summary> Attempts to scroll the text box to bottom. </summary>
	public static void ScrollToBottom(this RichTextBox box) {
		box.SelectionStart = box.TextLength;
		box.SelectionLength = 0;
		box.ScrollToCaret();
	}

	[DllImport("user32.dll")] private static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);
	[DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, ref Point lParam);

	/// <summary> Attempts to check if the box is scrolled to the bottom. </summary>
	public static bool IsScrolledToBottom(this RichTextBox box) {
		const int SB_VERT = 1;
		const int WM_USER = 0x400;
		// const int EM_SETSCROLLPOS = WM_USER + 222;
		const int EM_GETSCROLLPOS = WM_USER + 221;

		int minScroll;
		int maxScroll;

		GetScrollRange(box.Handle, SB_VERT, out minScroll, out maxScroll);
		Point rtfPoint = Point.Empty;
		SendMessage(box.Handle, EM_GETSCROLLPOS, 0, ref rtfPoint);

		return (rtfPoint.Y + box.ClientSize.Height >= maxScroll);
	}
	
}

/// <summary> Struct for containing a message intended to be appended to a RichTextBox. </summary>
public struct RichTextBoxMessage {

	/// <summary> Color of message, or null to use previous color. </summary>
	public Color? color;
	/// <summary> Message to append. </summary>
	public string message;
	
	public RichTextBoxMessage(string message, Color? color = null) { this.message = message; this.color = color; }

}

public static class RTBExtensions {
	public static Color color(float r, float g, float b) {
		return Color.FromArgb(((int)(r * 255)), ((int)(g * 255)), ((int)(b * 255)));
	}

	public static Color hex(int hex) {
		int r = (hex >> 16) & 0xFF;
		int g = (hex >> 8) & 0xFF;
		int b = (hex >> 0) & 0xFF;
		return Color.FromArgb(r,g,b);
	}

	public static Dictionary<char, Color> colorCodes {get;private set;} = DefaultColorCodes();
	public static Dictionary<char, Color> DefaultColorCodes() {
		Dictionary<char, Color> codes = new Dictionary<char, Color>();

		// My Colors
		// Rainbow, ROYGBIV 
		codes['r'] = color(.95f, .1f, 0);
		codes['o'] = color(.8f, .6f, 0);
		codes['y'] = color(1, .925f, .02f);
		codes['g'] = color(.2f, 1f, 0f);
		codes['b'] = color(.15f, .15f, 1);
		codes['i'] = color(.35f, 0, 1);
		codes['v'] = color(.8f, 0, 1);

		// Cyan
		codes['c'] = color(0f, .85f, .95f);

		// White/blacK
		codes['w'] = Color.White;
		codes['k'] = Color.Black;
		//codes['u'] = Colors.HSV(76f / 360f, .77f, .60f);
		codes['u'] = hex(0x799923);

		// "H"alf Grey
		codes['h'] = color(.5f, .5f, .5f);
		// "D"ark Grey
		codes['d'] = color(.25f, .25f, .25f);
		// "Q"uarter Grey
		//codes['q'] = color(.75f, .75f, .75f);
		
		// obsidian color codes
		codes['1'] = hex(0xF1F2F3); // PlainTextA
		codes['2'] = hex(0xEC7600); // String Orange
		codes['3'] = hex(0x93C763); // Keyword Green
		codes['4'] = hex(0x678CB1); // Blue 

		// bright orangE
		codes['e'] = color(.95f, .45f, 0);

		codes['t'] = color(.8f, 1, .8f);
		// codes['p'] = color(.8f, 1, .8f);
		codes['p'] = hex(0x578E3A); // p for pepe
		codes['j'] = color(.5259f, .7098f, .8508f);
		codes['m'] = color(.8259f, .3214f, .8109f);
		codes['l'] = color(.5151f, .9131f, .3212f);

		// Ace Online's Colors
		codes['q'] = hex(0xDDDDDD);
		codes['f'] = hex(0xFFBB33); // was actually 'e'. but I like.
		codes['u'] = hex(0x77BB22); // was actually 'o'. but I like.
		codes['z'] = hex(0xFF66EE); // was actually 'p'. but I like.
		codes['a'] = hex(0xBBBBFF);
		codes['x'] = hex(0x00AAFF); // was actually 'l'. but I like.
		codes['n'] = hex(0xAAFF00);
		codes['s'] = hex(0xFF00AA); // doesn't exist in ace but whatever

		// Some were same as others or just not good shades.
		//codes['w'] = hex(0xFFFFFF);
		//codes['z'] = hex(0x000000);
		//codes['d'] = hex(0x777777);
		//codes['q'] = hex(0xDDDDDD);
		// Ew too much saturation on these :
		//codes['r'] = hex(0xFF0000); 
		//codes['y'] = hex(0xFFFF00); 
		//codes['g'] = hex(0x00FF00);
		//codes['c'] = hex(0x00FFFF);
		//codes['b'] = hex(0x00FFFF);
		//codes['m'] = hex(0xFF00FF);


		return codes;
	}

	private static Regex colorCode = new Regex(@"\\[A-Za-z]");
	public static List<RichTextBoxMessage> Rich(this string str) {
		List<RichTextBoxMessage> msgs = new List<RichTextBoxMessage>();
		
		int last = 0;
		Color? next = null;
		while (last < str.Length && colorCode.IsMatch(str, last)) {
			Match match = colorCode.Match(str, last);
			string mat = match.Value;
			string rep = match.Value;
			rep = "";

			char ch = mat[1];
			
			//Console.WriteLine($"Match: {match} last: {last} / {str.Length}");
			string colStr = next.HasValue ? next.Value.HexString() : "None";
			//Console.WriteLine($"Using color: {colStr}");
			string nextThing = colorCodes.ContainsKey(ch)
				? str.Substring(last, match.Index - last)
				: str.Substring(last, match.Index - last + match.Length);
			RichTextBoxMessage rtbm = new RichTextBoxMessage(nextThing, next);

			if (colorCodes.ContainsKey(ch)) {
				next = colorCodes[ch];
			} else {
				next = null;
			}

			msgs.Add(rtbm);
			//Console.WriteLine($"Moved {match.Index} + {match.Length}");
			last = match.Index + match.Length;
			
		}

		if (last < str.Length) {
			msgs.Add(new RichTextBoxMessage(str.Substring(last), next));
		}
		
		return msgs;
	}



}
