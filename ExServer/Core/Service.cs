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

}
