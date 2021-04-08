import Service from "./Service.mjs";

export default class DebugService extends Service {
	constructor (client) { 
		super(client);	
	}
	
	Ping(msg) {
		console.log("Ping'd by server!");
		this.client.send("DebugService", "Pong");	
	}
	Pong(msg) {
		console.log("Pong'd by server!");
	}
}