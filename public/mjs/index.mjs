import { Client } from './exjs/Client.mjs';
import { openSocket } from "./exjs/lib.mjs";

function show(name) {
	$("#pageSwitcher").children(".page").addClass("hidden");
	$("#pageSwitcher").children(`#${name}`).removeClass("hidden");
}

$(document).ready(()=>{
	
	console.log("Hello from module. Got Client:", Client);
	
	console.log("Metadata:", import.meta);
	const sock = openSocket("ex");
	const client = new Client(sock);
	client.onopen = () => {
		show("loginPage");
	}
	
	console.log("Created client", client);
	
});
