using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core{

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
	
}
