import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { Utilisateur } from "../models/utilisateur";
import UserService from "./user.service";

/** Chemins d'API qui ne doivent pas déclencher la redirection vers la connexion sur un 401. */
export const CHEMINS_AUTH = ["/Auth/connexion", "/Auth/moi", "/Auth/deconnexion"];

@Injectable({
	providedIn: 'root'
})
export default class AuthService {
	private readonly http = inject(HttpClient);
	private readonly userService = inject(UserService);

	/**
	 * Ouvre une session. L'API pose le cookie ; on ne garde côté front que l'utilisateur.
	 * Rejette avec la réponse d'erreur (401 identifiants faux, 429 trop d'essais).
	 */
	public async login(identifiant: string, motDePasse: string): Promise<Utilisateur> {
		const utilisateur = await firstValueFrom(
			this.http.post<Utilisateur>("/Auth/connexion", { identifiant, motDePasse }));

		this.userService.setUser(utilisateur);
		return utilisateur;
	}

	/** Ferme la session. L'état local est vidé même si l'API ne répond pas. */
	public async logout(): Promise<void> {
		try {
			await firstValueFrom(this.http.post<void>("/Auth/deconnexion", null));
		} finally {
			this.userService.emptyUser();
		}
	}

	/**
	 * Au démarrage : y a-t-il déjà une session (cookie encore valide) ? Ne lève jamais —
	 * une API injoignable ou un 401 laissent simplement l'utilisateur déconnecté.
	 */
	public async restaurerSession(): Promise<void> {
		try {
			this.userService.setUser(await firstValueFrom(this.http.get<Utilisateur>("/Auth/moi")));
		} catch (erreur) {
			this.userService.emptyUser();

			if (!(erreur instanceof HttpErrorResponse && erreur.status === 401)) {
				console.warn("Session non vérifiée : l'API est injoignable.", erreur);
			}
		}
	}
}
