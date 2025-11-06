import { inject, Injectable } from "@angular/core";
import UserService from "./user.service";
import { firstValueFrom, of } from "rxjs";

@Injectable({
	providedIn: 'root'
})
export default class AuthService {
	private readonly FAKE_USER = {
		username: 'Testboi',
		password: 'passBoi'
	};

	private readonly userService = inject(UserService);

	public async login(username: string, password: string): Promise<boolean> {
		if (this.userService.isConnected())
			this.userService.emptyUser();

		return firstValueFrom(of(username == this.FAKE_USER.username && password == this.FAKE_USER.password))
			.then(res => {
				if (res)
					this.userService.setUser({});
				else
					this.userService.emptyUser();

				return res;
			});
	}
}
