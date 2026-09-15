import { DOCUMENT, inject, Injectable } from "@angular/core";
import { MessageService } from "@openng/optimus-ui/api";
import { Cocktail } from "../models/cocktail";

/**
 * Partage le lien d'une recette. Sur un appareil tactile, la feuille de partage du système
 * (messagerie, SMS…) ; ailleurs, le lien est copié. Seuls les membres connectés peuvent
 * l'ouvrir : le message le rappelle.
 */
@Injectable({
	providedIn: "root"
})
export class PartageService {
	private readonly document = inject(DOCUMENT);
	private readonly messageService = inject(MessageService);

	async partagerCocktail(cocktail: Pick<Cocktail, "id" | "name">): Promise<void> {
		const lien = new URL(`cocktails/${cocktail.id}`, this.document.baseURI).href;
		const navigateur = this.document.defaultView?.navigator;

		// Sur ordinateur, la feuille de partage du système surprend plus qu'elle n'aide : on copie.
		const tactile = this.document.defaultView?.matchMedia("(pointer: coarse)").matches ?? false;

		if (tactile && navigateur?.share) {
			try {
				await navigateur.share({ title: cocktail.name, text: `La recette « ${cocktail.name} » sur MixoLogger`, url: lien });
				return;
			} catch (erreur) {
				// Fermer la feuille de partage n'est pas une erreur ; tout autre échec retombe sur la copie.
				if (erreur instanceof DOMException && erreur.name === "AbortError") return;
			}
		}

		try {
			await navigateur!.clipboard.writeText(lien);
			this.messageService.add({
				severity: "success",
				summary: "Lien copié",
				detail: `« ${cocktail.name} » : le lien s'ouvre pour tout membre connecté.`,
				life: 4000
			});
		} catch {
			// Presse-papiers refusé (permissions, contexte non sécurisé) : on donne le lien à copier à la main.
			this.messageService.add({
				severity: "warn",
				summary: "Copie impossible",
				detail: `Copie ce lien : ${lien}`,
				life: 10000
			});
		}
	}
}
