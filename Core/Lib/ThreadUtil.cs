#if UNITY_WEBGL
#define NOTHREADS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
namespace Ex.Utils{
	public static class ThreadUtil {
		/// <summary> Pause for the given <paramref name="ms"/> or yield. </summary>
		/// <param name="ms"> Ms to pause for, or if &lt;= 0, yield.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Hold(int ms) {
			#if NOTHREADS
			// noop...
			#else
			if (ms <= 0) { Thread.Yield(); } else { Thread.Sleep(ms); }
			#endif
		}
		/// <summary> Kill a thread if and only if it is not the active thread. </summary>
		/// <param name="t"> Thread to try to kill </param>
		/// <returns> Returns true if an abort was attempted, false if it was the current thread or null. </returns>
		public static bool TerminateIfNotActive(this Thread t) {
			if (t != null && t != Thread.CurrentThread) {
				try { t.Abort(); } catch { }
				return true;
			}
			return false;
		}
		/// <summary> Try to abort all passed threads. </summary>
		/// <param name="threads"> Threads to terminate </param>
		public static void TerminateAll(params Thread[] threads) {
			foreach (var thread in threads) { TerminateIfNotActive(thread); }
			if (threads.Contains(Thread.CurrentThread)) { Thread.CurrentThread.Abort(); }
		}
	}

}
