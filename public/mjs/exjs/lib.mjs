
export const SEP = "\u0007";
export const EOT = "\u001f";
export const VERSION = "v0.0.0";

export function format() {
	let str = "";
	for (let i = 0; i < arguments.length; i++) {
		str += arguments[i];
		str += (i < arguments.length-1) ? SEP : "";
	}
	return str;
}

export function packNow() {
	const now = Date.now();
	const result = new Uint8Array(8);
	let x = now;
	for (let i = 0; i < 8; i++) {
		result[i] = x % 0x100;
		x /= 0x100;
	}
	return base64Encode(result);
}

export function base64Decode(base64) {
	const binary = atob(base64);
	const length = binary.length;
	const bytes = new Uint8Array(length);
	for (let i = 0; i < length; i++) {
		bytes[i] = binary.charCodeAt(i);	
	}
	return bytes.buffer;
}

export function base64Encode(bytes) {
	if (typeof(bytes) === "string") { return btoa(bytes); }
	
	let s = "";
	for (let i = 0; i < bytes.length; i++) {
		s += String.fromCharCode(bytes[i]);
	}
	return btoa(s);
}

export function openSocket(type) {
	let wsock = new WebSocket(`ws://localhost:3000/ws/${type}`, type);
	
	return wsock;
}

export function dynamicNew(ctor, args){
    return new (ctor.bind.apply(ctor, [null].concat(args)))();
};
/** Promise wrapper to run code after a delay */
export function delay(ms) {
	return new Promise((resolve, reject) => { setTimeout( ()=>{resolve(); }, ms); });
}