using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class ControlExt {
	public static void ForAll(this Control parent, Action<Control> action) {
		action(parent);
		foreach (Control control in parent.Controls) {
			ForAll(control, action);
		}
	}
	public static void ApplyTheme(this Control c, Theme theme) {
		c.BackColor = theme.bgColor;
		c.ForeColor = theme.textColor;
		if (c is Button) { c.BackColor = theme.buttonColor; }
		if (c is MenuStrip || c is Form) { c.BackColor = theme.windowColor; }
	}
}

public class Theme {
	public Color windowColor = Color.FromArgb(45, 45, 48);
	public Color buttonColor = Color.FromArgb(51, 51, 51);
	public Color bgColor = Color.FromArgb(41, 49, 52);
	public Color textColor = Color.FromArgb(224, 226, 228);
}
