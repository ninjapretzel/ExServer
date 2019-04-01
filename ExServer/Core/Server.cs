using Core.Libs;
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
		public Dictionary<string, Service.OnMessage> rpcCache { get; private set; }

		/// <summary> Queue used to hold all clients for checking for sending data </summary>
		public ConcurrentQueue<Client> sendCheckQueue;
		/// <summary> Queue used to hold all clients for checking for recieving data </summary>
		public ConcurrentQueue<Client> recrCheckQueue;

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


		/// <summary> Creates a Server with the given port and tickrate. </summary>
		public Server(int port = 32055, float tick = 100) {
			sendCheckQueue = new ConcurrentQueue<Client>();
			recrCheckQueue = new ConcurrentQueue<Client>();

			servicesByName = new Dictionary<string, Service>();
			services = new Dictionary<Type, Service>();
			rpcCache = new Dictionary<string, Service.OnMessage>();

			connections = new Dictionary<Guid, Client>();
			//commander = new Cmdr();
			this.port = port;

			this.tick = tick;
			tickRate = 1000.0f / tick;
			
			Stopping = false;
			Running = false;
		}
		
		public void Stop() {
			if (Stopping) { return; }
			Stopping = true;
			listener?.Stop();
			//ThreadExt.TerminateAll(mainSendThread, mainRecrThread, listenThread, globalUpdateThread);

			// Cleanup work
			List<Client> toClose = new List<Client>();
			foreach (var pair in connections) { toClose.Add(pair.Value); }
			foreach (var client in toClose) { Close(client); }
			


			Running = false;
			Stopping = false;
		}

		public void Start() {
			Running = true;
			globalUpdateThread = StartThread(Update);
			listenThread = StartThread(Listen);
			mainSendThread = StartThread(SendLoop);
			mainRecrThread = StartThread(RecrLoop);
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

						connections[client.id] = client;
						foreach (var pair in services) {
							pair.Value.OnConnected(client);
						}

						sendCheckQueue.Enqueue(client);
						recrCheckQueue.Enqueue(client);
					}
					
				}
				catch (ThreadAbortException) { return; }
				catch (Exception e) {
					Log.Error(e, "Socket listen had internal failure. Retrying.");
				}
			}
		}


		private void Update() {
			while (Running) {
				try {

				}
				catch (ThreadAbortException) { return; }
				catch (Exception e) {

				}
				Thread.Sleep(1);
			}
		}


		private void SendLoop() {
			while (Running) {
				try {
					Client c;
					if (sendCheckQueue.TryDequeue(out c)) {
						SendData(c);
					}

				} 
				catch (ThreadAbortException) { return; } 
				catch (Exception e) {

				}
				Thread.Sleep(1);
			}
		}

		private void RecrLoop() {
			while (Running) {
				try {
					Client c;
					if (recrCheckQueue.TryDequeue(out c)) {
						RecieveData(c);
					}

				} catch (ThreadAbortException) { return; } 
				catch (Exception e) {

				}
				Thread.Sleep(1);
			}
		}

		private void SendData(Client client) {
			DateTime now = DateTime.UtcNow;
			string msg = null;

			try {
				while (client.outgoing.TryDequeue(out msg)) {
					Log.Info($"Client {client.identity} sent message {msg}");

				}
			} catch (IOException e) {
				Log.Info($"\\hServer.SendData(Client):  {client.identity} Probably timed out. {e.GetType()}");
				Close(client);
			} catch (InvalidOperationException e) {
				Log.Info($"\\hServer.SendData(Client):  {client.identity} Probably timed out. {e.GetType()}");
				Close(client);
			} catch (SocketException e) {
				Log.Info($"\\hServer.SendData(Client):  {client.identity} Probably disconnected. {e.GetType()}");
				Close(client);
			} catch (Exception e) {
				Log.Warning("\\rServer.SendData(Client): ");
			}


		}

		private void RecieveData(Client c) {

		}

		private void Close(Client client) {
			connections.Remove(client.id);

			foreach (var pair in services) {
				pair.Value.OnClosed(client);
			}
			client.Close();
		}

		#region SERVICES

		/// <summary> MethodInfo pointing to setter on the <see cref="Service.server"/> propert </summary>
		private static MethodInfo SET_OWNER_METHODINFO;
		/// <summary> Cached object array for MethodInfo invocation without extra garbage collection. </summary>
		private object[] SETOWNER_ARGS;
		/// <summary> Adds service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to add. </typeparam>
		/// <returns> Service that was added. </returns>
		/// <exception cref="Exception"> if any service with conflicting type or name exists. </exception>
		public T AddService<T>() where T : Service {
			if (SET_OWNER_METHODINFO == null) { SET_OWNER_METHODINFO = typeof(Service).GetProperty("server", BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(); }
			if (SETOWNER_ARGS == null) { SETOWNER_ARGS = new object[] { this }; }
			Type type = typeof(T);
			if (services.ContainsKey(type)) {
				throw new Exception($"ExServer: Attempt made to add duplicate service {type}.");
			}
			if (servicesByName.ContainsKey(type.Name)) {
				throw new Exception($"ExServer: Attempt made to add a service with a duplicate name [{type.Name}] by {type}.");
			}

			T service = Activator.CreateInstance<T>();
			SET_OWNER_METHODINFO.Invoke(service, SETOWNER_ARGS);
			services[type] = service;
			servicesByName[type.Name] = service;

			service.OnEnable();

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
				servicesByName.Remove(type.Name);
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


		public class DebugService : Service {

			public override void OnEnable() {
				Log.Verbose("Debug Service Enabled");
			}

			public override void OnDisable() {
				Log.Verbose("Debug Service Disabled");
			}

			public override void OnTick(float delta) {
				Log.Verbose("Debug Service Tick " + delta);
			}

			public void Ping(Client sender, Message message) {
				sender.Send("DebugService", "Pong");
				Log.Verbose($"Ping'd by {sender.identity}");
			}
			public void Pong(Client sender, Message message) {
				sender.Send("DebugService", "Pong");
				Log.Verbose($"Pong'd by {sender.identity}");
			}

		}
	}
}
