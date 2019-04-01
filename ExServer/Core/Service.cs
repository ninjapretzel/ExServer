using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {

	/// <summary> Type used to add custom services to a Server </summary>
	public abstract class Service {

		/// <summary> Owner that this Service belongs to </summary>
		public Server server { get; private set; }
		
		/// <summary> Called when the Service is added to a Servcer </summary>
		public virtual void OnEnable() { }
		/// <summary> Called when the Service is removed from the server </summary>
		public virtual void OnDisable() { }
		/// <summary> Called every server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public virtual void OnTick(float delta) { }

		/// <summary> Called with a client when that client has connected. </summary>
		/// <param name="client"> Client who has connected. </param>
		public virtual void OnConnected(Client client) { }
		/// <summary> Called with a client when that client has disconnected. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public virtual void OnDisconnected(Client client) { } 


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
	}	

	/// <summary> Holds a way to access instance members of a type without needing to explicitly create instances. </summary>
	/// <typeparam name="T">Generic type </typeparam>
	/// <remarks> This is simply a convinience class to access instance OnMessage callbacks. </remarks>
	public sealed class Members<T> {
		/// <summary> Generic instance. Do not expect this instance to be valid to operate on. </summary>
		public static readonly T i = Activator.CreateInstance<T>();
	}

	/// <summary> Service providing network debugging messages when common events occur. </summary>
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


		public override void OnConnected(Client client) {
			Log.Verbose($"Connected {client.identity}");
		}

		public override void OnDisconnected(Client client) {
			Log.Verbose($"Disconnected {client.identity}");
		}

		public void Ping(Client sender, Message message) {
			Log.Verbose($"Ping'd by {sender.identity}");

			// Since we are an instance, we can reference the Pong method directly. 
			sender.Call(Pong);

			// If accessing another service's methods, this will help keep references during refactoring:
			// sender.Call(Members<DebugService>.i.Pong);

		}
		public void Pong(Client sender, Message message) {

			Log.Verbose($"Pong'd by {sender.identity}");

		}

	}

	/// <summary> Service that includes core functionality. Always on a server by default. </summary>
	public class CoreService : Service {

		public override void OnConnected(Client client) {
			Log.Info($"Core Service connected {client.identity}");
			if (server.isSlave) {
				Log.Info($"Slave beginning syn-synack-ack process {client.identity}");
				client.Call(Syn, client.id);
			}
		}

		public override void OnDisconnected(Client client) {
			if (server.isMaster) {
				client.Call(Closed);
				server.SendData(client);
			}
		}

		/// <summary> RPC sent by a client when it explicitly disconnects. </summary>
		public void Closed(Client sender, Message message) {
			Log.Debug($"Closing Client {sender.identity} isSlave?{sender.isSlave}");
			if (server.isMaster) {
				// Single client was closed remotely.
				server.Close(sender);
			} else {
				// Remote server was closed.
				server.Stop();
			}
		}

		public void Syn(Client sender, Message message) {
			Log.Verbose($"Syn from {sender.identity}: {message[0]}");
			sender.Call(SynAck, message[0]);
		}

		public void SynAck(Client sender, Message message) {
			Log.Verbose($"SynAck from {sender.identity}: {message[0]}");
			sender.Call(Ack, message[0]);
		}


		public void Ack(Client sender, Message message) {
			Log.Verbose($"Ack from {sender.identity}: {message[0]}");


		}

	}
}
