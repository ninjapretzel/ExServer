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
			Incremental.Format(                    1).ShouldBe("1.00");
			Incremental.Format(                   10).ShouldBe("10.00");
			Incremental.Format(                  100).ShouldBe("100.00");
			Incremental.Format(                1_000).ShouldBe("1.00 K");
			Incremental.Format(               10_000).ShouldBe("10.00 K");
			Incremental.Format(              100_000).ShouldBe("100.00 K");
			Incremental.Format(            1_000_000).ShouldBe("1.00 M");
			Incremental.Format(            5_050_000).ShouldBe("5.05 M");
			Incremental.Format(           50_500_000).ShouldBe("50.50 M");
			Incremental.Format(          505_000_000).ShouldBe("505.00 M");
			Incremental.Format(        5_050_000_000).ShouldBe("5.05 B");
			Incremental.Format(       50_500_000_000).ShouldBe("50.50 B");
			Incremental.Format(      505_000_000_000).ShouldBe("505.00 B");
			Incremental.Format(    5_050_000_000_000).ShouldBe("5.05 T");
			Incremental.Format(   50_500_000_000_000).ShouldBe("50.50 T");
			Incremental.Format(  505_000_000_000_000).ShouldBe("505.00 T");
			Incremental.Format(  100_000_000_000_000).ShouldBe("100.00 T");
			Incremental.Format(1_000_000_000_000_000).ShouldBe("1.00 Qa");

			Incremental.Format(1e15).ShouldBe("1.00 Qa");
			Incremental.Format(1e18).ShouldBe("1.00 Qt");
			Incremental.Format(1e21).ShouldBe("1.00 Sx");
			Incremental.Format(1e24).ShouldBe("1.00 Sp");
			Incremental.Format(1e27).ShouldBe("1.00 Oc");
			Incremental.Format(1e30).ShouldBe("1.00 No");
			
			Incremental.Format(1e33).ShouldBe("1.00 Dc");
			Incremental.Format(1e36).ShouldBe("1.00 UDc");
			Incremental.Format(1e39).ShouldBe("1.00 DDc");
			Incremental.Format(1e42).ShouldBe("1.00 TDc");
			Incremental.Format(1e63).ShouldBe("1.00 Vi");
			Incremental.Format(1e66).ShouldBe("1.00 UVi");
			Incremental.Format(1e69).ShouldBe("1.00 DVi");
			Incremental.Format(1e93).ShouldBe("1.00 Tg");



		}
	}
}
