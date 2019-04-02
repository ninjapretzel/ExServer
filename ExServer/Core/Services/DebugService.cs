using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex{

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

		public void Ping(Message msg) {
			Log.Verbose($"Ping'd by {msg.sender.identity}");

			// Since we are an instance, we can reference the Pong method directly. 
			msg.sender.Call(Pong);

			// If accessing another service's methods, this will help keep references during refactoring:
			// sender.Call(Members<DebugService>.i.Pong);

		}
		public void Pong(Message msg) {

			Log.Verbose($"Pong'd by {msg.sender.identity}");

		}

	}
	
}
