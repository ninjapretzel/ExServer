

function show(name) {
	$("#pageSwitcher").children(".page").addClass("hidden");
	$("#pageSwitcher").children(`#${name}`).removeClass("hidden");
}

$(document).ready(()=>{
	show("oops");
});