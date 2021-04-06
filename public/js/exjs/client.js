
SEP = "\u0007";
EOT = "\u001f";
VERSION = "v0.0.0";
function format() {
	let str = "";
	for (let i = 0; i < arguments.length; i++) {
		str += arguments[i];
		str += (i < arguments.length-1) ? SEP : "";
	}
	return str;
}
function base64Decode(base64) {
	const binary = atob(base64);
	const length = binary.length;
	const bytes = new Uint8Array(length);
	for (let i = 0; i < length; i++) {
		bytes[i] = binary.charCodeAt(i);	
	}
	return bytes.buffer;
}
function packNow() {
	const now = Date.now();
	const result = new Uint8Array(8);
	let x = now;
	for (let i = 0; i < 8; i++) {
		result[i] = x % 0x100;
		x /= 0x100;
	}
	
	return base64Encode(result);
}





function Client(wsock, services) {
	if (!services) { services = {}; }
	
	this.queue = [];
	this.wsock = wsock;
	this.data = {};
	this.LoginService = {
		SetServerPublicKey: (client, msg) => { client.data.publicKey = msg[3]; }
	};
	this.EntityService = {
		SpawnEntity: (client, msg) => {
			console.log("Got entity", msg[3], msg[4]);
		}
		
	};
	this.SyncService = {
		data: {},
		SyncJson: (client, msg) => {
			const target = msg[3];
			const data = JSON.parse(msg[4]);
			console.log("merging data", data, `to ${target}`);
			client.syncService.data[target] = { ...client.syncService.data[target], ...data };
		}
	}
	this.DebugService = {
		Ping: (client, msg) => {
			console.log("PING");
			// client.send("DebugService", "Pong",	
		}
	}
	this.services = { 
		LoginService: this.LoginService,
		EntityService: this.EntityService,
		SyncService: this.SyncService,
		DebugService: this.DebugService,
		...services
	}
	this.open = true;
	
	this.call = (service, name, ...rest) => {
		const now = packNow();
		const args = [service, name, now, ...rest];
		const msg = format.apply(null, args);
		wsock.send(msg);
	}
	
	wsock.onmessage = (e) => {
		let data = e.data.split(SEP);
		console.log("Got wsock data: ", data);
		this.queue.push(data);
	}
	wsock.onclose = (e) => {
		console.log("wsock closed. Client finished.", e);
		this.open = false;
	}
	
	this.handler = async ()=>{
		while (this.open) {
			for (let i = 0; i < this.queue.length; i++) {
				this.handle(queue[i]);	
			}
			
			queue = [];
			await delay(1);
		}
	};
	
	this.handle = (msg) => {
		const service = msg[0];
		const rpc = msg[1];
		const time = msg[2];
		
		const sv = this.services[service];
		if (sv && sv[rpc]) {
			sv[rpc](this, msg);	
		} else {
			console.log(`Could not find RPC target ${service}.${rpc}`);	
		}
	}
	
	
	
	
	
}