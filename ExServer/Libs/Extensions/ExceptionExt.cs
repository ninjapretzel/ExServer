using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ExceptionExt {
	
	/// <summary> Convert a file or folder path to only contain forward slashes '/' instead of backslashes '\'. </summary>
	/// <param name="path"> Path to convert </param>
	/// <returns> <paramref name="path"/> with all '\' characters replaced with '/' </returns>
	public static string ForwardSlashPath(string path) {
		string s = path.Replace('\\', '/');
		return s;
	}

	/// <summary> Converts backslashes to forward slashes in the info string. </summary>
	/// <param name="e"> Exception to print </param>
	/// <returns> String containing info about an exception, and all of its inner exceptions, with forward slashes in the stack trace paths. </returns>
	public static string FInfoString(this Exception e) {
		return ForwardSlashPath(InfoString(e));
	}
	/// <summary> Converts backslashes to forward slashes in the mini info string. </summary>
	/// <param name="e"> Exception to print </param>
	/// <returns> String containing info about an exception, with forward slashes in the stack trace paths. </returns>
	public static string FMiniInfoString(this Exception e) {
		return ForwardSlashPath(MiniInfoString(e));
	}

	/// <summary> Constructs a string with information about an exception, and all of its inner exceptions. </summary>
	/// <param name="e"> Exception to print. </param>
	/// <returns> String containing info about an exception, and all of its inner exceptions. </returns>
	public static string InfoString(this Exception e) {
		StringBuilder str = "\nException Info: " + e.MiniInfoString();
		str += "\n\tMessage: " + e.Message;
		Exception ex = e.InnerException;

		while (ex != null) {
			str = str + "\n\tInner Exception: " + ex.MiniInfoString();
			ex = ex.InnerException;
		}


		return str;
	}

	/// <summary> Constructs a string with information about an exception. </summary>
	/// <param name="e"> Exception to print </param>
	/// <returns> String containing exception type, message, and stack trace. </returns>
	public static string MiniInfoString(this Exception e) {
		StringBuilder str = e.GetType().ToString();
		str = str + "\n\tMessage: " + e.Message;
		str = str + "\nStack Trace: " + e.StackTrace;
		return ForwardSlashPath(str);
	}

}
