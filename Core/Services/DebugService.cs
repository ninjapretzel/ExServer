using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Service providing network debugging messages when common events occur. </summary>
	public class DebugService : Service {

		public override void OnStart() {
			Log.Info("Debug Service: Server Started.");
		}

		public override void OnEnable() {
			Log.Verbose("Debug Service Enabled");
		}

		public override void OnDisable() {
			Log.Verbose("Debug Service Disabled");
		}

		public float timeout;
		public bool enableDebugPings = false;
		public override void OnTick(float delta) {
			// Log.Verbose("Debug service tick");
			if (!isMaster && enableDebugPings) {
				timeout += delta;
				if (timeout > 1.0f) {
					server.localClient.Hurl(this.Ping);
					timeout -= 1.0f;
				}
			}
		}


		public override void OnConnected(Client client) {
			Log.Verbose($"Connected {client.identity}");
		}

		public override void OnDisconnected(Client client) {
			Log.Verbose($"Disconnected {client.identity}");
		}

		public void Ping(RPCMessage msg) {
			Log.Info($"Ping'd by {msg.sender.identity}");

			// Since we are an instance, we can reference the Pong method directly. 
			msg.Reply(Pong);
			
			// If accessing another service's methods, this will help keep references during refactoring:
			// sender.Call(Members<DebugService>.i.Pong);
		}

		public void Pong(RPCMessage msg) {

			Log.Info($"Pong'd by {msg.sender.identity}");

		}
		public struct PingNEvent { public int val; }

		public void PingN(RPCMessage msg) {
			if (msg.numArgs < 1) {
				Log.Info($"No arg passed to PingN.");
			}
			int val;
			if (int.TryParse(msg[0], out val)) {
				Log.Info($"Ping'd by {msg.sender.identity} @ {msg.sentAt}->{msg.recievedAt} #{val}");
				if (val > 0) { 
					msg.Reply(PingN, val-1);
				}
				server.On(new PingNEvent() { val = val });

			} else {
				Log.Warning($"PingN failed to parse number from {msg[0]}");
			}

		}

		
	}
	
}
