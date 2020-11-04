using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Tiny request library </summary>
	public static class Request {
		/// <summary> Instance of <see cref="HttpClient"/> singleton. </summary>
		public static readonly HttpClient http = new HttpClient();

		/// <summary> Callback to fire on any errors (eg for logging) </summary>
		public static Action<string, Exception> onError;

		/// <summary> Attempt to GET the given URL and handle the response as a <see cref="string"/></summary>
		/// <param name="url"> URL to GET from </param>
		/// <param name="callback"> Callback to fire on success </param>
		public static void Get(string url, Action<string> callback) {
			Task.Run(async () => { await GetAsync(url, callback); });
		}
		/// <summary> Attempt to GET the given URL and handle the response as a <see cref="string"/></summary>
		/// <param name="url"> URL to GET from </param>
		/// <param name="callback"> Callback to fire on success </param>
		/// <returns> Awaitable <see cref="Task"/> </returns>
		public static async Task GetAsync(string url, Action<string> callback) {
			try {
				HttpResponseMessage response = await http.GetAsync(url);

				await Finish(response, callback);
			} catch (Exception e) {
				onError?.Invoke($"Exception during GET raw", e);
			}
		}
		/// <summary> Attempt to GET the given URL and handle the response as a <see cref="byte[]"/></summary>
		/// <param name="url"> URL to GET from </param>
		/// <param name="callback"> Callback to fire on success </param>
		public static void GetRaw(string url, Action<byte[]> callback) {
			Task.Run( async ()=>{ await GetRawAsync(url, callback); });
		}
		/// <summary> Attempt to GET the given URL and handle the response as a <see cref="byte[]"/></summary>
		/// <param name="url"> URL to GET from </param>
		/// <param name="callback"> Callback to fire on success </param>
		/// <returns> Awaitable <see cref="Task"/> </returns>
		public static async Task GetRawAsync(string url, Action<byte[]> callback) {
			try {
				HttpResponseMessage response = await http.GetAsync(url);
				
				await Finish(response, callback);
			} catch (Exception e) {
				onError?.Invoke($"Exception during GET raw", e);
			}
		}

		/// <summary> Attempt to POST to the given URL and handle the response as a <see cref="string"/> </summary>
		/// <param name="url"> URL to POST to </param>
		/// <param name="content"> string to POST </param>
		/// <param name="callback"> Callback to fire on success </param>
		/// <param name="prep"> Custom generator of C#'s retarded <see cref="HttpContent"/>, if you want a different encoding or "ContentType"</param>
		/// <returns> Awaitable Task </returns>
		/// <remarks> Invokes <see cref="onError"/> during any failures. </remarks>
		public static void Post(string url, string content, Action<string> callback, Func<string, HttpContent> prep = null) {
			Task.Run( async ()=>{ await PostAsync(url, content, callback, prep); } );
		}

		/// <summary> Attempt to POST to the given URL and handle the response as a <see cref="string"/> </summary>
		/// <param name="url"> URL to POST to </param>
		/// <param name="content"> string to POST </param>
		/// <param name="callback"> Callback to fire on success </param>
		/// <param name="prep"> Custom generator of C#'s retarded <see cref="HttpContent"/>, if you want a different encoding or "ContentType", or any custom headers. </param>
		/// <returns> Awaitable Task </returns>
		/// <remarks> Invokes <see cref="onError"/> during any failures. </remarks>
		public static async Task PostAsync(string url, string content, Action<string> callback, Func<string, HttpContent> prep = null) {
			try {
				HttpContent request;

				if (prep == null) {
					request = new StringContent(content, Encoding.UTF8, "application/json");

				} else {
					request = prep(content);
				}

				HttpResponseMessage response = await http.PostAsync(url, request);
				await Finish(response, callback);
			} catch (Exception e) {
				onError?.Invoke($"Exception during POST", e);
			}
		}


		private static async Task Finish(HttpResponseMessage response, Action<string> callback) {
			if (response.IsSuccessStatusCode) {
				string result = await response.Content.ReadAsStringAsync();
				callback(result);
			} else {
				onError?.Invoke($"Bad status code from {response.RequestMessage.Method}", null);
			}
		}

		private static async Task Finish(HttpResponseMessage response, Action<byte[]> callback) {
			if (response.IsSuccessStatusCode) {
				byte[] result = await response.Content.ReadAsByteArrayAsync();
				callback(result);
			} else {
				onError?.Invoke($"Bad status code from {response.RequestMessage.Method}", null);
			}
		}
	}
}
