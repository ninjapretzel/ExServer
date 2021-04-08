import Component from "./Component.mjs";
import globals from "../globals.mjs";

export default class LoginPage extends Component {
	constructor(element) {
		super(element);
		
		element.find("#login").click(async (e)=> {
			const res = await globals.client._services.LoginService.RequestLogin("test", "test");
			console.log("Did it work? ", res);
		});
	}
	
	
	
}