import Service from "./Service.mjs";

export default class LoginService extends Service {
	
	publicKey;
	constructor (client) {	
		super(client);
	}
	SetServerPublicKey(msg) {
		this.publicKey = msg[0];
	}
}