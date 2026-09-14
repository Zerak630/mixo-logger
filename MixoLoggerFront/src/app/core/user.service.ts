import { computed, Injectable, signal } from "@angular/core";
import { Utilisateur } from "../models/utilisateur";

/**
 * État de la session côté front : qui est connecté, s'il l'est.
 *
 * Ce n'est qu'un reflet : la session réelle est un cookie HttpOnly que le JavaScript ne peut
 * pas lire. C'est l'API qui fait foi (`GET /api/auth/moi`, et tout 401).
 */
@Injectable({
	providedIn: 'root'
})
export default class UserService {
	private readonly utilisateurCourant = signal<Utilisateur | null>(null);

	public readonly utilisateur = this.utilisateurCourant.asReadonly();

	public readonly isConnected = computed(() => this.utilisateurCourant() !== null);

	/** « AL » pour « Alice Lemaire », « A » pour « alice ». */
	public readonly initiales = computed(() => {
		const nom = this.utilisateurCourant()?.nomAffiche.trim() ?? "";
		return nom.split(/\s+/).filter(Boolean).slice(0, 2).map(mot => mot[0]!.toLocaleUpperCase("fr-FR")).join("");
	});

	public setUser(utilisateur: Utilisateur) {
		this.utilisateurCourant.set(utilisateur);
	}

	public emptyUser() {
		this.utilisateurCourant.set(null);
	}
}
