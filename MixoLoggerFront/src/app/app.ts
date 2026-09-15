import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { MenuItem } from '@openng/optimus-ui/api';
import { Avatar } from '@openng/optimus-ui/avatar';
import { Button } from '@openng/optimus-ui/button';
import { DialogService } from '@openng/optimus-ui/dynamicdialog';
import { Menubar } from '@openng/optimus-ui/menubar';
import { ToastModule } from '@openng/optimus-ui/toast';
import AuthService from './core/auth.service';
import UserService from './core/user.service';

@Component({
	selector: 'app-root',
	imports: [
		RouterOutlet,
		Menubar,
		Avatar,
		Button,
		ToastModule
	],
	providers: [
		DialogService
	],
	templateUrl: './app.html',
	changeDetection: ChangeDetectionStrategy.Eager,
	styleUrl: './app.scss'
})
export class App {
	protected readonly title = signal('MixoLoggerFront');

	/** Seulement des écrans qui existent : « À propos » et « Contact » pointaient vers des routes absentes. */
	protected readonly menuItems: MenuItem[] = [
		{
			label: "Cocktails",
			icon: "pi pi-list",
			routerLink: "/cocktails"
		},
		{
			label: "Mon bar",
			icon: "pi pi-box",
			routerLink: "/my_bar"
		},
	];

	protected readonly annee = new Date().getFullYear();

	protected readonly userService = inject(UserService);
	private readonly authService = inject(AuthService);
	private readonly router = inject(Router);

	protected readonly deconnexionEnCours = signal(false);

	protected async seDeconnecter(): Promise<void> {
		this.deconnexionEnCours.set(true);
		try {
			await this.authService.logout();
		} catch {
			// La session locale est déjà vidée par AuthService : on quitte l'application quand même.
		} finally {
			this.deconnexionEnCours.set(false);
			await this.router.navigate(['/connexion']);
		}
	}
}
