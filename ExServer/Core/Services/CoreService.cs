using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

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
		public void Closed(Message msg) {
			Log.Debug($"Closing Client {msg.sender.identity} isSlave?{msg.sender.isSlave}");
			if (server.isMaster) {
				// Single client was closed remotely.
				server.Close(msg.sender);
			} else {
				// Remote server was closed.
				server.Stop();
			}
		}

		public void Syn(Message msg) {
			Log.Verbose($"Syn from {msg.sender.identity}: {msg[0]}");
			msg.sender.Call(SynAck, msg[0]);
		}

		public void SynAck(Message msg) {
			Log.Verbose($"SynAck from {msg.sender.identity}: {msg[0]}");
			msg.sender.Call(Ack, msg[0]);
		}


		public void Ack(Message msg) {
			Log.Verbose($"Ack from {msg.sender.identity}: {msg[0]}");


		}

	}
}
