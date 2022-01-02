#if UNITY_2017_1_OR_NEWER
#define UNITY
#endif

#if UNITY
using UnityEngine;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using static BakaTest.BakaTests;

namespace Ex.Utils {
	/// <summary> Class holding functions for dealing with positional <see cref="Vector3"/>/<see cref="Vector3Int"/> pairs
	/// mainly to circumvent floating point precision problems (eg past 10k, there tends to be loss of fine details) </summary>
	public struct Region {

		/// <summary> Default value to use for regionSize </summary>
		public const float DEFAULT_REGION_SIZE = 2048f;

		/// <summary> Position within region </summary>
		public Vector3 position;
		/// <summary> Region indexes </summary>
		public Vector3Int indexes;

		/// <summary> Construct <see cref="Region"/> with region index zero and given <paramref name="position"/>. </summary>
		/// <param name="position">Position within region</param>
		public Region(Vector3 position) {
			this.position = position;
			this.indexes = Vector3Int.zero;
		}
		/// <summary> Construct <see cref="Region"/>  with given <paramref name="position"/> and <paramref name="indexes"/></summary>
		/// <param name="position">Position within region</param>
		/// <param name="indexes">Region indexes</param>
		public Region(Vector3 position, Vector3Int indexes) {
			this.position = position;
			this.indexes = indexes;
		}

		/// <summary> Normalizes this <see cref="Region"/> with the give <paramref name="regionSize"/> </summary>
		/// <param name="regionSize"> Size of region to use, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		public void Normalize(float regionSize = DEFAULT_REGION_SIZE) {
			Normalize(ref position, ref indexes, regionSize);
		}

		/// <summary> Get the difference between this and another <see cref="Region"/>. </summary>
		/// <param name="other"> <see cref="Region> to compare to </param>
		/// <param name="regionSize"> Size of region to use, defaults to <see cref="DEFAULT_REGION_SIZE"/></param>
		/// <returns> Difference between this and the given <see cref="Region> "/></returns>
		public Vector3 Difference(Region other, float regionSize = DEFAULT_REGION_SIZE) {
			return Difference(position, indexes, other.position, other.indexes, regionSize);
		}

		/// <summary> Denormalize this <see cref="Region"/> in relation to the given <paramref name="perspective"/>. </summary>
		/// <param name="perspective"> <see cref="Region"/> to denormalize compared to </param>
		/// <param name="regionSize"> Size of region to use, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		public void RelativeFrom(Region perspective, float regionSize = DEFAULT_REGION_SIZE) {
			Relative(perspective.indexes, ref position, ref indexes, regionSize);
		}

		/// <summary> Denomalize the <paramref name="other"/> <see cref="Region"/> in relation to this as the perspective. </summary>
		/// <param name="other"> <see cref="Region"/> to denormalize compared to this </param>
		/// <param name="regionSize"> Size of region to use, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		public void RelativeTo(ref Region other, float regionSize = DEFAULT_REGION_SIZE) {
			Relative(indexes, ref other.position, ref other.indexes, regionSize);
		}

		/// <summary> Get the true position of this <see cref="Region"/>. </summary>
		/// <param name="regionSize"> Size of region to use, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		/// <returns> Lossy, "true" position of this Vector3. </returns>
		public Vector3 TruePosition(float regionSize = DEFAULT_REGION_SIZE) { 
			Vector3 idx = indexes;
			return position + idx * regionSize;
		}

		/// <summary> Get the true difference between two <see cref="Vector3"/>/<see cref="Vector3Int"/> pairs </summary>
		/// <param name="posA"> First Position </param>
		/// <param name="regionA"> First Region </param>
		/// <param name="posB"> Second Position </param>
		/// <param name="regionB"> Second Region </param>
		/// <param name="regionSize"> Size of region, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		/// <returns> Difference between two given pairs </returns>
		public static Vector3 Difference(Vector3 posA, Vector3Int regionA, Vector3 posB, Vector3Int regionB, float regionSize = DEFAULT_REGION_SIZE) {
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
		/// <param name="regionSize"> Size of region, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		public static void Normalize(ref Vector3 pos, ref Vector3Int region, float regionSize = DEFAULT_REGION_SIZE) {
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

		/// <summary> Denormalize <paramref name="pos"/> and <paramref name="region"/> relative to <paramref name="perspective"/>. </summary>
		/// <param name="perspective"> Perspective region indexes </param>
		/// <param name="pos"> Position to denormalize </param>
		/// <param name="region"> Region to denormalize </param>
		/// <param name="regionSize"> Size of regions, defaults to <see cref="DEFAULT_REGION_SIZE"/> </param>
		public static void Relative(Vector3Int perspective, ref Vector3 pos, ref Vector3Int region, float regionSize = DEFAULT_REGION_SIZE) {
			Vector3Int regionDiff = (region - perspective);
			pos += ((Vector3)regionDiff) * regionSize;
			region -= regionDiff;
		}

	}

	public class Region_Tests {
		const float TEST_REGION_SIZE = 2048;
		public static void TestTrueDifference() {
			Vector3 posA = new Vector3(0, 0, 0);
			Vector3Int regionA = new Vector3Int(0, 0, 0);
			Vector3 posB = new Vector3(0, 0, 0);
			Vector3Int regionB = new Vector3Int(0, 0, 0);
			Vector3 expected = Vector3.zero;
			float regionSize = TEST_REGION_SIZE;
			{
				Region.Difference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				regionB += Vector3Int.right;
				expected.x = regionSize;
				Region.Difference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				regionA -= Vector3Int.right;
				expected.x = regionSize * 2;
				Region.Difference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			} {
				posA = new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				regionA = new Vector3Int(0, 0, 0);
				posB = new Vector3(-regionSize / 2, -regionSize / 2, -regionSize / 2);
				regionB = new Vector3Int(1, 1, 1);
				expected = Vector3.zero;
				Region.Difference(posA, regionA, posB, regionB, regionSize).ShouldEqual(expected);
			}



		}

		public static void TestNormalize() {
			Vector3 pos = new Vector3(0, 0, 0);
			Vector3Int region = new Vector3Int(0, 0, 0);
			Vector3 expectedPos = Vector3.zero;
			Vector3Int expectedRegion = Vector3Int.zero;
			float regionSize = TEST_REGION_SIZE;
			{
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos += Vector3.right * regionSize;
				expectedRegion += Vector3Int.right;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos += Vector3.right * regionSize * 3;
				expectedRegion += Vector3Int.right * 3;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos += new Vector3(1234, 4567, 8910);
				expectedPos += new Vector3(1234 - regionSize, 4567 - (2 * regionSize), 8910 - (4 * regionSize));
				expectedRegion += new Vector3Int(1, 2, 4);
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} { // Normalize should not move regions when exactly on closest edge
				pos = new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = pos;
				expectedRegion = region;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} { // Normalize may move regions when on an edge further away.
				pos = new Vector3(3 * regionSize / 2, 3 * regionSize / 2, 3 * regionSize / 2);
				region = new Vector3Int(0, 0, 0);
				expectedPos = -new Vector3(regionSize / 2, regionSize / 2, regionSize / 2);
				expectedRegion = Vector3Int.one * 2;
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
				pos = new Vector3(2050, 0, 0);
				region = Vector3Int.zero;
				expectedPos = new Vector3(2, 0, 0);
				expectedRegion = new Vector3Int(1, 0, 0);
				Region.Normalize(ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			}
		}

		public static void TestRelative() {
			float regionSize = TEST_REGION_SIZE;
			Vector3Int perspective = new Vector3Int(4, 5, 6);
			Vector3 pos = new Vector3(0, 0, 0);
			Vector3Int region = new Vector3Int(1, 2, 3);
			Vector3 expectedPos = Vector3.one * -3 * regionSize;
			Vector3Int expectedRegion = perspective;
			{ 
				Region.Relative(perspective, ref pos, ref region, regionSize);
				pos.ShouldEqual(expectedPos);
				region.ShouldEqual(expectedRegion);
			} {
			
			}

		}
	}

}
