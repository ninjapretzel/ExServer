using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Libs {
	public static class ThreadExt {
		public static void TerminateIfNotActive(this Thread t) {
			if (t != null && t != Thread.CurrentThread) {
				try { t.Abort(); } catch { }
			}
		}
		public static void TerminateAll(params Thread[] threads) {
			foreach (var thread in threads) { TerminateIfNotActive(thread); }
			if (threads.Contains(Thread.CurrentThread)) { Thread.CurrentThread.Abort(); }
		}
	}
}
