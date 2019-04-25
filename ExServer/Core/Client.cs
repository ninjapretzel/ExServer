using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ex {

	public class Client {

		#region Constants/Static stuff

		/// <summary> max timeout for stream interaction </summary>
		public const int DEFAULT_READWRITE_TIMEOUT = 10 * 1000;

		/// <summary> Const array used to send an empty message to 'poke' the connection. </summary>
		public static readonly byte[] oneByte = { (byte)RPCMessage.EOT };
		#endregion

		#region Fields and Properties
		/// <summary> Id for client </summary>
		public Guid id { get; private set; }
		/// <summary> Connection to client </summary> 
		public TcpClient connection { get; private set; }
		/// <summary> Underlying stream used to communicate with connected client </summary>
		public NetworkStream stream { get { return connection?.GetStream(); } }
		/// <summary> Server object </summary>
		public Server server { get; private set; }

		/// <summary> Are we running on a slave (local) client? </summary>
		public bool isSlave { get { return server.isSlave; } }
		/// <summary> Are we running on a master (remote) client? </summary>
		public bool isMaster { get { return server.isMaster; } }
		/// <summary> Quick string to identify client </summary>
		public string identity { get { return (isSlave ? "*[LocalClient]*" : ("*[Client#" + id + "]*")); } }

		/// <summary> Remote IP address, if applicable "????" if not. </summary>
		public string remoteIP { get; private set; }
		/// <summary> Remote port, if applicable. -1 if not. </summary>
		public int remotePort { get; private set; }
		/// <summary> Local IP address, if applicable "????" if not. </summary>
		public string localIP { get; private set; }
		/// <summary> Remote port, if applicable. -1 if not. </summary>
		public int localPort { get; private set; }

		/// <summary> Outgoing messages. Preprocessed strings that are sent over the stream. </summary>
		public ConcurrentQueue<string> outgoing;
		
		/// <summary> Can this client expected to be open? </summary>
		/// <remarks> Closed connections do not remain in Server.connections </remarks>
		public bool closed { get; private set; }

		#region subRegion "struct ReadState"
		/// <summary> Holds intermediate message data between reads </summary>
		public StringBuilder held = "";
		/// <summary> Last number of bytes read from stream </summary>
		public int bytesRead = -1;
		/// <summary> Buffer for reading from stream </summary>
		public byte[] buffer;
		/// <summary> Buffer for chopping messages from stream </summary>
		public byte[] message;
		#endregion

		/// <summary> Encryption </summary>
		public Crypt enc = (b) => b;
		/// <summary> Decryption </summary>
		public Crypt dec = (b) => b;

		#endregion

		public Client(TcpClient tcpClient, Server server = null) {
			if (server == null) { server = Server.NullInstance; }
			this.server = server;
			this.id = Guid.NewGuid();
			this.connection = tcpClient;
			var remoteEndPoint = tcpClient.Client.RemoteEndPoint;
			this.remoteIP = (remoteEndPoint is IPEndPoint) ? ((remoteEndPoint as IPEndPoint).Address.ToString()) : "????";
			this.remotePort = (remoteEndPoint is IPEndPoint) ? ((remoteEndPoint as IPEndPoint).Port) : -1;
			var localEndpoint = tcpClient.Client.LocalEndPoint;
			this.localIP = (localEndpoint is IPEndPoint) ? ((localEndpoint as IPEndPoint).Address.ToString()) : "????";
			this.localPort = (localEndpoint is IPEndPoint) ? ((localEndpoint as IPEndPoint).Port) : -1;
			
			this.stream.ReadTimeout = DEFAULT_READWRITE_TIMEOUT;
			this.stream.WriteTimeout = DEFAULT_READWRITE_TIMEOUT;
			
			outgoing = new ConcurrentQueue<string>();
			

			Log.Info($"\\eClient \\y {identity}\\e connected from \\y {localEndpoint} -> {remoteEndPoint}");
			buffer = new byte[4096];
		}

		/// <summary> If a slave, this client connects to the server. </summary>
		public void ConnectSlave() {
			if (isSlave) {
				server.OnConnected(this);
				server.Start();
			}
		}

		/// <summary> If a slave, this client disconnects from the server. </summary>
		public void DisconnectSlave() {
			if (isSlave) {
				server.Stop();
			}
		}

		/// <summary> Sends a message to remotely call a function handler on the client. </summary>
		/// <param name="callback"> Handler to call </param>
		/// <param name="stuff"> parameters to send into call </param>
		public void Call(RPCMessage.Handler callback, params System.Object[] stuff) {
			if (closed) { throw new InvalidOperationException("Cannot send messages on a closed Client"); }
			string msg = FormatCall(callback, stuff);
			outgoing.Enqueue(msg);
		}

		/// <summary> Sends an RPCMessage to the connected client </summary>
		/// <param name="stuff"> Everything to send. Method name first, parameters after. </param>
		/// <remarks> 
		///		ToString's all of the <paramref name="stuff"/>, and then joins it together with <see cref="SEP"/>.
		///		Enqueues the resulting string into <see cref="outgoing"/>. 
		///	</remarks>
		public void Send(params System.Object[] stuff) {
			if (closed) { throw new InvalidOperationException("Cannot send messages on a closed Client"); }
			string msg = FormatMessage(stuff);
			outgoing.Enqueue(msg);
		}

		/// <summary> Formats a message into a string intended to be sent over the network. </summary>
		/// <param name="stuff"> Array of parameters to format. </param>
		/// <returns> String of all objects in <paramref name="stuff"/> formatted to be sent over the network. </returns>
		public static string FormatCall(RPCMessage.Handler callback, params System.Object[] stuff) {
			string methodName = callback.Method.Name;
			string typeName = callback.Method.DeclaringType.ShortName();
			string msg;
			if (stuff.Length > 0) {
				string[] strs = new string[stuff.Length];
				for (int i = 0; i < strs.Length; i++) { strs[i] = stuff[i].ToString(); }
				string rest = String.Join("" + RPCMessage.SEPARATOR, strs);
				msg = String.Join("" + RPCMessage.SEPARATOR, typeName, methodName, rest);
			} else {
				msg = String.Join("" + RPCMessage.SEPARATOR, typeName, methodName);
			}
			return msg;
		}

		/// <summary> Formats a message into a string intended to be sent over the network. </summary>
		/// <param name="stuff"> Array of parameters to format. </param>
		/// <returns> String of all objects in <paramref name="stuff"/> formatted to be sent over the network. </returns>
		public static string FormatMessage(params System.Object[] stuff) {
			string[] strs = new string[stuff.Length];
			for (int i = 0; i < strs.Length; i++) { strs[i] = stuff[i].ToString(); }
			string msg = String.Join("" + RPCMessage.SEPARATOR, strs);
			return msg;
		}

		/// <summary> Enqueues a (hopefully, properly formatted) message directly into the outgoing queue. </summary>
		/// <param name="message"> Message to enqueue. </param>
		public void SendMessageDirectly(string message) {
			if (closed) { throw new InvalidOperationException("Cannot send messages on a closed Client"); }
			outgoing.Enqueue(message);
		}

		/// <summary> Closes the client's connection. </summary>
		public void Close() {
			if (!closed) {
				closed = true;

				try { 
					connection.Close();
					Log.Verbose($"Client {identity} closed.");
				} catch (Exception e) {
					Log.Error("Failed to close connection", e);
				}
			}

		}

		#region Services
		/// <summary> Adds service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to add. </typeparam>
		/// <returns> Service that was added. </returns>
		/// <exception cref="Exception"> if any service with conflicting type or name exists. </exception>
		public T AddService<T>() where T : Service { return server.AddService<T>(); }

		/// <summary> Removes service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to remove </typeparam>
		/// <returns> True if removed, false otherwise. </returns>
		public bool RemoveService<T>() where T : Service { return server.RemoveService<T>(); }
		/// <summary> Gets a service with type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Type of service to get </typeparam>
		/// <returns> Service of type <typeparamref name="T"/> if present, otherwise null. </returns>
		public T GetService<T>() where T : Service { return server.GetService<T>(); }
		#endregion

	}

}
