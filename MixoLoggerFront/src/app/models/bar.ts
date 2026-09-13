import { Guid } from "../core/base-models";
import { Volume } from "./cocktail";

/**
 * Niveau approximatif d'une bouteille. C'est la granularité que l'utilisateur
 * tient réellement à jour ; le volume exact est optionnel (cf. docs/STRATEGIE.md §3).
 */
export type NiveauStock = "Pleine" | "Entamee" | "PresqueFinie";

export const NIVEAUX_STOCK: readonly NiveauStock[] = ["Pleine", "Entamee", "PresqueFinie"];

export const LIBELLES_NIVEAU: Record<NiveauStock, string> = {
	Pleine: "Pleine",
	Entamee: "Entamée",
	PresqueFinie: "Presque finie"
};

/** Une ligne de stock du bar. */
export interface LigneStock {
	id: Guid;
	name: string;
	niveau: NiveauStock;
	/** `null` en possession simple. */
	quantity: Volume | null;
	suiviPrecis: boolean;
}

export interface MyBar {
	ingredients: LigneStock[];
}

/** Entrée du référentiel d'ingrédients, proposée à l'autocomplétion. */
export interface IngredientReference {
	id: Guid;
	name: string;
	/** Autres libellés reconnus par l'API, déjà normalisés (cf. `normaliserNom`). */
	aliases: string[];
}
