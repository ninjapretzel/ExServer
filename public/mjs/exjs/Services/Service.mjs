export default class Service {
	_client; get client() { return this._client; }
	
	constructor(client) {
		this._client = client;	
	}
}