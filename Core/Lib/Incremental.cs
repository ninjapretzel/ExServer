using BakaTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ex.Libs {

	public static class Incremental {

		static readonly string[] basicFixes = { "", "K", "M", "B", "T", "Qa", "Qt", "Sx", "Sp", "Oc", "No" };
		static readonly string[] preFixes = { "", "U", "D", "T", "Qa", "Qt", "Sx", "Sp", "Oc", "No", };
		static readonly string[] postFixes = { "Dc", "Vi", "Tg", "Qd", "Qq", "Sg", "St", "Ot", "Ng", };
		static readonly string[] fixes = LoadFixes();
		
		public static string[] LoadFixes() {
			List<string> fixes = new List<string>();
			fixes.AddRange(basicFixes);
			
			foreach (var post in postFixes) {
				foreach (var pre in preFixes) {
					fixes.Add(pre+post);
				}
			}

			return fixes.ToArray();
		}

		private static readonly double LOG_1000 = Math.Log(1000);

		public static string Format(double num, int places = 2) {

			if (num < 1000) { return string.Format($"{{0:F{places}}}", num); }
			
			double b = Math.Log(num) / LOG_1000;;
			int bn = (int)b ;
			double p = Math.Pow(1000, bn);
			string fmt = $"{{0:F{places}}} {fixes[bn]}";
			//Log.Info($"num:{num} - bn:{bn} - p:{p} - num/p:{num/p} - fmt:\"{fmt}\"");
			string str = string.Format(fmt, num / p);

			return str;
		}


	}

	public static class zzIncremental_Tests {
		public static void TestThing() {
			Incremental.Format(1).ShouldBe("1.00");
			Incremental.Format(10).ShouldBe("10.00");
			Incremental.Format(100).ShouldBe("100.00");
			Incremental.Format(1000).ShouldBe("1.00 K");
			Incremental.Format(10000).ShouldBe("10.00 K");
			Incremental.Format(100000).ShouldBe("100.00 K");
			Incremental.Format(1000000).ShouldBe("1.00 M");
			Incremental.Format(5050000).ShouldBe("5.05 M");
			Incremental.Format(50500000).ShouldBe("50.50 M");
			Incremental.Format(505000000).ShouldBe("505.00 M");
			Incremental.Format(5050000000).ShouldBe("5.05 B");
			Incremental.Format(50500000000).ShouldBe("50.50 B");
			Incremental.Format(505000000000).ShouldBe("505.00 B");
			Incremental.Format(5050000000000).ShouldBe("5.05 T");
			Incremental.Format(50500000000000).ShouldBe("50.50 T");
			Incremental.Format(505000000000000).ShouldBe("505.00 T");
			Incremental.Format(100000000000000).ShouldBe("100.00 T");
			Incremental.Format(1000000000000000).ShouldBe("1.00 Qa");

		}
	}
}
