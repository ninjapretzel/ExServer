using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core {

	public delegate byte[] Crypt(byte[] source);

	/// <summary> Primary class for ExServer </summary>
	public class Server {
		
		/// <summary> Null instance static constructor for servers. Used when stuff is running on a 'local' client. </summary>
		public static Server NullInstance { get { return new Server(-1); } }
		/// <summary> Helper function to create a thread with priority </summary>
		/// <param name="start"> Insertion point </param>
		/// <param name="priority"> Priority of thread </param>
		/// <returns> Created and started thread. </returns>
		private static Thread StartThread(ThreadStart start, ThreadPriority priority = ThreadPriority.Normal) {
			Thread t = new Thread(start);
			t.Priority = priority;
			t.Start();
			return t;
		}

		/// <summary> Update thread. </summary>
		private Thread globalUpdateThread;
		/// <summary> Thread for listening for new connections. </summary>
		private Thread listenThread;
		/// <summary> Thread for sending information. </summary>
		private Thread mainSendThread;
		/// <summary> Thread for recieving information. </summary>
		private Thread mainRecrThread;

		/// <summary> Collection of services keyed by type. </summary>
		public Dictionary<Type, Service> services { get; private set; }
		/// <summary> Collection of services keyed by name.  </summary>
		public Dictionary<string, Service> servicesByName { get; private set; }
		/// <summary> Connections, keyed by client ID </summary>
		public Dictionary<Guid, Client> connections { get; private set; }
		/// <summary> Cache of message delegates </summary>
		public Dictionary<string, Message.Handler> rpcCache { get; private set; }

		/// <summary> Queue used to hold all clients for checking for sending data </summary>
		public ConcurrentQueue<Client> sendCheckQueue;
		/// <summary> Queue used to hold all clients for checking for recieving data </summary>
		public ConcurrentQueue<Client> recrCheckQueue;

		/// <summary> Incoming messages. </summary>
		public ConcurrentQueue<Message> incoming;

		/// <summary> Command runner </summary>
		// public Cmdr commander { get; private set; }

		/// <summary> Server port. Negative means 'slave server' or client-sided. </summary>
		public int port { get; private set; }
		/// <summary> Update ticks per second </summary>
		public float tick { get; private set; }
		/// <summary> Milliseconds per tick </summary>
		public float tickRate { get; private set; }

		/// <summary> Slave server. Port is negative. </summary>
		public bool isSlave { get { return port < 0; } }
		/// <summary> Master server. Port is non-negative. </summary>
		public bool isMaster { get { return port >= 0; } }

		/// <summary> Is the server currently running? </summary>
		public bool Running { get; private set; }
		/// <summary> Is the server attempting to stop running? </summary>
		public bool Stopping { get; private set; }

		/// <summary> TCP listener </summary>
		private TcpListener listener;

		/// <summary> Hidden reference to local client for slave server </summary>
		private Client _localClient;
		/// <summary> Returns local client object, if this is a slave server. </summary>
		public Client localClient { get { return isSlave ? _localClient : null; } }
		
		/// <summary> Creates a Server with the given port and tickrate. </summary>
		public Server(int port = 32055, float tick = 100) {
			sendCheckQueue = new ConcurrentQueue<Client>();
			recrCheckQueue = new ConcurrentQueue<Client>();
			incoming = new ConcurrentQueue<Message>();

			servicesByName = new Dictionary<string, Service>();
			services = new Dictionary<Type, Service>();
			rpcCache = new Dictionary<string, Message.Handler>();

			connections = new Dictionary<Guid, Client>();
			//commander = new Cmdr();
			this.port = port;

			this.tick = tick;
			tickRate = 1000.0f / tick;
			
			Stopping = false;
			Running = false;

			AddService<CoreService>();
		}

		public void Start() {
			
			Running = true;
			if (isMaster) {
				listenThread = StartThread(Listen);
			}

			globalUpdateThread = StartThread(Update);
			mainSendThread = StartThread(SendLoop);
			mainRecrThread = StartThread(RecrLoop);
		}

		public void Stop() {
			if (!Running || Stopping) { return; }
			// Set flags and push to RAM.
			Running = false;
			Stopping = true;
			Thread.MemoryBarrier();

			// Wait for threads to finish their work
			listener?.Stop();
			globalUpdateThread?.Join();
			listenThread?.Join();
			mainSendThread?.Join();
			mainRecrThread?.Join();

			// Clean up on slave: just tell the server you closed.
			if (isSlave) {
				localClient.Call(Members<CoreService>.i.Closed);
				SendData(localClient);
				Close(localClient);
			} else {
				// @Todo: Cleanup master by telling all clients server is closed

			}
			
			Stopping = false;
			Thread.MemoryBarrier();

		}

		/// <summary> Hooks up details of client so server can handle communication.
		/// Exposed to allow slave clients to explicitly connect to their server. </summary>
		/// <param name="client"> Slave client to connect. </param>
		public void OnConnected(Client client) {
			if (isMaster) {
				connections[client.id] = client;
			} else {
				_localClient = client;
			}

			foreach (var service in services.Values) {
				service.OnConnected(client);
			}

			sendCheckQueue.Enqueue(client);
			recrCheckQueue.Enqueue(client);
		}

		/// <summary> Removes client from being tracked by the server. 
		/// Exposed to allow slave clients to explicitly disconnect from their server. </summary>
		/// <param name="client"> Slave client to connect. </param>
		public void Close(Client client) {
			connections.Remove(client.id);

			foreach (var service in services.Values) {
				service.OnDisconnected(client);
			}
			client.Close();
		}

		private void Listen() {
			while (Running) {
				try {
					listener = new TcpListener(IPAddress.Any, port);
					listener.Start();
					Log.Info("\\eListening for clients...");
					
					while (true) {
						TcpClient tcpClient = listener.AcceptTcpClient();
						Client client = new Client(tcpClient, this);
						OnConnected(client);
					}
					
				}
				catch (Exception e) {
					Log.Error(e, "Socket Listener had internal failure. Retrying.");
				}
			}
		}


		private void Update() {
			while (Running) {
				try {

					Message msg;
					while (incoming.TryDequeue(out msg)) {
						if (msg.sender.closed) {
							
						}
						HandleMessage(msg);

					}

				}
				catch (Exception e) {
					Log.Error("Failure in Server.Update", e);
				}
				Thread.Sleep(1);
			}

			Log.Info("Server stopping updates and cleaning up.");
			//Todo: Cleanup work
		}


		/// <summary> Loop for sending data to connected clients. @Todo: Pool this. </summary>
		private void SendLoop() {
			while (Running) {
				try {
					Client c;
					if (sendCheckQueue.TryDequeue(out c)) {
						SendData(c);
						if (!c.closed) { sendCheckQueue.Enqueue(c); }
					}

				} catch (Exception e) {
					Log.Error("Failure in Server.SendLoop", e);
				}
				Thread.Sleep(1);
			}
		}

		/// <summary> Loop for recieving data from connected clients. @Todo: Pool this. </summary>
		private void RecrLoop() {
			while (Running) {
				try {
					Client c;
					if (recrCheckQueue.TryDequeue(out c)) {
						RecieveData(c);
						if (!c.closed) { recrCheckQueue.Enqueue(c); } 
					}
					
				} catch (Exception e) {
					Log.Error("Failure in Server.RecrLoop", e);
				}
				Thread.Sleep(1);
			}
		}

		/// <summary> Sends all pending messages to a client. </summary>
		/// <param name="client"> Client to send data for </param>
		private void SendData(Client client) {
			// Todo: Enable Pokeing client with 0 bytes occasionally.
			// DateTime now = DateTime.UtcNow;
			string msg = null;

			try {
				while (!client.closed && client.outgoing.TryDequeue(out msg)) {
					Log.Info($"Client {client.identity} sending message {msg}");

					msg += Message.EOT;
					byte[] message = msg.ToBytesUTF8();
					message = client.enc(message);
					
					client.stream.Write(message, 0, message.Length);
				}
			} catch (ObjectDisposedException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (SocketException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (IOException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (InvalidOperationException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (Exception e) {
				Log.Warning("Server.SendData(Client): ", e);
			}


		}

		/// <summary> Attempts to read data from a client once </summary>
		/// <param name="client"> Client information to read data for </param>
		private void RecieveData(Client client) {

			try {
				
				client.bytesRead = !client.closed && client.stream.CanRead && client.stream.DataAvailable
					? client.stream.Read(client.buffer, 0, client.buffer.Length)
					: -1;

				if (client.bytesRead > 0) {
					client.message = client.buffer.Chop(client.bytesRead);
					client.message = client.dec(client.message);
					string str = client.message.GetStringUTF8();

					client.held += str;
					int index = client.held.IndexOf(Message.EOT);
					while (index >= 0) {
						string pulled = client.held.Substring(0, index);
						client.held = client.held.Remove(0, index+1);
						index = client.held.IndexOf(Message.EOT);

						if (pulled.Length > 0) {
							Message msg = new Message(client, pulled);
							incoming.Enqueue(msg);
						}
					}


				}


			} catch (ObjectDisposedException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (SocketException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (IOException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (InvalidOperationException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (Exception e) {
				Log.Warning($"Server.RecieveData(Client): ", e);
			}
		}

		private void HandleMessage(Message msg) {
			Message.Handler handler = GetHandler(msg);
			
			try {
				handler?.Invoke(msg.sender, msg);
			} catch (Exception e) {
				Log.Warning($"Error occurred during {msg.rpcName}: ", e);
			}
			
		}

		private static Type[] SIGNATURE_OF_MESSAGEHANDLER = new Type[] { typeof(Client), typeof(Message) };

		private void LoadCache(Service service) {
			Type t = service.GetType();
			var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var method in methods) {
				
				if (MatchesSig(method, SIGNATURE_OF_MESSAGEHANDLER)) {
					var handler = (Message.Handler)method.CreateDelegate(typeof(Message.Handler), service);
					var rpcName = t.ShortName() + "." + method.Name;
					rpcCache[rpcName] = handler;
					// Log.Verbose($"Loaded RPC {rpcName}");
				}

			}
		}
		private static bool MatchesSig(MethodInfo method, Type[] signature) {
			var param = method.GetParameters();
			if (param.Length != signature.Length) { return false; }
			for (int i = 0; i < signature.Length; i++) {
				if (signature[i] != param[i].ParameterType) { return false; }
			}
			return true;
		}

		private Message.Handler GetHandler(Message msg) {
			string rpcName = msg.rpcName;
			if (rpcCache.ContainsKey(rpcName)) {
				return rpcCache[rpcName];
			}

			if (!servicesByName.ContainsKey(msg.serviceName)) {
				Log.Warning($"No service for name [{msg.serviceName}] is registered.");
				return null;
			}

			Service service = servicesByName[msg.serviceName];
			Type type = service.GetType();
			MethodInfo method = type.GetMethod(msg.methodName);

			if (method != null) {
				var handler = (Message.Handler)method.CreateDelegate(typeof(Message.Handler), service);
				rpcCache[rpcName] = handler;

			}
			
			Log.Warning($"No method [{msg.rpcName}] found.");
			return null;
		}

		#region SERVICES

		/// <summary> MethodInfo pointing to setter on the <see cref="Service.server"/> propert </summary>
		private static MethodInfo SET_OWNER_METHODINFO = typeof(Service).GetProperty("server", typeof(Server)).GetSetMethod(true);
		/// <summary> Cached object array for MethodInfo invocation without extra garbage collection. </summary>
		private object[] SET_OWNER_ARGS;
		/// <summary> Adds service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to add. </typeparam>
		/// <returns> Service that was added. </returns>
		/// <exception cref="Exception"> if any service with conflicting type or name exists. </exception>
		public T AddService<T>() where T : Service {
			if (SET_OWNER_ARGS == null) { SET_OWNER_ARGS = new object[] { this }; }
			Type type = typeof(T);
			string typeName = type.ShortName();
			if (services.ContainsKey(type)) {
				throw new Exception($"ExServer: Attempt made to add duplicate service {type}.");
			}
			if (servicesByName.ContainsKey(typeName)) {
				throw new Exception($"ExServer: Attempt made to add a service with a duplicate name [{typeName}] by {type}.");
			}

			T service = Activator.CreateInstance<T>();
			SET_OWNER_METHODINFO.Invoke(service, SET_OWNER_ARGS);
			services[type] = service;
			servicesByName[typeName] = service;
			service.OnEnable();
			Log.Verbose($"Enabled Service with type=[{type}] name=[{typeName}]");
			LoadCache(service);

			return service;
		}

		/// <summary> Removes service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to remove </typeparam>
		/// <returns> True if removed, false otherwise. </returns>
		public bool RemoveService<T>() where T : Service {
			if (services.ContainsKey(typeof(T))) {
				Service removed = services[typeof(T)];
				removed.OnDisable();

				Type type = typeof(T);
				servicesByName.Remove(type.ShortName());
				services.Remove(type);

				return true;
			}
			return false;
		}

		/// <summary> Gets a service with type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Type of service to get </typeparam>
		/// <returns> Service of type <typeparamref name="T"/> if present, otherwise null. </returns>
		public T GetService<T>() where T : Service {
			if (services.ContainsKey(typeof(T))) { return (T)services[typeof(T)]; }
			return null;
		}
		#endregion


	}
	public static class ServerUtils {

		public static string ShortName(this Type t) {
			string name = t.Name;
			if (name.Contains('.')) {
				return name.Substring(name.LastIndexOf('.'));
			}
			return name;
		}

		/// <summary> Take a copy of a sub-region of a byte[] </summary>
		/// <param name="array"> byte[] to chop </param>
		/// <param name="size"> maximum size of resulting sub-array </param>
		/// <param name="start"> start index </param>
		/// <returns> sub-region from given byte[], of max length <paramref name="size"/> starting from index <paramref name="start"/> in the original <paramref name="array"/>. </returns>
		public static byte[] Chop(this byte[] array, int size, int start = 0) {
			if (start >= array.Length) { return null; }
			if (size + start > array.Length) {
				size = array.Length - start;
			}
			byte[] chopped = new byte[size];
			for (int i = 0; i < size; i++) {
				chopped[i] = array[i + start];
			}
			return chopped;
		}
		static Encoding utf8 = Encoding.UTF8;
		static Encoding ascii = Encoding.ASCII;
		/// <summary> Turns a string into a byte[] using ASCII </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by ASCII encoding </returns>
		public static byte[] ToBytes(this string s) { return ascii.GetBytes(s); }
		/// <summary> Turns a string into a byte[] using UTF8 </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by UTF8 encoding </returns>
		public static byte[] ToBytesUTF8(this string s) { return utf8.GetBytes(s); }

		/// <summary> Reads a byte[] as if it is an ASCII encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> ASCII string created from the byte[] </returns>
		public static string GetString(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return ascii.GetString(b, 0, length);
		}

		/// <summary> Reads a byte[] as if it is a UTF8 encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> UTF8 string created from the byte[] </returns>
		public static string GetStringUTF8(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return utf8.GetString(b, 0, length);
		}
	}
}
