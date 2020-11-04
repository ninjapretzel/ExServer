using BakaTest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Class representing no return type from promises. </summary>
	public sealed class Void { private Void() { } }

	/// <summary> Type similar to JS promises to bridge sync/async code. </summary>
	/// <typeparam name="T"> Generic type of task result. </typeparam>
	public class Promise<T> {
		/// <summary> Delegate type for `resolve` callback. </summary>
		/// <param name="value"> Value to resolve promise to </param>
		public delegate void Resolve(T value);
		/// <summary> Delegate type for `reject` callback. </summary>
		/// <param name="error"> Exception to reject with </param>
		public delegate void Reject(Exception error);
		/// <summary> Delegate type expected for code passed to promises. </summary>
		/// <param name="resolve"> <see cref="Resolve"/> callback, which sets the task result to the supplied value. </param>
		/// <param name="reject"> <see cref="Reject"/> callback, which sets the task exception to the supplied exception. </param>
		public delegate void Body(Resolve resolve, Reject reject);

		/// <summary> Internal <see cref="TaskCompletionSource{T}"/> which is essentially wrapped. </summary>
		public TaskCompletionSource<T> promise { get; private set; }
		/// <summary> Constructs a new <see cref="Promise{T}"/> with the given <see cref="Body"/> executor </summary>
		/// <param name="body"> <see cref="Body"/> executor to use in function body. </param>
		/// <remarks> The body is run with <see cref="Task.Run(Action)"/></remarks>
		public Promise(Body body) {
			promise = new TaskCompletionSource<T>();

			Task.Run(() => {
				body(
					(result)=>{ promise.TrySetResult(result);},
					(error)=>{ promise.TrySetException(error);}
				);
			});
		}

		/// <summary> Chains a new <see cref="Promise{T}"/> that will wait on this one to complete. </summary>
		/// <typeparam name="TNext"> Generic type returned by next <see cref="Promise{T}"/> in the chain </typeparam>
		/// <param name="callback"> Next function in the chain. </param>
		/// <returns> A <see cref="Promise{T}"/> that waits on this one to complete, then runs the <paramref name="callback"/>. </returns>
		/// <remarks> Any errors are propagated to the next downstream <see cref="Catch"/> call. </remarks>
		public Promise<TNext> Then<TNext>(Func<T, TNext> callback) { 
			return new Promise<TNext>((resolve, reject)=>{
				try {
					Task.WaitAll(this);
					if (promise.Task.Exception != null) {
						reject(promise.Task.Exception);
					} else {
						TNext result = callback(promise.Task.Result);
						resolve(result);
					}
				} catch (Exception e) {
					reject(e);
				}
			});
		}

		/// <summary> Chains a new <see cref="Promise{Task}"/> that will wait on the current one to complete or fail, 
		/// and handle any errors generated at any point in the chain. </summary>
		/// <param name="callback"> Function to use to handle the error. </param>
		/// <returns> A <see cref="Promise{T}"/> that waits on this one to complete, and handles any errors. </returns>
		public Promise<Void> Catch(Action<Exception> callback) {
			return new Promise<Void>((resolve, reject)=>{
				bool handled = false;
				try {
					Task.WaitAll(this);
					if (promise.Task.Exception != null) {
						handled = true;
						callback(promise.Task.Exception);
					}
				} catch (Exception e) {
					if (!handled) { callback(e); }
				}
				resolve(null);
			});
		}

		/// <summary> Implicit conversion that allows <see cref="Promise{T}"/> to be transmuted into <see cref="Task{TResult}"/>. </summary>
		/// <param name="p"> <see cref="Promise{T}"/> to convert </param>
		public static implicit operator Task<T>(Promise<T> p) { return p.promise.Task; }

		/// <summary> Fulfill the invisible "IAwaitable" interface. </summary>
		/// <returns> <see cref="System.Runtime.CompilerServices.TaskAwaiter{T}"/> to wait for thist task. </returns>
		public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter() { return promise.Task.GetAwaiter(); }

	}

	public class Promise_Tests {
		public static void TestingBasics() {
			int? result = null;
			Promise<int> promise = new Promise<int>((resolve, reject) => {
				//Task.WaitAll(Task.Delay(50));
				resolve(5);
			});
			async Task<int> test() {
				int r = await promise;
				result = r;
				return r;
			}
			Task.WaitAll(test());
			result.ShouldEqual(5);
		}

		public static void TestExceptionThrown() {
			int? result = null;
			Exception error1 = null;
			Exception error2 = null;
			Promise<int> promise = new Promise<int>((resolve, reject) => {
				reject(new Exception("Oops"));
			});
			async Task<int> test() {
				try {
					int r = await promise;
					result = r;
					return r;
				} catch (Exception e) {
					error1 = e;
					throw e;
				}
			}

			try { Task.WaitAll(test()); } 
			catch (Exception e) { error2 = e; }

			(result == null).ShouldBeTrue();

			error1.ShouldNotBe(null);
			error2.ShouldNotBe(null);
			error2.InnerException.ShouldBe(error1);
		}

		public static void TestThen() {
			int? result = null;
			Promise<int> promise = new Promise<int>((resolve, reject) => {
				for (int i = 0; i < 10; i++) {
					Task.WaitAll(Task.Run( async ()=>{await Task.Yield();} ));
				}
				resolve(5);
			}).Then((r)=> {result = r; return r; });
			
			async Task<int> test() { return await promise; }

			Task.WaitAll(test());

			result.ShouldEqual(5);
		}

		public static void TestPromiseChaining() {
			int addOne(int v) { return v + 1; }
			Promise<int> p = new Promise<int>((resolve, reject)=>{ resolve(0);})
				.Then(addOne).Then(addOne).Then(addOne)
				.Then(addOne).Then(addOne).Then(addOne);

			Task.WaitAll(p);

			p.promise.Task.Result.ShouldEqual(6);
		}

		public static void TestTypeChanging() {
			Promise<float> p = new Promise<int>((resolve, reject) => { resolve(255); })
				.Then(i => (double)i)
				.Then(i => (byte)i)
				.Then(i => (short)i)
				.Then(i => (long)i)
				.Then(i => (float)i);

			Task.WaitAll(p);
			p.promise.Task.Result.ShouldEqual(255f);
		}

		public static void TestExceptionChaining() {
			int addOneOrError(int v, int e) {
				if (v == e) { throw new Exception("Gottem"); }
				return v + 1;
			}

			void test(int n, int e) {
				int step = 0;
				int curried(int v) { 
					step = v;
					return addOneOrError(v, e); 
				}

				Promise<int> p = new Promise<int>((resolve, reject)=>{ resolve(0); });
				for (int i = 0; i < n; i++) { p = p.Then(curried); }

				bool oops = false;
				try {
					Task.WaitAll(p);
					oops = true;

				} catch (Exception) {
					step.ShouldEqual(e);
				}
				oops.ShouldBeFalse();
			}

			for (int i = 0; i < 10; i++) {
				test(10, i);
			}

		}

	}
}
