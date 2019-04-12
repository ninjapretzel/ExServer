using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Ex.Utils {
	public class Packing {
		public static void Unpack<T>(string encoded, out T ret) where T : struct {
			ret = default(T);

			
		}

		public static string Pack<T>(T value) where T : struct {
			byte[] copy = Unsafe.ToBytes(value);
			return "";
		}

	}
}
