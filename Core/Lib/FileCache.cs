using System;
using System.Threading.Tasks;
using System.IO;
using Ex.Utils;

namespace Ex {
	
	public class FileCache {

		public delegate void MakeRequest(string url, Action<byte[]> callback);
		
		private static string _baseDirectory = "./cache/";
		public static string baseDirectory {
			get { return _baseDirectory; }
			set {
				value = value.Replace('\\', '/');
				if (!value.EndsWith("/")) { value += "/"; }
				_baseDirectory = value;
			}
		}

		private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
		private static readonly char[] invalidFileChars = Path.GetInvalidFileNameChars();

		public static string FileSafe(string str) {
			foreach (char c in invalidFileChars) { str = str.Replace(c, '-'); }
			return str;
		}
		public static string PathSafe(string str) {
			foreach (char c in invalidPathChars) { str = str.Replace(c, '-'); }
			return str.Replace('.', '-');
		}

		public static string LocalPathOf(string url) {
			Uri uri = new Uri(url);

			StringBuilder filepath = baseDirectory + PathSafe(uri.Host) + "/";
			for (int i = 0; i < uri.Segments.Length; i++) {
				string seg = uri.Segments[i];
				if (seg == "/") { continue; }
				filepath += (i == uri.Segments.Length - 1) ? FileSafe(seg) : PathSafe(seg);
			}
			return filepath;
		}
		public static bool HasFile(string url) {
			string path = LocalPathOf(url);
			return File.Exists(path);
		}

		public static MakeRequest requester = (url, callback) => { 
			Task.Run(()=>Request.GetRaw(url, callback));
		};

		public static byte[] GetData(string url) {
			return null;
		}
	}

	
}
