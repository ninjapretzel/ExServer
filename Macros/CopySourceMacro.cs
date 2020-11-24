using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class CopySourceMacro {
#if DEBUG
	public static void CopyAllFiles(string fromDirectory, string toDirectory) {
		var files = GetAllFiles(fromDirectory.ForwardSlashPath()).Select(s => s.ForwardSlashPath());
		Console.WriteLine($"Copying {files.Count()} files in tree \n\tFrom: {fromDirectory}\n\tTo  : {toDirectory}");
		if (Directory.Exists(toDirectory)) {
			Directory.Delete(toDirectory, true);
		}
		if (!Directory.Exists(toDirectory)) {
			Directory.CreateDirectory(toDirectory);
		}
		foreach (var file in files) {
			string filename = file.Filename();
			string relpath = file.RelPath(fromDirectory);
			string destination = $"{toDirectory}{relpath}{filename}".ForwardSlashPath();
			// Console.WriteLine($"Copy for [{relpath}] [{filename}]\n\tFrom: {file}\n\tTo  : {destination}");
			string folder = destination.Folder();
			
			if (!Directory.Exists(folder)) {
				Directory.CreateDirectory(folder);
			}
			string text = File.ReadAllText(file);
			File.WriteAllText(destination, text);

			//File.Copy(file, destination, true);
		}
	}

	public static void FixFiles(string inDirectory) {
		var files = GetAllFiles(inDirectory.ForwardSlashPath()).Select(s => s.ForwardSlashPath())
			.Where(it => it.EndsWith(".cs"));
		Console.WriteLine($"Checking {files.Count()} files in tree:\n{inDirectory}");
		foreach (var file in files) {
			string text = File.ReadAllText(file);
			if (text.Contains("\r\n")) {
				text = text.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n");
				File.WriteAllText(file, text, Encoding.UTF8);
			}
		}
	}

	private static string Filename(this string filepath) {
		return filepath.ForwardSlashPath().FromLast("/");
	}
	private static string Folder(this string filepath) {
		return filepath.UpToLast("/");
	}

	private static string ForwardSlashPath(this string path) { return path.Replace('\\', '/'); }
	private static string UpToLast(this string str, string search) {
		if (str.Contains(search)) {
			int ind = str.LastIndexOf(search);
			return str.Substring(0, ind);
		}
		return str;
	}
	private static string FromLast(this string str, string search) {
		if (str.Contains(search) && !str.EndsWith(search)) {
			int ind = str.LastIndexOf(search);

			return str.Substring(ind + search.Length);
		}
		return "";
	}

	private static string RelPath(this string filepath, string from) {
		return filepath.Replace(from, "").Replace(filepath.Filename(), "");
	}

	private static List<string> GetAllFiles(string dirPath, List<string> collector = null) {
		if (collector == null) { collector = new List<string>(); }

		collector.AddRange(Directory.GetFiles(dirPath));
		foreach (var subdir in Directory.GetDirectories(dirPath)) {
			GetAllFiles(subdir, collector);
		}

		return collector;
	}
#endif
}
