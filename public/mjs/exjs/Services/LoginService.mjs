import Service from "./Service.mjs";
import * as OpenPGP from "../../openpgp.mjs";
import * as lib from "../lib.mjs";

export default class LoginService extends Service {
	
	publicKey;
	login;
	_isAttemptingLogin = false;
	
	constructor (client) {	
		super(client);
	}
	SetServerPublicKey(msg) {
		this.publicKey = msg[0];
	}
	
	async RequestLogin(user, pass) {
		if (this.login) { return false; }
		if (this._isAttemptingLogin) { return false; }
		this._isAttemptingLogin = true;
		this.loginName = user;
		
		const encrypted =  await this.encryptPass(pass);
		this.client.call("LoginService", "Login", user, encrypted, lib.VERSION);
		return true;
	}
	
	async LoginResponse(args) {
		console.log("Login response", args);
		this._isAttemptingLogin = false;
		
		if (args[0] === "succ" && args[1] === this.loginName) { 
			//...success...etc
			const user = args[1];
			const token = args[2];
			const guid = args[3];
			this.login = { user, token, guid };
		} else {
			//...failure...etc
		}
	}
	
	async encryptPass(pass) {
		let publicKeyRead = await OpenPGP.readKey({ armoredKey: this.publicKey });
		const encrypted = await OpenPGP.encrypt({
			message: await OpenPGP.Message.fromText(pass),
			publicKeys: publicKeyRead,
		});
		return encrypted;
	}
}
