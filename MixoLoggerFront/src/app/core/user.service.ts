import { computed, Injectable, signal, WritableSignal } from "@angular/core";

@Injectable({
	providedIn: 'root'
})
export default class UserService {
	private readonly userData: WritableSignal<ConnectedUserData | undefined> = signal(undefined);

	public readonly isConnected = computed(() => this.userData());


	public setUser(user: ConnectedUserData) {
		this.userData.set(user);
	}

	public emptyUser() {
		this.userData.set(undefined);
	}
}

export interface ConnectedUserData {}
