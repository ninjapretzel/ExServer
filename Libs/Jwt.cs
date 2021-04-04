using System;
using JWT;
using JWT.Algorithms;

namespace Ex {
	/// <summary> Makes more sensible API to the <see cref="JWT"/> library. </summary>
	public class Jwt {

		/// <summary> Retarded. This is already a static API. Lets just add some extra pointers to jump through for no reason. </summary>
		private sealed class StraightSerializer : IJsonSerializer {
			public static readonly StraightSerializer instance = new StraightSerializer();
			private StraightSerializer() { }
			public T Deserialize<T>(string json) { return Json.To<T>(json); }
			public string Serialize(object obj) { return Json.ToJson(obj); }
		}
		/// <summary> Retarded. This is already a static API. Lets just add some extra pointers to jump through for no reason.</summary>
		private sealed class StraightProvider : IDateTimeProvider {
			public static readonly StraightProvider instance = new StraightProvider();
			private StraightProvider() { }
			public DateTimeOffset GetNow() { return DateTimeOffset.UtcNow; }
		}
		/// <summary> Retarded. This is already a static API. Lets just add some extra pointers to jump through for no reason.</summary>
		private sealed class StraightEncoder : IBase64UrlEncoder {
			public static readonly StraightEncoder instance = new StraightEncoder();
			private StraightEncoder() { }
			public byte[] Decode(string input) { return Convert.FromBase64String(input); }
			public string Encode(byte[] input) { return Convert.ToBase64String(input); }
		}

		/// <summary> The implementation for <see cref="HMACSHA256Algorithm"/> is basically a static function already. </summary>
		//private static readonly IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
		private static readonly IJwtAlgorithm algorithm = new HMACSHA512Algorithm();
		/// <summary> <see cref="JwtEncoder"/>s do not have any state, and can just be static anyway. </summary>
		private static readonly JwtEncoder encoder = new JwtEncoder(algorithm, StraightSerializer.instance, StraightEncoder.instance);
		/// <summary> <see cref="JwtValidator"/>s do not have any state, and can just be static anyway. </summary>
		private static readonly JwtValidator validator = new JwtValidator(StraightSerializer.instance, StraightProvider.instance);
		/// <summary> <see cref="JwtDecoder"/>s do not have any state, and can just be static anyway. </summary>
		private static readonly JwtDecoder decoder = new JwtDecoder(StraightSerializer.instance, validator, StraightEncoder.instance, algorithm);
		/// <summary> Provided only as a default, you should provide your own. </summary>
		public static readonly string DEFAULT_SECRET = "REALLY BAD SECRET CHANGE ME PLEASE";

		/// <summary> Default packing into JWT function </summary>
		/// <typeparam name="T"> Generic type to pack </typeparam>
		/// <param name="obj"> Data object to pack </param> 
		/// <param name="secret"> Secret password to use to encode. If not provided during debug mode, <see cref="DEFAULT_SECRET"/> is used. </param>
		/// <param name="expiry"> Time in seconds until token expires. </param>
		/// <returns> Payload encoded in jwt. </returns>
		public static string Encode<T>(T obj, string secret = null, int? expiry = null) {
			#if DEBUG
			if (secret == null) { secret = DEFAULT_SECRET; }
			#endif
			var now = DateTimeOffset.UtcNow;
			JsonObject encoded = Json.Reflect(obj) as JsonObject;
			if (encoded == null) { throw new Exception("Jwt.Encode: The Json representation of encoded JWT data MUST be a JsonObject!"); }
			if (secret == null) { throw new Exception("Jwt.Encode: No secret provided."); }
			if (expiry != null) {
				var exp = UnixEpoch.GetSecondsSince(now);
				encoded["exp"] = exp;
			}
			
			DateTime start = DateTime.UtcNow;
			var result = encoder.Encode(obj, secret);
			DateTime end = DateTime.UtcNow;
			Log.Debug($"Jwt.Encode completed in {(end-start).TotalMilliseconds}ms");
			return result;
		}

		/// <summary> Default unpacking a JWT function </summary>
		/// <typeparam name="T"> Generic type to unpack </typeparam>
		/// <param name="token"> Token to unpack </param>
		/// <param name="result"> Output location </param>
		/// <param name="secret"> Secret to use to unpack. If not provided during debug mode, <see cref="DEFAULT_SECRET"/> is used. </param>
		/// <returns> True if unpacking was successful, false otherwise. </returns>
		public static bool Decode<T>(string token, out T result, string secret = null) {
			#if DEBUG
			if (secret == null) { secret = DEFAULT_SECRET; }
			#endif
			try {
				DateTime start = DateTime.Now;
				string json = decoder.Decode(token, secret, true);
				DateTime end = DateTime.Now;
				Log.Debug($"Jwt.Decode completed in {(end-start).TotalMilliseconds}ms");

				JsonObject obj = Json.Parse<JsonObject>(json);
				if (obj == null) { result = default(T); return false; }
				result = Json.GetValue<T>(obj);
				return true;
			} catch (Exception) {
				result = default(T);
				return false;
			}
		}
	}
}