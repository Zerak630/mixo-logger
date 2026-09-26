import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Avatar } from '@openng/optimus-ui/avatar';
import { Button } from '@openng/optimus-ui/button';
import { ToastModule } from '@openng/optimus-ui/toast';
import AuthService from './core/auth.service';
import UserService from './core/user.service';

@Component({
	selector: 'app-root',
	imports: [
		RouterOutlet,
		RouterLink,
		RouterLinkActive,
		Avatar,
		Button,
		ToastModule
	],
	templateUrl: './app.html',
	changeDetection: ChangeDetectionStrategy.Eager,
	styleUrl: './app.scss'
})
export class App {
	protected readonly title = signal('MixoLoggerFront');

	/**
	 * Deux liens : une navigation écrite à la main, plutôt que le Menubar d'OptimusUI (38 kB dans
	 * le chargement initial, pour un menu sans sous-menu).
	 */
	protected readonly liens = [
		{ libelle: "Cocktails", icone: "pi pi-list", chemin: "/cocktails" },
		{ libelle: "Mon bar", icone: "pi pi-box", chemin: "/my_bar" }
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
