import { Component, computed, inject, signal } from "@angular/core";
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Button } from "primeng/button";
import { DialogModule } from "primeng/dialog";
import { FloatLabel } from "primeng/floatlabel";
import { InputIcon } from "primeng/inputicon";
import AuthService from "../../core/auth.service";
import { toggle, ToggleableSignal } from "../../utils/toggle-signal";
import { IconField } from "primeng/iconfield";

@Component({
	selector: 'login-modal',
	templateUrl: './login-modal.html',
	styleUrl: './login-modal.scss',
	imports: [
		DialogModule,
		ReactiveFormsModule,
		Button,
		InputIcon,
		IconField,
		FloatLabel
	],

})
export default class LoginModalComponent {
	public isModalVisible = toggle(false);
	public hidePasswordInput: ToggleableSignal<boolean> = toggle(true);
	public typePasswordInput = computed(() => this.hidePasswordInput() ? 'password' : 'text');
	public iconPasswordInput = computed(() => this.hidePasswordInput() ? "pi-eye" : "pi-eye-slash");
	public titlePasswordInput = computed(() => this.hidePasswordInput() ? "Voir mon mot de passe" : "Cacher mon mot de passe");

	private readonly authService = inject(AuthService);
	private readonly formBuilder = inject(FormBuilder);

	public connexionForm: FormGroup<ConnexionControls> = this.formBuilder.group({
		username: ['', Validators.required],
		password: ['', Validators.required],
	});
	public get controls() { return this.connexionForm.controls; }

	public async onLoginCLick() {
		if(this.controls.username.value && this.controls.password.value) {
			const isLoginOK = await this.authService.login(this.controls.username.value, this.controls.password.value);
			if(isLoginOK)
				this.isModalVisible.toggle();
			else
				console.log("ERROR");
		}
	}
}

interface ConnexionFields {
	username: string;
	password: string;
}

type ConnexionControls = {
	[K in keyof ConnexionFields]: FormControl<ConnexionFields[K] | null>;
};
