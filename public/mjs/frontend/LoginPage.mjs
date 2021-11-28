import Component from "./Component.mjs";
import globals from "../globals.mjs";

export default class LoginPage extends Component {
	constructor(element) {
		super(element);
		
		element.find("#login").click(async (e)=> {
			
			const user = element.find("#username").val();
			const pass = element.find("#password").val();
			const requestSent = await globals.client._services.LoginService.RequestLogin(user, pass);
			
			
			console.log("Request Sent?", requestSent);
		});
	}
	
	
	
}
