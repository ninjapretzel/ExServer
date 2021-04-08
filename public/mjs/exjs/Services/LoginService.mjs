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
		
		this.client.call("LoginService", "Login", user, await this.encryptPass(pass), lib.VERSION);
		return true;
	}
	
	async encryptPass(pass) {
		let publicKeyRead = await OpenPGP.readKey({ armoredKey: this.publicKey });
		await OpenPGP.encrypt({
			message: await OpenPGP.Message.fromText(pass),
			publicKeys: publicKeyRead,
		});
		return pass;
	}
}