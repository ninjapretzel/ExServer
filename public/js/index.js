function base64Decode(base64) {
	const binary = atob(base64);
	const length = binary.length;
	const bytes = new Uint8Array(length);
	for (let i = 0; i < length; i++) {
		bytes[i] = binary.charCodeAt(i);	
	}
	return bytes.buffer;
}
function base64Encode(bytes) {
	if (typeof(bytes) === "string") { return btoa(bytes); }
	
	let s = "";
	for (let i = 0; i < bytes.length; i++) {
		s += String.fromCharCode(bytes[i]);
	}
	return btoa(s);
}



async function getPublicKey() {
	let request = await fetch("/api/auth/publicKey");
	return (await request.json()).publicKey;
}

const version = "v0.0.0";




async function login(user, pass) {
	let publicKey = await getPublicKey();
	
	let publicKeyRead = await openpgp.readKey({ armoredKey: publicKey });
	
	pass = await openpgp.encrypt({
		message: await openpgp.Message.fromText(pass),
		publicKeys: publicKeyRead,
	});
	
	let payload = { user, pass, version }
	const rawResponse = await fetch("/api/auth/login", {
		method: "POST",
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify(payload)
	});
	return await rawResponse.json();
}

function openSocket(type) {
	let wsock = new WebSocket(`ws://localhost:3000/ws/${type}`, type);

	return wsock;
}


/** Promise wrapper to run code after a delay */
function delay(ms) {
	return new Promise((resolve, reject) => { setTimeout( ()=>{resolve(); }, ms); });
}
var pipe;
var connected;
const SEP = "\u0007";
const EOT = "\u001f";
function format() {
	let str = "";
	for (let i = 0; i < arguments.length; i++) {
		str += arguments[i];
		str += (i < arguments.length-1) ? SEP : "";
	}
	return str;
}

function show(name) {
	$("#pageSwitcher").children(".page").addClass("hidden");
	$("#pageSwitcher").children(`#${name}`).removeClass("hidden");
}
function onConnected(evt) {
	
}

$(document).ready(()=>{
	
	console.log("Ready");
	pipe = openSocket("ex");
	const client = new Client(pipe);
	
	pipe.onopen = (e) => {
		client.call("DebugService", "Ping");
		show("loginPage");
	}
	
	
	
	$("#login").click( async ()=>{
		
		let username = $("#username").val();
		let password = $("#password").val();
		
		$("#username").addClass("disabled");
		$("#password").addClass("disabled");
		$("#login").addClass("disabled");
		
		const result = await login(username, password);
		
		$("#username").removeClass("disabled");
		$("#password").removeClass("disabled");
		$("#login").removeClass("disabled");
		
		if (result.success) {
			M.toast({
				html: "Login Success!",
				classes: "green"
			});
			
		} else {
			M.toast({
				html: `Login Failed: ${result.reason}!`,
				classes: "red"
			});
			return;
		}
		
		let wsock = openSocket();
		
		await delay(200);
		wsock.close();
		
	});
	
});