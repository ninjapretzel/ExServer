using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Utils {
	public static class ReflectionUtil {

		public static Type FindType(string name) {
			Type t = null;
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
				t = a.GetType(name);
				if (t != null) { return t; }
			}
			return null;
		}
	}
}
