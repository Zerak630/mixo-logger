import { normaliserNom } from "./normaliser-nom";

/**
 * Teinte (0 à 359) propre à une recette, tirée de son nom : le même cocktail garde la même
 * couleur d'un affichage à l'autre, et deux recettes voisines dans la liste se distinguent.
 * Tient lieu d'illustration tant que les photos (F9) n'existent pas.
 */
export function teinteCocktail(nom: string): number {
	// FNV-1a sur le nom normalisé : « Piña Colada » et « pina colada » ont la même teinte.
	let empreinte = 0x811c9dc5;
	for (const caractere of normaliserNom(nom)) {
		empreinte ^= caractere.codePointAt(0)!;
		empreinte = Math.imul(empreinte, 0x01000193);
	}

	return (empreinte >>> 0) % 360;
}
