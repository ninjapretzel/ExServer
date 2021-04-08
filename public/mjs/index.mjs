import { Client } from './exjs/Client.mjs';
import * as lib from "./exjs/lib.mjs";
import globals from "./globals.mjs";

import LoginPage from "./frontend/LoginPage.mjs";

function show(name) {
	$("#pageSwitcher").children(".page").addClass("hidden");
	$("#pageSwitcher").children(`#${name}`).removeClass("hidden");
}
const pages = {} 
function start() {
	const pageTypes = { 
		LoginPage,
	}
	for (let key in pageTypes) {
		pages[key] = lib.dynamicNew(pageTypes[key], [ $(`#${key}`), key ] );
	}
}

$(document).ready(()=>{
	start();
	
	console.log("Hello from module. Got Client:", Client);
	
	console.log("Metadata:", import.meta);
	const sock = lib.openSocket("ex");
	const client = new Client(sock);
	globals.client = client;
	client.onopen = () => {
		show("LoginPage");
	}
	
	console.log("Created client", client);
});
