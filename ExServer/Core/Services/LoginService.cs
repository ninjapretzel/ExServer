#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using BDoc = MongoDB.Bson.BsonDocument;
using MDB = MongoDB.Driver.IMongoDatabase;
using Coll = MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>;
#endif
namespace Ex {

	/// <summary> Represents information about a logged in user. </summary>
	public class Credentials {
		/// <summary> Name of user </summary>
		public string username { get; private set; }
		/// <summary> Password hash </summary>
		public string passhash { get; private set; }
		/// <summary> User ID </summary>
		public Guid userId { get; private set; }
		/// <summary> Timestamp of creation </summary>
		public DateTime created { get; private set; }

		/// <summary> Used to create a credentials object on the client. </summary>
		/// <param name="user"> Username </param>
		/// <param name="tokenString"> Guid session token as a string </param>
		public Credentials(string user, string tokenString) {
			username = user;
			passhash = "local";
			Guid guid;
			Guid.TryParse(tokenString, out guid);
			userId = guid;
			created = DateTime.Now;
		}
		/// <summary> Used to create a credentials object on the server, after authenticating the user. </summary>
		/// <param name="user"> Username </param>
		/// <param name="hash"> Password hash </param>
		public Credentials(string user, string hash, Guid userId) {
			username = user;
			passhash = hash;
			this.userId = userId;
			created = DateTime.UtcNow;
		}

	}

	public class LoginService : Service {

		#if !UNITY
		[BsonIgnoreExtraElements]
		public class UserInfo {
			public Guid userId { get; set; }
			public string userName { get; set; }
			public string hash { get; set; }
			public DateTime lastLogin { get; set; }
		}
		#endif

		/// <summary> Serverside, maps client to user ID </summary>
		private Dictionary<Client, Guid> loginsByClient;
		/// <summary> Serverside, maps user ID to client </summary>
		private Dictionary<Guid, Client> loginsByUserId;

		/// <summary> Reference to login information that a local client can use. passhash of this is always "local" if it exists. </summary>
		public Credentials localLogin { get; private set; } = null;

		/// <summary> Is the local Client logged in? </summary>
		public bool LoggedIn { get { return localLogin != null; } }
		/// <summary> Version associated with login endpoint (client and server) must match to allow logins. </summary>
		public string versionCode = null;

		/// <summary> Function used to check for valid usernames. </summary>
		public Func<string, bool> usernameValidator = DefaultValidateUsername;
		/// <summary> Function used to check for valid passwords. </summary>
		public Func<string, bool> passwordValidator = DefaultValidatePassword;
		/// <summary> Hash function. Should be replaced with something more secure than the default. </summary>
		public Func<string, string> Hash = DefaultHash;

		/// <summary> Name of user info collection </summary>
		private static readonly string USER_INFO_COLLECTION = "userInfo";

		public override void OnEnable() {
			loginsByClient = new Dictionary<Client, Guid>();
			loginsByUserId = new Dictionary<Guid, Client>();
		}

		public override void OnDisable() {
			loginsByClient.Clear();
			loginsByUserId.Clear();
		}

		public override void OnConnected(Client client) {
			
		}

		public override void OnDisconnected(Client client) {
			if (loginsByClient.ContainsKey(client)) {
				Guid guid = loginsByClient[client];

				loginsByClient.Remove(client);
				loginsByUserId.Remove(guid);
			}
			
		}

		#if !UNITY
		/// <summary> Client -> Server RPC </summary>
		/// <param name="msg"></param>
		public void Login(Message msg) {
			string user = msg[0];
			string hash = msg[1];
			string version = msg.numArgs >= 3 ? msg[2] : "[[Version Not Set]]";
			


		}
		#endif

		/// <summary> Simple quick check for valid usernames. </summary>
		/// <param name="user"> Name to check </param>
		/// <returns> True if name is between 4 and 64 characters long, and contains only [a-zA-Z_0-9] characters, and does not start with [0-9] </returns>
		public static bool DefaultValidateUsername(string user) {
			if (user == null) { return false; }
			if (user.Length < 4 || user.Length > 64) { return false; }

			for (int i = 0; i < user.Length; i++) {
				char c = user[i];
				if (c > 'z') { return false; } // No characters past 'z' allowed, narrows down valid usernames quick.
				if (c >= 'a') { continue; } // Accept a-z
				if (c > 'Z') { return false; } // Next range is A-Z, reject characters between 'Z' and 'a'.
				if (c >= 'A') { continue; }
				if (c == '_') { continue; }

				// must start with [a-zA-Z_]
				if (i > 0 && c >= '0' && c <= '9') { continue; }

				// Reject any other characters
				return false;
			}

			// If we've passed the whole string without rejecting, it's good.
			return true;
		}
		/// <summary> Simple check for valid passwords. </summary>
		/// <param name="pass"> password to check </param>
		/// <returns> True, if password is at least 8 characters. That's all we care about for simplicity's sake. </returns>
		public static bool DefaultValidatePassword(string pass) {
			if (pass.Length < 4) { return false; }

			return true;
		}
		/// <summary> Default Password 'Hash' function. </summary>
		/// <param name="pass"> password to hash </param>
		/// <returns> Simple salted + hashed result. </returns>
		public static string DefaultHash(string pass) {
			return ":\\:" + (pass + "IM A STUPID HACKER DECOMPILING YOUR CODE LOL").GetHashCode() + ":/:";
		}

	}
}
