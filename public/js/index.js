
async function getPublicKey() {
	let request = await fetch("/api/auth/publicKey");
	return (await request.json()).publicKey;
}

const version = "v0.0.0";

async function login(user, pass) {
	let publicKey = await getPublicKey();
	console.log(publicKey);
	
	let publicKeyRead = await openpgp.readKey({ armoredKey: publicKey });
	
	pass = await openpgp.encrypt({
		message: await openpgp.Message.fromText(pass),
		publicKeys: publicKeyRead,
	});
	console.log(pass);
	
	
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

function openSocket() {
	let wsock = new WebSocket("ws://localhost:3000/ws/test", "test");
	
	wsock.onopen = (e) => {
		wsock.send("Yeet.");	
	}
	wsock.onmessage = (e) => {
		console.log(e.data);
	}
	
	return wsock;
}

/** Promise wrapper to run code after a delay */
function delay(ms) {
	return new Promise((resolve, reject) => { setTimeout( ()=>{resolve(); }, ms); });
}

$(document).ready(()=>{
	console.log("Ready");
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
		console.log(result);
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
		}
		
		let wsock = openSocket();
		
		await delay(200);
		wsock.close();
		
	});
	
});