using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	public class RPCMessage {
		/// <summary>Separator character to separate segments of transmissions</summary>
		public const char SEPARATOR = (char)0x07; // 'Bell'
												  /// <summary> End Of Transmission </summary>
		public const char EOT = (char)0x1F; // 'Unit Separator'

		/// <summary> Client message was recieved from </summary>
		public Client sender;

		/// <summary> Raw message sent </summary>
		public string rawMessage { get; private set; }
		/// <summary> Timestamp when instance was created </summary>
		public DateTime recievedAt { get; private set; }
		/// <summary> Service name to look up service </summary>
		public string serviceName { get { return content[0]; } }
		/// <summary> Method name to look up method </summary>
		public string methodName { get { return content[1]; } }
		/// <summary> Name of RPC to call (serviceName.methodName)</summary>
		public string rpcName { get { return $"{content[0]}.{content[1]}"; } }
		/// <summary> Raw content of message </summary>
		public string[] content { get; private set; }
		/// <summary> Number of arguments, besides service name/method name </summary>
		public int numArgs { get { return content.Length - FIXED_SIZE; } }

		/// <summary> Fixed Size of the message, for spaces for Serivce Name and for Method Name </summary>
		public const int FIXED_SIZE = 2;

		/// <summary> Indexer for arguments. Valid indexes are in [0, numArgs-1] </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string this[int index] { get { return content[index + FIXED_SIZE]; } }

		/// <summary> Constructs a message around the given string array. </summary>
		/// <param name="prams"> </param>
		public RPCMessage(Client client, params string[] prams) {
			sender = client;
			if (content.Length >= FIXED_SIZE) {
				content = prams;
			} else {
				throw new Exception($"{nameof(RPCMessage)}: Required parameters not provided. Must be {FIXED_SIZE} or more, was provided {prams.Length}.");
			}
		}
		public RPCMessage(Client client, string str) {
			sender = client;
			rawMessage = str;
			recievedAt = DateTime.Now;
			content = rawMessage.Split(SEPARATOR);
		}


		/// <summary> Delegate type used to search for messages to invoke from network messages </summary>
		/// <param name="Client"> Client whomst'd've sent the message </param>
		/// <param name="message"> Message that was sent </param>
		public delegate void Handler(RPCMessage message);
	}

}
