using System;
using System.Collections.Generic;
using System.Text;
using static BakaTest.BakaTests;

namespace Ex.Utils {
	/// <summary> Class holding functions for dealing with positional <see cref="Vector3"/>/<see cref="Vector3Int"/> pairs </summary>
	public static class Region {
		/// <summary> Get the true difference between two <see cref="Vector3"/>/<see cref="Vector3Int"/> pairs </summary>
		/// <param name="posA"> First Position </param>
		/// <param name="regionA"> First Region </param>
		/// <param name="posB"> Second Position </param>
		/// <param name="regionB"> Second Region </param>
		/// <param name="regionSize"> Size of region, defaults to 2048 </param>
		/// <returns> Difference between two given pairs </returns>
		public static Vector3 TrueDifference(Vector3 posA, Vector3Int regionA, Vector3 posB, Vector3Int regionB, float regionSize = 2048f) {
			if (regionA == regionB) { return posB - posA; }

			Vector3 regionDiff = regionB - regionA;
			Vector3 posBAdj = posB + regionDiff * regionSize;

			return posBAdj - posA;
		}
		/// <summary> Normalize a <see cref="Vector3"/>/<see cref="Vector3Int"/> pair.
		/// <para>Moves the <paramref name="pos"/> into (-<paramref name="regionSize"/>/2, <paramref name="regionSize"/>/2) for (x,y,z)</para>
		/// <para>and adjusts the <paramref name="region"/> by however many adjustments needed to be made. </para></summary>
		/// <param name="pos"> Reference to Position </param>
		/// <param name="region"> Reference to Region </param>
		/// <param name="regionSize"> Size of region, defaults to 2048 </param>
		public static void Normalize(ref Vector3 pos, ref Vector3Int region, float regionSize = 2048f) {
			float halfSize = regionSize / 2;
			if (Mathf.Abs(pos.x) > halfSize) {
				int dir = (int)Mathf.Sign(pos.x);
				if (Mathf.Abs(pos.x) > regionSize) {
					int step = (1 + (int)((pos.x - halfSize) / regionSize));
					pos.x -= dir * regionSize * step;
					region.x += dir * step;
				} else { pos.x -= dir * regionSize; region.x += (int)dir; }
			}

			if (Mathf.Abs(pos.y) > halfSize) {
				int dir = (int)Mathf.Sign(pos.y);
				if (Mathf.Abs(pos.y) > regionSize) {
					int step = (1 + (int)((pos.y - halfSize) / regionSize));
					pos.y -= dir * regionSize * step;
					region.y += dir * step;
				} else { pos.y -= dir * regionSize; region.y += (int)dir; }
			}

			if (Mathf.Abs(pos.z) > halfSize) {
				int dir = (int)Mathf.Sign(pos.z);
				if (Mathf.Abs(pos.z) > regionSize) {
					int step = (1 + (int)((pos.z - halfSize) / regionSize));
					pos.z -= dir * regionSize * step;
					region.z += dir * step;
				} else { pos.z -= dir * regionSize; region.z += (int)dir; }
			}
		}
	}

	public class Region_Tests {
		public static void TestTrueDifference() {
			Vector3 posA = new Vector3(0, 0, 0);
			Vector3Int regionA = new Vector3Int(0, 0, 0);
			Vector3 posB = new Vector3(0, 0, 0);
			Vector3Int regionB = new Vector3Int(0, 0, 0);
			Vector3 expected = Vector3.zero;
			float regionSize = 2048;
			{
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}
			{
				regionB += Vector3Int.right;
				expected.x = regionSize;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}
			{
				regionA -= Vector3Int.right;
				expected.x = regionSize * 2;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}
			{
				posA = new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				regionA = new Vector3Int(0, 0, 0);
				posB = new Vector3(-regionSize / 2, -regionSize / 2, -regionSize / 2);
				regionB = new Vector3Int(1, 1, 1);
				expected = Vector3.zero;
				Region.TrueDifference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}



		}

		public static void TestNormalize() {
			Vector3 pos = new Vector3(0, 0, 0);
			Vector3Int region = new Vector3Int(0, 0, 0);
			Vector3 expectedPos = Vector3.zero;
			Vector3Int expectedRegion = Vector3Int.zero;
			float regionSize = 2048;
			{
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{
				pos += Vector3.right * regionSize;
				expectedRegion += Vector3Int.right;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{
				pos += Vector3.right * regionSize * 3;
				expectedRegion += Vector3Int.right * 3;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{
				pos += new Vector3(1234, 4567, 8910);
				expectedPos += new Vector3(1234 - regionSize, 4567 - (2 * regionSize), 8910 - (4 * regionSize));
				expectedRegion += new Vector3Int(1, 2, 4);
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{ // Normalize should not move regions when exactly on closest edge
				pos = new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = pos;
				expectedRegion = region;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{ // Normalize may move regions when on an edge further away.
				pos = new Vector3(3 * regionSize / 2, 3 * regionSize / 2, 3 * regionSize / 2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = -new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				expectedRegion = Vector3Int.one * 2;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
			{
				pos = new Vector3(2050, 0, 0);
				region = Vector3Int.zero;
				expectedPos = new Vector3(2, 0, 0);
				expectedRegion = new Vector3Int(1, 0, 0);
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}

		}
	}

}
