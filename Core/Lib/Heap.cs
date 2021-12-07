using BakaTest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ex.Utils {

	/// <summary> Class that implements a min-heap structure for any type that implements <see cref="IComparable{T}"/></summary>
	/// <typeparam name="T"> Generic type contained within </typeparam>
	public class Heap<T> : IEnumerable<T> where T : IComparable<T> {
		/// <summary> Default capacity of a new Heap </summary>
		public const int DEFAULT_CAPACITY = 20;
		/// <summary> Default growth factor of a new Heap </summary>
		public const float GROWTH = 1.5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Parent(int i) { return (i - 1) / 2; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Left(int i) { return i * 2 + 1; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Right(int i) { return i * 2 + 2; }

		/// <summary> Delegate for overriding comparison functions. </summary>
		/// <param name="a"> First parameter </param>
		/// <param name="b"> Second parameter </param>
		/// <returns> 0 when a == b, negative number when a &lt; b, positive number when a &gt b </returns>
		public delegate int Compare(T a, T b);

		/// <summary> Heapify an array in-place. </summary>
		/// <param name="ts"> Array to heapify </param>
		/// <param name="cnt"> Number of elements to heapify. if not provided, entire array length is heapified </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void Heapify(T[] ts, int? cnt = null, Compare compare = null) {
			int n = cnt.HasValue ? cnt.Value : ts.Length;
			for (int i = ts.Length - 1; i >= 0; i--) {
				SiftDown(ts, i, n, compare);
			}
		}

		/// <summary> Sift a given index upwards. </summary>
		/// <param name="ts"> Array of values to sift </param>
		/// <param name="index"> Index of item </param>
		/// <param name="cnt"> maximum index to consider. </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void SiftUp(T[] ts, int index, int cnt, Compare compare = null) {
			// [MethodImpl(MethodImplOptions.AggressiveInlining)]
			int cmp(T a, T b) { return (compare == null) ? a.CompareTo(b) : compare(a, b); }

			if (index < 0 || index >= cnt) { return; }
			int i = index;
			int parent = Parent(i);
			T t = ts[i];
			while (cmp(t, (ts[parent])) <= 0) {
				ts[i] = ts[parent];
				ts[parent] = t;
				i = parent;
				parent = Parent(i);
				if (i == 0) { break; }
			}
		}



		/// <summary> Sift a given index downwards. </summary>
		/// <param name="ts"> Array of values to sift </param>
		/// <param name="index"> Index of item </param>
		/// <param name="cnt"> maximum index to consider. </param>
		/// <param name="compare"> Optional override comparison function. If not provided, default `<see cref="IComparable{T}.CompareTo(T?)"/> is used. </param>
		public static void SiftDown(T[] ts, int index, int cnt, Compare compare = null) {
			// [MethodImpl(MethodImplOptions.AggressiveInlining)]
			int cmp(T a, T b) { return (compare == null) ? a.CompareTo(b) : compare(a, b); }

			if (index < 0 || index >= cnt) { return; }
			int i = index;
			T t = ts[i];
			while (true) {
				int left = Left(i);
				int right = Right(i);
				if (left >= cnt) { break; }

				if (right < cnt) {
					T tL = ts[left];
					T tR = ts[right];

					if (cmp(tL, tR) <= 0) {
						if (cmp(t, tL) > 0) {
							ts[i] = ts[left];
							ts[left] = t;
							i = left;
							continue;
						} else { break; }
					} else {
						if (cmp(t, tR) > 0) {
							ts[i] = ts[right];
							ts[right] = t;
							i = right;
							continue;
						} else { break; }
					}
				} else {
					if (cmp(t, ts[left]) > 0) {
						ts[i] = ts[left];
						ts[left] = t;
						i = left;
					} else { break; }
				}
			}

		}

		/// <summary> Current number of items in the heap </summary>
		public int Count { get { return cnt; } }
		/// <summary> Is the heap currently empty? </summary>
		public bool IsEmpty { get { return cnt == 0; } }

		/// <summary> Public access to comparator. Re-heapifies internal array on every write. </summary>
		public Compare Comparator {
			get { return comparator; }
			set {
				comparator = value;
				Heapify(ts, cnt, comparator);
			}
		}

		/// <summary> Internal comparator. </summary>
		private Compare comparator;

		/// <summary> Current items in heap </summary>
		private T[] ts;
		/// <summary> Current count field </summary>
		private int cnt;

		/// <summary> Empty constructor </summary>
		public Heap() {
			ts = new T[DEFAULT_CAPACITY];
			cnt = 0;
		}

		public Heap(Compare cmp) : this() {
			comparator = cmp;
		}

		/// <summary> Copy constructor </summary>
		/// <param name="ts"> Array of values to copy </param>
		public Heap(T[] ts, Compare cmp = null) {
			this.ts = new T[ts.Length];
			Array.Copy(ts, this.ts, ts.Length);
			cnt = ts.Length;
			comparator = cmp;
			Heapify(this.ts, cnt, comparator);
		}

		/// <summary> Creates a heap, which internally uses the given array (unlike copy constructor). </summary>
		/// <param name="ts"> Array to wrap a heap around </param>
		/// <param name="cnt"> Number of items to place into the heap </param>
		/// <returns> Heap constructed around the given array </returns>
		public static Heap<T> From(T[] ts, Compare cmp = null) {
			Heap<T> h = new Heap<T>();
			h.ts = ts;
			h.comparator = cmp;
			h.cnt = ts.Length;
			Heapify(ts, h.cnt, h.comparator);
			return h;
		}

		/// <summary> Returns the minimal element in the heap </summary>
		/// <returns> Element at position 0 in heap </returns>
		public T Peek() {
			if (cnt == 0) { throw new InvalidOperationException("Heap is empty, cannot Peek."); }
			return ts[0];
		}

		/// <summary> Adds the given element to the heap structure </summary>
		/// <param name="item"> item to add to heap </param>
		public void Push(T item) {
			if (cnt == ts.Length) { Grow(); }
			ts[cnt] = item;
			cnt++;
			SiftUp(cnt - 1);
		}

		/// <summary> Removes the minimal element from the heap </summary>
		/// <returns> Element that was previously at position 0 in heap </returns>
		public T Pop() {
			if (cnt == 0) { throw new InvalidOperationException("Heap is empty, cannot Pop."); }
			T t = ts[0];
			if (cnt == 1) {
				ts[0] = default(T);
				cnt = 0;
			} else {
				ts[0] = ts[cnt - 1];
				ts[cnt - 1] = default(T);
				cnt--;
				SiftDown(0);
			}

			return t;
		}

		/// <inheritdoc/>
		public override string ToString() {
			StringBuilder str = new StringBuilder($"Heap<{typeof(T)}> [ ");
			for (int i = 0; i < cnt; i++) {
				str.Append(ts[i]);
				str.Append(", ");
			}
			str.Append("]");
			return str.ToString();
		}

		/// <summary> Internal function to grow <see cref="ts"/> for more space </summary>
		private void Grow() {
			T[] newTs = new T[(int)(ts.Length * GROWTH)];
			Array.Copy(ts, newTs, cnt);
			ts = newTs;
		}

		/// <summary> Internal function to sift upwards </summary>
		/// <param name="index"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SiftUp(int index) { SiftUp(ts, index, cnt, comparator); }

		/// <summary> Internal function to sift downwards </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SiftDown(int index) { SiftDown(ts, index, cnt, comparator); }

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() { return new Enumerator(this); }

		public class Enumerator : IEnumerator<T> {
			private int pos;
			private Heap<T> heap;
			public Enumerator(Heap<T> heap) { this.heap = heap; pos = -1; }

			public T Current { get { return heap.ts[pos]; } }
			object IEnumerator.Current { get { return heap.ts[pos]; } }
			public void Dispose() { }
			public bool MoveNext() { return ++pos < heap.Count; }
			public void Reset() { pos = -1; }
		}
	}


	public static class Heap_Tests {
		public static void TestSimple() {
			Heap<string> heap = new Heap<string>();
			heap.Push("20");
			heap.Push("30");
			heap.Push("10");
			heap.Peek().ShouldBe("10");
			heap.ToString().ShouldBe("Heap<System.String> [ 10, 30, 20, ]");

			heap.Push("40");
			heap.Push("50");
			heap.Push("60");
			heap.Push("70");
			heap.Pop().ShouldBe("10");
			heap.Pop().ShouldBe("20");
			heap.Pop().ShouldBe("30");

			heap.Pop().ShouldBe("40");
			heap.Pop().ShouldBe("50");
			heap.Pop().ShouldBe("60");
			heap.Pop().ShouldBe("70");
			heap.IsEmpty.ShouldBeTrue();
		}
		public static void TestHeapify() {
			int[] ints = new int[] { 70, 50, 30, 20, 40, 60, 10, 80 };
			Heap<int> heap = Heap<int>.From(ints);

			heap.Pop().ShouldBe(10);
			heap.Pop().ShouldBe(20);
			heap.Pop().ShouldBe(30);
			heap.Pop().ShouldBe(40);
			heap.Pop().ShouldBe(50);
			heap.Pop().ShouldBe(60);
			heap.Pop().ShouldBe(70);
			heap.Pop().ShouldBe(80);

		}
		public static void TestCustomCompare() {
			int[] ints = new int[] { 70, 50, 30, 20, 40, 60, 10, 80 };
			Heap<int> heap = Heap<int>.From(ints, (a, b) => { return b - a; });

			heap.Pop().ShouldBe(80);
			heap.Pop().ShouldBe(70);
			heap.Pop().ShouldBe(60);
			heap.Pop().ShouldBe(50);
			heap.Pop().ShouldBe(40);
			heap.Pop().ShouldBe(30);
			heap.Pop().ShouldBe(20);
			heap.Pop().ShouldBe(10);
		}

		public static void TestIterator() {
			int[] ints = new int[] { 70, 50, 30, 20, 40, 60, 10, 80 };
			Heap<int> heap = Heap<int>.From(ints);
			int cnt = 0;
			foreach (int i in heap) {
				cnt++;
			}
			cnt.ShouldBe(ints.Length);

		}
	}

}
