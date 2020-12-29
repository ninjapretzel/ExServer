#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using BakaDB;
#if !UNITY
using Isopoh.Cryptography.Argon2;
using MiniHttp;
#endif
namespace Ex {

	/// <summary> Represents information about a logged in user. </summary>
	public class Credentials {
		/// <summary> Name of user </summary>
		public string username { get; private set; }
		/// <summary> Is this a local credentials, or server credentials? </summary>
		public bool isLocal { get; private set; }
		/// <summary> User ID, equal to guid value of token if valid, otherwise <see cref="Guid.Empty"/> </summary>
		public Guid userId { get; private set; }
		/// <summary> Token provided by server, typically a valid <see cref="Jwt"/> encoded string</summary>
		public string token { get; private set; }
		/// <summary> Timestamp of creation </summary>
		public DateTime created { get; private set; }

		/// <summary> Used to create a credentials object on the client. </summary>
		/// <param name="user"> Username </param>
		/// <param name="token"> Guid session token as a string </param>
		public Credentials(string user, string token, string userId) {
			username = user;
			isLocal = true;
			Guid guid;
			this.token = token;
			if (!Guid.TryParse(userId, out guid)) { guid = Guid.Empty; }
			this.userId = guid;
			created = DateTime.UtcNow;
		}
		/// <summary> Used to create a credentials object on the server, after authenticating the user. </summary>
		/// <param name="user"> Username </param>
		/// <param name="hash"> Password hash </param>
		public Credentials(string user, string token, Guid userId) {
			username = user;
			this.token = token;
			isLocal = false;
			this.userId = userId;
			created = DateTime.UtcNow;
		}

	}
	/// <summary> Delegate for hashing passwords </summary>
	/// <param name="password"> Password to hash </param>
	/// <returns> Hashed password </returns>
	public delegate string HashFn(string password);
	/// <summary> Delegate for verifying passwords </summary>
	/// <param name="hash"> Hash to test </param>
	/// <param name="password"> Password to test </param>
	/// <returns> True if they match, false otherwise </returns>
	public delegate bool VerifyFn(string hash, string password);
	/// <summary> Delegate for packing data into a JWT </summary>
	/// <typeparam name="T"> Generic type to pack into the JWT </typeparam>
	/// <param name="data"> Data object to pack into the JWT </param>
	/// <param name="secret"> Secret password to use </param>
	/// <param name="expiry"> Time in seconds until token expires </param>
	/// <returns> Encoded JWT </returns>
	public delegate string EncodeJWTFn<T>(T data, string secret, int? expiry = null);
	/// <summary> Delegate for unpacking data from a JWT </summary>
	/// <typeparam name="T"> Generic type to unpack from JWT </typeparam>
	/// <param name="token"> Encoded JWT to unpack </param>
	/// <param name="result"> Output location </param>
	/// <param name="secret"> Secret password to use </param>
	/// <returns> true if JWT is valid, false otherwise.  </returns>
	public delegate bool DecodeJWTFn<T>(string token, out T result, string secret);
	/// <summary> Delegate for encrypting </summary>
	/// <param name="payload"> Payload to encrypt </param>
	/// <returns> Encrypted string </returns>
	public delegate string EncryptFn(string payload);
	/// <summary> Delegate for decrypting </summary>
	/// <param name="encrypted"> Encrypted payload to decrypt </param>
	/// <returns> Decrypted string or null if Decryption fails </returns>
	public delegate string DecryptFn(string encrypted);

	public class LoginService : Service {

		#region MESSAGE_STRUCTS
		/// <summary> Message type sent on a client when a login attempt was successful </summary>
		public struct LoginSuccess_Client { public Credentials credentials; }
		/// <summary> Message type sent on a client when a login attempt was failed. </summary>
		public struct LoginFailure_Client { public string reason; }

		/// <summary> Message type sent on server when a login attempt was successful. </summary>
		public struct LoginSuccess_Server {
			public readonly Client client;
			public string username;
			public LoginSuccess_Server(string username) { this.username = username; client = null; }
			public LoginSuccess_Server(string username, Client client) { this.username = username; this.client = client; }
		}
		/// <summary> Message type sent on server when a login attempt was failed</summary>
		public struct LoginFailure_Server {
			public string ip;
			public string reason;
		}
		#endregion

#if !UNITY
		/// <summary> Database object storing user login info </summary>
		public class UserLoginInfo {
			/// <summary> Username </summary>
			public string userName;
			/// <summary> password hash </summary>
			public string hash;
			/// <summary> Unique identifier for </summary>
			public Guid guid;
			/// <summary> last login </summary>
			public DateTime lastLogin;
		}

		/// <summary> Database object storing user account creation info </summary>
		public class UserAccountCreation {
			/// <summary> Account name </summary>
			public string userName;
			/// <summary> IP Address </summary>
			public string ipAddress;
			/// <summary> Time of account creation </summary>
			public DateTime time;
		}

		/// <summary> Results of a login attempt </summary>
		public enum LoginResult : int {
			/// <summary> Login did not exist, was created, and user was logged in. </summary>
			Success_Created,
			/// <summary> Login did exist, and user was logged in </summary>
			Success,
			
			/// <summary> Login was rejected due to unspecified reasons </summary>
			Failed_Unspecified = 10000,
			/// <summary> Login was rejected due to version mismatch </summary>
			Failed_VersionMismatch,
			/// <summary> Login was rejected due to password mismatch </summary>
			Failed_BadCredentials,
			/// <summary> Login was rejected due to login cooldown applied </summary>
			Failed_LoginCooldown,
			/// <summary> Login was rejected due to creation cooldown applied </summary>
			Failed_CreationCooldown,
			/// <summary> Login was rejected due to username not valid </summary>
			Failed_BadUsername,
			/// <summary> Login was rejected due to client already being logged in </summary>
			Failed_ClientAlreadyLoggedIn,
			/// <summary> Login was rejected due to requested user already being logged in </summary>
			Failed_UserAlreadyLoggedIn,
		}

		/// <summary> Database object storing login attempts. </summary>
		public class LoginAttempt {
			/// <summary> Username attempted for login </summary>
			public string userName;
			/// <summary> Passhash provided for login </summary>
			public string hash;
			/// <summary> Timestamp of login attempt </summary>
			public DateTime timestamp;
			/// <summary> Was the login a success? </summary>
			public bool success;
			/// <summary> Was the login an account creation? (implies success) </summary>
			public bool creation;
			/// <summary> IP of client requesting login </summary>
			public string ip;
			/// <summary> Enum result of login </summary>
			public LoginResult result;
			/// <summary> Descriptive result of login </summary>
			public string result_desc;
		}
		/// <summary> information about a logged in client </summary>
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

#endif
		string _VERSION_MISMATCH = null;
		string VERSION_MISMATCH {
			get {
				if (_VERSION_MISMATCH != null) { return _VERSION_MISMATCH; }
				return (_VERSION_MISMATCH = $"Version Mismatch\nPlease update to version [{versionCode}]");
			}
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
		
		/// <summary> Local keypair for encryption usages. </summary>
		public Pgp.KeyPair kp = Pgp.GenerateKey();
		/// <summary> Server's public key, or null if not yet sent. </summary>
		public string serverPublic = null;
		/// <summary> Current function to encrypt sensitive information with </summary>
		public EncryptFn EncryptPassword;
		/// <summary> Current function to decrypt sensitive information with </summary>
		public DecryptFn DecryptPassword;
		/// <summary> Default function to encrypt sensitive information with </summary>
		public string DefaultEncrypt(string pass) { return Pgp.Encrypt(pass, serverPublic); }
		/// <summary> Default function to decrypt sensitive information with </summary>
		public string DefaultDecrypt(string encoded) { return Pgp.Decrypt(encoded, kp); }
#if !UNITY
		/// <summary> Hash function. Defaults to <see cref="Isopoh.Cryptography.Argon2.Argon2.Hash"/> with a default configuration </summary>
		public HashFn Hash = DefaultHash;
		/// <summary> Hash verification function. Defaults to <see cref="Isopoh.Cryptography.Argon2.Argon2.Verify(string, Argon2Config)"/> with a default configuration </summary>
		public VerifyFn Verify = DefaultVerify;
		/// <summary> JWT Encode function. Defaults to <see cref="Jwt.Encode{T}(T, string, int?)"/>. </summary>
		public EncodeJWTFn<JsonObject> EncodeJWT = Jwt.Encode;
		/// <summary> JWT Decode function. Defaults to <see cref="Jwt.Decode{T}(string, out T, string)/>"/>. </summary>
		public DecodeJWTFn<JsonObject> DecodeJWT= Jwt.Decode;
		
		/// <summary> Initializer for a newly logged in user </summary>
		public Action<Guid> userInitializer;

		public static LocalDB<UserLoginInfo> userDB = DB.Of<UserLoginInfo>.db;
		public static LocalDB<LoginAttempt> loginAttemptDB = DB.Of<LoginAttempt>.db;
		public static LocalDB<List<UserAccountCreation>> accountCreationDB = DB.Of<List<UserAccountCreation>>.db;

		public override void OnStart() {
			
			
		}

		public override void OnEnable() {
			loginsByClient = new Dictionary<Client, Session>();
			loginsByUserId = new Dictionary<Guid, Session>();
			EncryptPassword = DefaultEncrypt;
			DecryptPassword = DefaultDecrypt;

		}

		public override void OnDisable() {
			loginsByClient.Clear();
			loginsByUserId.Clear();
		}

		public override void OnConnected(Client client) {
			if (isMaster) {
				client.Call(SetServerPublicKey, kp.publicKey);
			}
			
		}
		
		public override void OnFinishedDisconnected(Client client) {
			if (loginsByClient.ContainsKey(client)) {
				Log.Verbose($"Logging out {client.identity}");
				Guid guid = loginsByClient[client].credentials.userId;
				
				loginsByClient.Remove(client);
				loginsByUserId.Remove(guid);
			}
			
		}
#else
		public override void OnEnable() {
			EncryptPassword = DefaultEncrypt;
			DecryptPassword = DefaultDecrypt;
		}
#endif

		private int _isAttemptingLogin;
		public bool isAttemptingLogin { get { return _isAttemptingLogin != 0; } }
		private string loginName;
		/// <summary> Begins a login for the given username/password pair </summary>
		/// <param name="user"> Username </param>
		/// <param name="pass"> Password </param>
		/// <returns> True, if the login is propagated to the server, false otherwise. (Login already in progress, already logged in, or called on a server instance) </returns>
		public bool RequestLogin(string user, string pass) {
			if (!isSlave) { return false; }

			if (localLogin != null) { return false; }
			if (isAttemptingLogin) { return false; }
			if (Interlocked.CompareExchange(ref _isAttemptingLogin, 1, 0) != 0) { return false; }
			
			loginName = user;
			server.localClient.Call(Login, user, EncryptPassword(pass), VersionInfo.VERSION);
			return true;
		}

		/// <summary> Server -> Client RPC. Response with results of login attempt. </summary>
		/// <param name="msg"> RPC Info. </param>
		public void LoginResponse(RPCMessage msg) {
			if (!isMaster) {
				Interlocked.Exchange(ref _isAttemptingLogin, 0);
				Log.Info($"LoginResponse: {msg[0]}, [{msg[1]}] / [{msg[2]}] / [{msg[3]}]");
				if (msg[0] == "succ" && msg[1] == loginName) {
					localLogin = new Credentials(loginName, msg[2], msg[3]);
					server.On(new LoginSuccess_Client() { credentials = localLogin } );
				} else {
					if (msg[1] != loginName) {
						server.On(new LoginFailure_Client() { reason = $"Server responded with wrong name. Expected {loginName} got {msg[1]}." });
					} else {
						server.On(new LoginFailure_Client() { reason = msg[1] });
					}
				}
			}	
		}

		/// <summary> Server -> Client RPC. Sets <see cref="serverPublic"/> with the given parameter. </summary>
		/// <param name="msg"> RPC Info. </param>
		public void SetServerPublicKey(RPCMessage msg) {
			if (isSlave) {
				serverPublic = msg[0];
				Log.Info($"Got server public key {serverPublic}");
			}
		}

		/// <summary> Client -> Server RPC. Checks user and credentials to validate login, responds with <see cref="LoginResponse(RPCMessage)"/></summary>
		/// <param name="msg"> RPC Info. </param>
		public void Login(RPCMessage msg) {
#if !UNITY
			string user = msg[0];
			string pass = DecryptPassword(msg[1]);
			string version = msg.numArgs >= 3 ? msg[2] : "[[Version Not Set]]";

			LoginOutcome outcome; 
			// Login flow.
			if (GetLogin(msg.sender) != null) {
				Log.Debug($"{nameof(LoginService)}: Client {msg.sender.identity} already logged in");
				outcome.creds = null;
				outcome.userInfo = null;
				outcome.reason = "Already Logged In";
				outcome.result = LoginResult.Failed_ClientAlreadyLoggedIn;
			} else {
				outcome = Login(user, pass, msg.sender.remoteIP, version);
			}
			Credentials creds = outcome.creds;
			if (creds == null) {
				string reason = outcome.reason;
				msg.sender.Call(LoginResponse, "fail", reason);

				Log.Info($"Client {msg.sender.identity} Failed to login.");

				server.On(new LoginFailure_Server() {
					ip = msg.sender.remoteIP,
					reason = reason
				});

			} else {
				var session = new Session(msg.sender, creds);
				loginsByClient[msg.sender] = session;
				loginsByUserId[creds.userId] = session;

				msg.sender.Call(LoginResponse, "succ", creds.username, creds.token, creds.userId);

				Log.Info($"Client {msg.sender.identity} logged in as user {creds.username} / {creds.userId}. ");

				server.On(new LoginSuccess_Server(user, msg.sender));
			}

#endif
		}

#if !UNITY
		/// <summary> Class holding result from <see cref="Login(string, string, string, string)"/> </summary>
		private struct LoginOutcome {
			/// <summary> <see cref="LoginResult"/> enum value </summary>
			public LoginResult result;
			/// <summary> Description of <see cref="result"/> or other information about login rejection </summary>
			public string reason;
			/// <summary> Resulting user credentials from successful login, or null if unsuccessful </summary>
			public Credentials creds;
			/// <summary> Resulting DB record from successful login, or null if unsuccessful </summary>
			public UserLoginInfo userInfo;
		}

		/// <summary> Function holding client Login validation and recording</summary>
		/// <param name="user"> Username to login as </param>
		/// <param name="pass"> password provided by client </param>
		/// <param name="remoteIP"> IP of client </param>
		/// <param name="version"> Version of Client</param>
		/// <returns> <see cref="LoginOutcome"/> containing result information </returns>
		private LoginOutcome Login(string user, string pass, string remoteIP, string version = null) {
			Credentials creds = null;
			LoginResult result = LoginResult.Failed_Unspecified;
			string reason = "none";
			UserLoginInfo userInfo = userDB.Get(user);

			if (version != versionCode) {
				Log.Debug($"{nameof(LoginService)}: Version mismatch {version}, expected {versionCode}");
				reason = VERSION_MISMATCH;
				result = LoginResult.Failed_VersionMismatch;
			} else if (!usernameValidator(user)) {
				Log.Debug($"{nameof(LoginService)}: Bad username {user}");
				reason = "Invalid Username";
				result = LoginResult.Failed_BadUsername;
			} else if (userInfo == null) {
				Log.Debug($"{nameof(LoginService)}: User {user} not found, creating them now. ");

				userInfo = CreateNewUser(user, pass, remoteIP);

				if (userInfo != null) {
					result = LoginResult.Success_Created;
					string token = EncodeJWT(new JsonObject("user", user), "Reee", 60 * 60 * 24);
					creds = new Credentials(user, token, userInfo.guid);
				} else {
					result = LoginResult.Failed_CreationCooldown;
					reason = "Too many account creations";
				}
			} else {
				// Check credentials against existing credentials.
				if (loginsByUserId.ContainsKey(userInfo.guid)) {
					reason = "Already logged in";
					result = LoginResult.Failed_UserAlreadyLoggedIn;
				} else if (!Verify(userInfo.hash, pass)) {
					reason = "Bad credentials";
					result = LoginResult.Failed_BadCredentials;
				} else { // normal existing user login
					string token = EncodeJWT(new JsonObject("user", user), "Reee", 60 * 60 * 24);
					creds = new Credentials(user, token, userInfo.guid);
					result = LoginResult.Success;
				}
			}

			LoginAttempt attempt = new LoginAttempt();
			

			attempt.result = result;
			attempt.result_desc = result.ToString();
			attempt.timestamp = DateTime.UtcNow;
			string hash = Hash(pass);
			attempt.hash = hash;
			attempt.userName = user;
			attempt.ip = remoteIP;
			if (userInfo != null) {
				attempt.success = true;
				attempt.creation = result == LoginResult.Success_Created;
			} else {
				attempt.success = attempt.creation = false;
			}

			loginAttemptDB.Save($"{attempt.timestamp.UnixTimestamp()}-{attempt.userName}", attempt);

			LoginOutcome outcome;
			outcome.creds = creds;
			outcome.result = result;
			outcome.reason = reason; ;
			outcome.userInfo = userInfo;

			return outcome;
		}

		/// <summary> Function holding user creation and initialization logic </summary>
		/// <param name="user"> Username to initialize </param>
		/// <param name="pass"> Password provided for creation </param>
		/// <param name="remoteIP"> IP of client </param>
		/// <returns> Created <see cref="UserLoginInfo"/> record </returns>
		private UserLoginInfo CreateNewUser(string user, string pass, string remoteIP) {
			var file = $"{remoteIP}.wtf";
			// List<UserAccountCreation> accountCreations = dbService.GetAll<UserAccountCreation>(nameof(UserAccountCreation.ipAddress), remoteIP);
			List<UserAccountCreation> accountCreations = accountCreationDB.Open(file);

			Log.Info($"Client at {remoteIP} has {accountCreations.Count} account creations.");
			if (accountCreations.Count > 5) {
				Log.Info($"Too many account creations!.");
				return null;
			}

			if (userDB.Exists(user)) {
				Log.Info($"Username {user} already taken, can't create a new user!!");
				return null;
			}
			
			Guid userId = Guid.NewGuid();
			UserLoginInfo userInfo = new UserLoginInfo();
			userInfo.userName = user;
			string hash = Hash(pass);
			userInfo.hash = hash;
			userInfo.guid = userId;
			userInfo.lastLogin = DateTime.UtcNow;
			userDB.Save(user, userInfo);

			try {
				if (userInitializer == null) {
					Log.Warning($"LoginService.CreateNewUser: No userInitializer found. Please set a function to set up a new user.");
				}
				Log.Info($"Creating new account for user {user} / {userId}");
				userInitializer?.Invoke(userId);

				accountCreations.Add(new UserAccountCreation() {
					userName = user,
					ipAddress = remoteIP,
					time = DateTime.UtcNow
				});
				DB.Of<List<UserAccountCreation>>.Save(file, accountCreations);


			} catch (Exception e) {
				Log.Error($"Error initializing user {user} / {userId} ", e);
				return null;
			}

			return userInfo;
		}

		/// <summary> Default Password 'Hash' function. </summary>
		/// <param name="pass"> password to hash </param>
		/// <returns> argon2 hash result. </returns>
		/// <remarks> Literally just wraps <see cref="Argon2.Hash(string, string?, int, int, int, Argon2Type, int, Isopoh.Cryptography.SecureArray.SecureArrayCall?)"/> 
		/// so that specific overload is found and used. </remarks>
		public static string DefaultHash(string pass) { return Argon2.Hash(pass); }

		/// <summary> Verifies an argon2 hash. </summary>
		/// <param name="hash"> Hash to check </param>
		/// <param name="pass"> Password to check </param>
		/// <returns> True if password matches hash, false otherwise. </returns>
		/// <remarks> Wraps <see cref="Argon2.Verify(string, string)"/> </remarks>
		public static bool DefaultVerify(string hash, string pass) { return Argon2.Verify(hash, pass); }

		private Router _router;
		public Router router {
			get {
				if (_router == null) { 
					_router = new Router();
					_router.Get("/publicKey", (ctx, next)=> { ctx.body = new JsonObject("publicKey",kp.publicKey); });
					_router.Post("/login", (ctx, next) => {
						JsonObject result = new JsonObject();
						ctx.body = result;
						
						string remoteIP = ctx.RemoteEndPoint.Address.ToString();
						void Fail(string reason) { 
							result["success"] = false; 
							result["reason"] = reason;
							server.On(new LoginFailure_Server() { ip = remoteIP, reason = reason});
						}

						try {
							JsonObject data = ctx.req.bodyObj;
							string user = data.Get<string>("user");
							string pass = data.Get<string>("pass");
							string version = data.Get<string>("version");
							if (user == null || pass == null || version == null) {
								Fail("Missing Information");
								return;
							}
							try { pass = DecryptPassword(pass); }
							catch (Exception) { Fail("Incorrectly encrypted password"); return; }

							LoginOutcome outcome = Login(user, pass, remoteIP, version);
							Credentials creds = outcome.creds;
							if (creds == null) { Fail(outcome.reason); return; }

							UserLoginInfo userInfo = outcome.userInfo;
							userInfo.lastLogin = DateTime.UtcNow;
							userDB.Save(user, userInfo);

							result["success"] = true;
							result["username"] = creds.username;
							result["userId"] = creds.userId.ToString();
							result["token"] = creds.token;
							server.On(new LoginSuccess_Server());

						} catch (Exception e) {
							Fail("Server Error");
							Log.Warning("LoginService.Router/login: Error during login", e);
							ctx.res.StatusCode = 500;
						}

					});
				}
				
				return _router;
			}

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


	}

}
