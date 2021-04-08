import * as lib from "./lib.mjs"
import DebugService from "./Services/DebugService.mjs";
import LoginService from "./Services/LoginService.mjs";

export class Client {
	
	_wsock;
	_queue;
	_services;
	_credentials; get credentials() { return this._credentials; }
	_data; get data() { return this._data; }
	_open; get open() { return this._open; }
	
	constructor(wsock, services) {
		this._queue = [];
		this._wsock = wsock;
		this._credentials = null;
		this._data = {};
		wsock.onopen = this.wsockOnOpen;
		wsock.onmessage = this.wsockOnMessage;
		wsock.onclose = this.wsockOnClose;
		
		if (!services) { services = { }; }
		const defs = { ...this.defaultServices(), ...services };
		this._services = {};
		for (let key in defs) {
			this._services[key] = lib.dynamicNew(defs[key], this);
		}
		console.log("Got services:", services);
	}
	
	async handler() {
		while (this.open) {
			for (let i = 0; i < this._queue.length; i++) {
				this.handle(this._queue[i]);	
			}
			if (this._queue.length > 0) { this._queue = []; } 
			await lib.delay(1);
		}
	}
	handle(msg) {
		const service = msg[0];
		const rpc = msg[1];
		const time = msg[2]; // Todo: Logging?
		const rest = msg.slice(3);
		msg.time = time;
		msg.service = service;
		msg.rpc = rpc;
		msg.client = this;
		const sv = this._services[service];
		if (sv && sv[rpc]) {
			console.log(`Invoking ${service}.${rpc}(`, rest, `)`);
			sv[rpc](rest);
		} else {
			console.log(`Could not find RPC target ${service}.${rpc}`);	
		}
	}
	
	wsockOnOpen = (e) => {
		console.log("wsock connection begun", e);	
		this.call("DebugService", "Ping");
		if (this.onopen) { this.onopen(e); }
		this._open = true;
		this.handler();
	}
	
	wsockOnMessage = (e) => {
		let data = e.data.split(lib.SEP);
		// console.log("Got wsock data", data);
		this._queue.push(data);
	};
	
	wsockOnClose = (e) => {
		console.log("wsock closed. Client finished.", e);
		this._open = false;
		if (this.onclose) { this.onclose(e); }
	};
	
	
	call(service, name, ...rest) {
		const now = lib.packNow();
		const args = [ service, name, now, ...rest ];
		const msg = lib.format.apply(null, args);
		this._wsock.send(msg);
	}
	
	defaultServices() {
		return {
			LoginService,
			DebugService
		}
		
	}
	
}