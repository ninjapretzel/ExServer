#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#endif
namespace Ex {

	/// <summary> Represents information about a logged in user. </summary>
	public class Credentials {
		/// <summary> Name of user </summary>
		public string username { get; private set; }
		/// <summary> Password hash </summary>
		public string passhash { get; private set; }
		/// <summary> User ID, equal to guid value of token if valid, otherwise <see cref="Guid.Empty"/> </summary>
		public Guid userId { get; private set; }
		/// <summary> Token provided by server, typically a valid <see cref="Guid"/></summary>
		public string token { get; private set; }
		/// <summary> Timestamp of creation </summary>
		public DateTime created { get; private set; }

		/// <summary> Used to create a credentials object on the client. </summary>
		/// <param name="user"> Username </param>
		/// <param name="token"> Guid session token as a string </param>
		public Credentials(string user, string token) {
			username = user;
			passhash = "local";
			Guid guid;
			this.token = token;
			if (!Guid.TryParse(token, out guid)) { guid = Guid.Empty; }
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
		public class UserInfo : DBEntry {
			public string userName { get; set; }
			public string hash { get; set; }
			public DateTime lastLogin { get; set; }
		}

		[BsonIgnoreExtraElements]
		public class LoginAttempt : DBEntry {
			public string userName { get; set; }
			public string hash { get; set; }
			public DateTime timestamp { get; set; }
			public string result { get; set; }
			public string ip { get; set; }
		}
#endif
		/// <summary> Pair of information about a logged in client </summary>
		public struct Session {
			public Credentials credentials { get; private set; }
			public Client client { get; private set; }
			public Session(Client client, Credentials credentials) {
				this.client = client;
				this.credentials = credentials;
			}
		}
		
		/// <summary> Serverside, maps client to user ID </summary>
		private Dictionary<Client, Session> loginsByClient;
		/// <summary> Serverside, maps user ID to client </summary>
		private Dictionary<Guid, Session> loginsByUserId;

		/// <summary> Gets a login by a user ID </summary>
		/// <param name="userId"> ID of user to check for </param>
		/// <returns> Login information for user, if they are currently logged in </returns>
		public Session? GetLogin(Guid userId) {
			if (isSlave) { return null; }
			if (loginsByUserId.ContainsKey(userId)) {
				return loginsByUserId[userId];
			}
			return null;
		}

		/// <summary> Gets a login by a client ID </summary>
		/// <param name="client"> Client to check for </param>
		/// <returns> Login information for client, if they are currently logged in. </returns>
		public Session? GetLogin(Client client) {
			if (isSlave) { return null; }
			if (loginsByClient.ContainsKey(client)) {
				return loginsByClient[client];
			}
			return null;
		}

		/// <summary> Reference to login information that a local client can use. passhash of this is always "local" if it exists. </summary>
		public Credentials localLogin { get; private set; } = null;

		/// <summary> Is the local Client logged in? </summary>
		public bool LoggedIn { get { return localLogin != null; } }
		/// <summary> Version associated with login endpoint (client and server) must match to allow logins. </summary>
		public string versionCode { get { return VersionInfo.VERSION; } }

		/// <summary> Function used to check for valid usernames. </summary>
		public Func<string, bool> usernameValidator = DefaultValidateUsername;
		/// <summary> Function used to check for valid passwords. </summary>
		public Func<string, bool> passwordValidator = DefaultValidatePassword;
		/// <summary> Hash function. Should be replaced with something more secure than the default. </summary>
		public Func<string, string> Hash = DefaultHash;

		/// <summary> Initializer for a newly logged in user </summary>
		public Action<Guid> initializer;

		public override void OnEnable() {
			loginsByClient = new Dictionary<Client, Session>();
			loginsByUserId = new Dictionary<Guid, Session>();
		}

		public override void OnDisable() {
			loginsByClient.Clear();
			loginsByUserId.Clear();
		}

		public override void OnConnected(Client client) {
			
		}

		public override void OnDisconnected(Client client) {
			if (loginsByClient.ContainsKey(client)) {
				Guid guid = loginsByClient[client].credentials.userId;
				
				loginsByClient.Remove(client);
				loginsByUserId.Remove(guid);
			}
			
		}

		private int _isAttemptingLogin;
		public bool isAttemptingLogin { get { return _isAttemptingLogin != 0; } }
		private string loginName;
		/// <summary> Begins a login for the given username/password pair </summary>
		/// <param name="user"> Username </param>
		/// <param name="pass"> Password </param>
		/// <returns> True, if the login is propagated to the server, false otherwise. (Login already in progress, already logged in, or called on a server instance) </returns>
		public bool Login_Slave(string user, string pass) {
			if (!isSlave) { return false; }
			if (localLogin != null) { return false; }
			if (isAttemptingLogin) { return false; }
			if (Interlocked.CompareExchange(ref _isAttemptingLogin, 1, 0) != 0) { return false; }
			
			loginName = pass;
			string hashedPass = Hash(pass);

			server.localClient.Call(Login, user, hashedPass, VersionInfo.VERSION);
			return true;
		}

		/// <summary> Server -> Client RPC. Response with results of login attempt. </summary>
		/// <param name="msg"> RPC Info. </param>
		public void LoginResponse(RPCMessage msg) {
			_isAttemptingLogin = 0;
			Log.Info($"LoginResponse: {msg[0]}, [{msg[1]}]");
			if (msg[0] == "succ") {
				localLogin = new Credentials(loginName, msg[1]);
				server.On(new LoginSuccess_Client() { credentials = localLogin } );
			} else {
				server.On(new LoginFailure_Client() { reason = msg[1] });
			}	
		}

		/// <summary> Message type sent on a client when a login attempt was successful </summary>
		public struct LoginSuccess_Client { public Credentials credentials; }
		/// <summary> Message type sent on a client when a login attempt was failed. </summary>
		public struct LoginFailure_Client { public string reason; }
			

#if !UNITY
		string _VERSION_MISMATCH = null;
		string VERSION_MISMATCH {
			get {
				if (_VERSION_MISMATCH != null) { return _VERSION_MISMATCH; }
				return (_VERSION_MISMATCH = $"Version Mismatch\nPlease update to version [{versionCode}]");
			}
		}


		/// <summary> Connected database service </summary>
		DBService dbService { get { return GetService<DBService>(); } }
		/// <summary> Client -> Server RPC. Checks user and credentials to validate login, responds with <see cref="LoginResponse(RPCMessage)"/></summary>
		/// <param name="msg"> RPC Info. </param>
		public void Login(RPCMessage msg) {
			string user = msg[0];
			string hash = msg[1];
			string version = msg.numArgs >= 3 ? msg[2] : "[[Version Not Set]]";
			// Login flow.
			Credentials creds = null;
			string reason = "none";
			
			UserInfo userInfo = null; 
			if (version != versionCode) {
				Log.Debug($"{nameof(LoginService)}: Version mismatch {version}, expected {versionCode}");
				reason = VERSION_MISMATCH;
			} else if (!usernameValidator(user)) {
				Log.Debug($"{nameof(LoginService)}: Bad username {user}");
				reason = "Invalid Username";
			} else if (GetLogin(msg.sender) != null) {
				Log.Debug($"{nameof(LoginService)}: Client {msg.sender.identity} already logged in");
				reason = "Already Logged In";
			} else {
				userInfo = dbService.Get<UserInfo>(nameof(userInfo.userName), user);

				if (userInfo == null) {
					Log.Debug($"{nameof(LoginService)}: User {user} not found, creating them now. ");

					Guid userId = Guid.NewGuid();
					userInfo = new UserInfo();
					userInfo.userName = user;
					userInfo.hash = hash;
					userInfo.guid = userId;
					userInfo.lastLogin = DateTime.UtcNow;
					dbService.Save(userInfo);

					try {
						initializer?.Invoke(userId);
						
					} catch (Exception e) {
						Log.Error($"Error initializing user {user} / {userId} ", e);
					}

					creds = new Credentials(user, hash, userInfo.guid);
					
				} else {
					// Check credentials against existing credentials.
					if (loginsByUserId.ContainsKey(userInfo.guid)) {
						reason = "Already logged in";
					} else if (hash != userInfo.hash) {
						reason = "Bad credentials";
					} else {
						creds = new Credentials(user, hash, userInfo.guid);
					}
					

				}
			}
			
			if (creds == null) {
				msg.sender.Call(LoginResponse, "fail", reason);
			} else {
				var session = new Session(msg.sender, creds);
				loginsByClient[msg.sender] = session;
				loginsByUserId[creds.userId] = session;

				userInfo.lastLogin = DateTime.UtcNow;
				dbService.Save(userInfo);

				msg.sender.Call(LoginResponse, "succ", creds.userId);

				Log.Info($"Client {msg.sender.identity} logged in as user {creds.username} / {creds.userId}. ");

				server.On(new LoginSuccess_Server(msg.sender));
			}
		}
		public struct LoginSuccess_Server {
			public readonly Client client;
			public LoginSuccess_Server(Client client) { this.client = client; }
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
