using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {

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
