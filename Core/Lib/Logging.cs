﻿#if UNITY_WEBGL
#define NOTHREADS
#endif

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Ex.Utils;

namespace Ex {

	/// <summary> Enum of log levels. </summary>
	public enum LogLevel {
		/// <summary> Disables all logging from the BakaNet library, minus exceptions. </summary>
		Error = 0,
		/// <summary> Logs only important information, such as when servers/clients start and stop. </summary>
		Warning = 1,
		/// <summary> Logs most information, such as when tasks start and stop.  </summary>
		Info = 2,
		/// <summary> Logs more information, such as network messages </summary>
		Debug = 3,
		/// <summary> Logs ALL information, including heartbeats. </summary>
		Verbose = 4,
	}

	
	public delegate void Logger(LogInfo info);

	/// <summary> Log message info passed on to <see cref="Logger"/> callbacks. </summary>
	public struct LogInfo {
		/// <summary> Severity of logging </summary>
		public LogLevel level { get; private set; }
		/// <summary> Log Message </summary>
		public string message { get; private set; }
		/// <summary> Log Tag </summary>
		public string tag { get; private set; }
		public LogInfo(LogLevel level, string message, string tag) {
			this.level = level;
			this.message = message;
			this.tag = tag;
		}
	}

	/// <summary> Class handling statically accessible logging </summary>
	public static class Log {
		
		public static readonly string[] LEVEL_CODES = { "\\r", "\\y", "\\w", "\\h", "\\d" };
		public static string defaultTag = "Baka";
		
		public static string ColorCode(LogLevel level) { return (LEVEL_CODES[(int)level]); }
		/// <summary> Path to use to filter file paths </summary>
		public static string ignorePath = null;
		/// <summary> Path to insert infront of filtered paths </summary>
		public static string fromPath = null;

		/// <summary> True to insert backslash color codes. </summary>
		public static bool colorCodes = true;
		
		/// <summary> Log handler to use to print logs </summary>
		public static Logger logHandler;
		/// <summary> Queue of unhandled <see cref="LogInfo"/>s </summary>
		public static readonly ConcurrentQueue<LogInfo> logs = new ConcurrentQueue<LogInfo>();

		/// <summary> If logging is currently running </summary>
		private static bool go = false;
		/// <summary> Thread handling logging  </summary>
		private static Thread logThread = InitializeLoggingThread();
		/// <summary> Initializes thread that handles logging </summary>
		private static Thread InitializeLoggingThread() {
			go = true;
			#if !NOTHREADS
			Thread t = new Thread(() => {
				LogInfo info;
				while (go) {
					while (logs.TryDequeue(out info)) {
						try {
							logHandler.Invoke(info);
						} catch (Exception) { /* Can't really log when there's an exception */ }
					}
					Thread.Sleep(1);
				}
			});
			t.Start();
			return t;
			#else
			return null;
			#endif
		}
		/// <summary> Stops the logging thread (after a delay) </summary>
		public static void Stop() {
			go = false;
		}
		/// <summary> Restarts the logging thread </summary>
		public static void Restart() {
			go = false;
			#if !NOTHREADS
			logThread.Join();
			logThread = InitializeLoggingThread();
			#endif
		}

		/// <summary> Logs a message using the Verbose LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Verbose(object obj, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Verbose, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Verbose LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Verbose(object obj, Exception ex, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Verbose, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Debug LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Debug(object obj, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Debug, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Debug LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Debug(object obj, Exception ex, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Debug, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Info LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object obj, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Info, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Info LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object obj, Exception ex, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Info, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Warning LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warning(object obj, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Warning, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Warning LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warning(object obj, Exception ex, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Warning, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Error LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object obj, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Error, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Error LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object obj, Exception ex, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Error, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Primary workhorse logging method with all options. </summary>
		/// <param name="ex"> Exception to log </param>
		/// <param name="obj"> Message to log </param>
		/// <param name="level"> Minimum log level to use </param>
		/// <param name="tag"> Tag to log with </param>
		/// <param name="callerName">Name of calling method </param>
		/// <param name="callerPath">File of calling method </param>
		/// <param name="callerLine">Line number of calling method </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void log(Exception ex, object obj, LogLevel level = LogLevel.Info, string tag = null,
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			if (obj == null) { obj = "[null]"; }
			if (tag == null) { tag = defaultTag; }
			string callerInfo = CallerInfo(callerName, callerPath, callerLine, level);
			string message = (colorCodes ? ColorCode(level) : "") + obj.ToString() 
				+ (ex != null ? $"\n{ex.InfoString()}" : "")
				+ callerInfo;

			#if !NOTHREADS
			logs.Enqueue(new LogInfo(level, message, tag));
			#else
			if (go) {
				logHandler.Invoke(new LogInfo(level, message, tag));
			}
			#endif
		}


		/// <summary> Little helper method to consistantly format caller information </summary>
		/// <param name="callerName"> Name of method </param>
		/// <param name="callerPath"> Path of file method is contained in </param>
		/// <param name="callerLine"> Line in file where log is called. </param>
		/// <returns>Formatted caller info</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] // Gotta go fast. 
		public static string CallerInfo(string callerName, string callerPath, int callerLine, LogLevel level) {
			string path = (fromPath != null ? fromPath : "")
				+ (ignorePath != null && callerPath.Contains(ignorePath)
					? callerPath.Substring(callerPath.IndexOf(ignorePath) + ignorePath.Length)
					: callerPath);
			return (colorCodes ? "\\d" : "")
				+ $"\n{level.ToString()[0]}: [{DateTime.UtcNow.UnixTimestamp()}] by "
				+ ForwardSlashPath(path)
				+ $" at {callerLine} in {callerName}()";
		}
		private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long UnixTimestamp(this DateTime date) {
			TimeSpan diff = date.ToUniversalTime().Subtract(epoch);
			return (long)diff.TotalMilliseconds;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DateTime DateTimeFromUnixTimestamp(this long ms) {
			return epoch.AddMilliseconds(ms);
		}
		/// <summary> Convert a file or folder path to only contain forward slashes '/' instead of backslashes '\'. </summary>
		/// <param name="path"> Path to convert </param>
		/// <returns> <paramref name="path"/> with all '\' characters replaced with '/' </returns>
		public static string ForwardSlashPath(string path) {
			string s = path.Replace('\\', '/');
			return s;
		}
		
		/// <summary> Constructs a string with information about an exception, and all of its inner exceptions. </summary>
		/// <param name="e"> Exception to print. </param>
		/// <returns> String containing info about an exception, and all of its inner exceptions. </returns>
		public static string InfoString(this Exception e) {
			StringBuilder str = "\nException Info: " + e.MiniInfoString();
			str += "\n\tMessage: " + ForwardSlashPath(e.Message);
			Exception ex = e.InnerException;

			while (ex != null) {
				str = str + "\n\tInner Exception: " + ex.MiniInfoString();
				ex = ex.InnerException;
			}


			return ForwardSlashPath(str);
		}

		/// <summary> Constructs a string with information about an exception. </summary>
		/// <param name="e"> Exception to print </param>
		/// <returns> String containing exception type, message, and stack trace. </returns>
		public static string MiniInfoString(this Exception e) {
			StringBuilder str = e.GetType().ToString();
			str = str + "\n\tMessage: " + ForwardSlashPath(e.Message);
			str = str + "\nStack Trace: " + e.StackTrace;
			return str;
		}

	}
}
