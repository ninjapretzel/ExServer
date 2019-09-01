using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ex.Utils;
using static Ex.RPCMessage;

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
		internal Crypt enc = (b) => b;
		/// <summary> Decryption </summary>
		internal Crypt dec = (b) => b;
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

			{ // Temp encryption
				EncDec encryptor = new EncDec();
				Crypt e = (b) => encryptor.Encrypt(b);
				Crypt d = (b) => encryptor.Decrypt(b);
				SetEncDec(e, d);
				//enc = e;
				//dec = d;
			}
			
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
				connection.Dispose();
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


		#region Crypt
		private static readonly string testMessage = ("holy s-t g*d-n this is an annoying f-n song." + EOT
				+ "Now you'll get it stuck inside your head and you'll sing it all night long." + EOT
				+ "This is the game." + EOT
				+ "That doesn't end." + EOT
				+ "One hundred levels." + EOT
				+ "And start again." + EOT
				+ "While you were stuck in the basement all day." + EOT
				+ "Other children were out to play." + EOT
				+ "*Gunshot*" + EOT
				+ "Anyone who takes anything they find on the internet seriously should be dragged out and shot." + EOT
				+ "Everything here is satire, only a fool would take it seriously." + EOT
				+ "Don't you dare make negative memes about CNN" + EOT
				+ "SOME" + EOT + "VERY" + EOT + "SHORT" + EOT + "MESSAGES " + EOT + "HERE" + EOT
				+ "EVN" + EOT + "MORE" + EOT + "SHRT" + EOT + "MSGS" + EOT + "HRE " + EOT + "YEP" + EOT
				+ "SHUT IT DOWN THEY KNOW" + EOT
				+ "Clown World" + EOT
				+ "Is this enough test data yet?" + EOT
				+ "Nuke California" + EOT
				+ "何だ？ 女人の写真？ 綺麗な人はな。誰？ ｍａｉ ｗａｉｆｕ。嘘！" + EOT
				+ "なんだ？ おなのひとのしゃしん？ きれいなひとはな。 だれ？ ｍａｉ ｗａｉｆｕ。うそ!" + EOT
				+ "I'm running out of ideas." + EOT
				+ "Epic Games Exclusivity made Mechwarrior 5 a dumpster fire." + EOT
				+ "I just want my son back" + EOT
				+ "The easiest way to make money is to make a game where everyone is on an island trying to shoot each other. I don't want to make that - Hideo Kojima, SDCC2019" + EOT
				+ "TradChad" + EOT
				+ "动态网自由门 天安門 天安门 法輪功 李洪志 Free Tibet 六四天安門事件 The Tiananmen Square protests of 1989 天安門大屠殺 The Tiananmen Square Massacre 反右派鬥爭 " 
				+ "The Anti-Rightist Struggle 大躍進政策 The Great Leap Forward 文化大革命 The Great Proletarian Cultural Revolution 人權 Human Rights 民運 " 
				+ " Democratization 自由 Freedom 獨立 Independence 多黨制 Multi-party system 台灣 臺灣 Taiwan Formosa 中華民國 Republic of China 西藏 土伯特 唐古特 " 
				+ "Tibet 達賴喇嘛 Dalai Lama 法輪功 Falun Dafa 新疆維吾爾自治區 The Xinjiang Uyghur Autonomous Region 諾貝爾和平獎 Nobel Peace Prize 劉暁波 Liu Xiaobo 民主 言論 思想 反共 "
				+ "反革命 抗議 運動 騷亂 暴亂 騷擾 擾亂 抗暴 平反 維權 示威游行 李洪志 法輪大法 大法弟子 強制斷種 強制堕胎 民族淨化 人體實驗 肅清 胡耀邦 趙紫陽 魏京生 王丹 還政於民 和平演變 激流中國 北京之春 "
				+ "大紀元時報 九評論共産黨 獨裁 專制 壓制 統一 監視 鎮壓 迫害 侵略 掠奪 破壞 拷問 屠殺 活摘器官 誘拐 買賣人口 遊進 走私 毒品 賣淫 春畫 賭博 六合彩 天安門 天安门 法輪功 李洪志 Winnie the Pooh 劉曉波动态网自由门" + EOT
				+ "I can't sleep eddy, I keep thinking. How's this possible? A bakery existed for 5 years and had 15 ovens to bake breads. "
				+ "Each oven could only bake one bread an hour. 15x24 hours = 360, 360x365 days = 131,400, 131,400x5 years = 657,000. "
				+ "And yet people say they bought 6 million breads from that bakery" + EOT
				+ "Today is the day we take the stairs." + EOT
				+ "Would you like to know more?" + EOT
				+ "With the glass ceiling broken, all the oppressed groups shall prosper. Especially the most oppressed group of all: Gamers." + EOT
				+ "Digger is our word, but you can say digga." + EOT
				+ "TetraDev: Follow your dreams! YokoTaro: I'll follow your dreams!" + EOT
				+ "Klarth: Mint has that quiet elegance about her, but I bet Arche f---- like a tiger." + EOT
				+ "Storm Area 51" + EOT
				+ "Yes Epstien was SPIRIT COOKING." + EOT
				+ "Don't worry, it's just fake news." + EOT
			).Replace(' ', SEPARATOR);

		/// <summary>
		/// Attempts to set enc and dec to a pair of methods.
		/// Tests them agains some test data in a loop similar to how data is handled over the network, 
		/// and makes sure that arbitrary cuts on the data doesn't break the encryption scheme.
		/// </summary>
		/// <param name="encrypt"> Function for encrypting the data </param>
		/// <param name="decrypt"> Function for decrypting the data </param>
		public void SetEncDec(Crypt encrypt, Crypt decrypt) {
			Log.Info("Doing encrypt/decrypt self test");
			if (encrypt == null || decrypt == null) {
				Log.Warning("Proper Encryption/Decryption functions must be provided to clients...");
				return;
			}

			byte[] oneEnc = encrypt(oneByte);
			byte[] oneDec = decrypt(oneEnc);

			if (oneDec.Length != 1 && oneDec[0] != oneByte[0]) { 
				Log.Warning("!Encryption/decryption must properly handle tiny things as well! (One byte in size)");
				return;
			}

			List<byte[]> wew = new List<byte[]>();
			StringBuilder recieved = "";
			int stringpos = 0;
			SRNG rand = new SRNG();
			while (stringpos < testMessage.Length) {
				int next = stringpos + rand.NextInt(1, 42);
				if (next > testMessage.Length) { next = testMessage.Length; }

				int diff = next - stringpos;
				string cut = testMessage.Substring(stringpos, diff);
				stringpos = next;

				wew.Add(cut.ToBytesUTF8());
			}


			string[] testMessages = testMessage.Split(EOT);
			byte[][] multibytearraydrifting = wew.ToArray();
			StringBuilder held = "";
			string str;
			int pos = 0;
			foreach (var bytes in multibytearraydrifting) {
				byte[] message = encrypt(bytes);
				message = decrypt(message);
				str = message.GetStringUTF8();

				held += str;
				int index = held.IndexOf(EOT);
				while (index >= 0) {
					string pulled = held.Substring(0, index);
					held = held.Remove(0, index + 1);
					index = held.IndexOf(EOT);
					
					if (pulled.Length > 0) {
						if (pulled != testMessages[pos]) { 
							Log.Warning($"Failed to convert message {pos} accurately. Expected {testMessage[pos]}, got { pulled }");
						}
						recieved = recieved + pulled + EOT;
						pos++;
					}
				}
			}



			string fullRecieved = recieved.ToString();
			if (fullRecieved == testMessage) {
				enc = encrypt;
				dec = decrypt;
			} else {
				string s = "Client.SetEncDec: Attempted to set Encryption/Decryption methods, but they did not work properly."
					+ "\n\nExpected: " + testMessage + "\n\nRecieved: " + fullRecieved;
				Log.Warning(s);
				enc = null;
				dec = null;
			}

		}

		#endregion
	}

}
