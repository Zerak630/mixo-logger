import { Guid } from "../core/base-models";

export interface Cocktail {
	id: Guid;
	name: string;
	description: string | null;
}

/** Un cocktail de la liste, évalué contre le bar courant (F4). */
export interface CocktailResume extends Cocktail {
	realisable: boolean;
	/** Ce qui manque pour un verre, dans l'ordre de la recette. Vide si réalisable. */
	manques: Manque[];
}

export interface Manque {
	ingredient: string;
	raison: "Absent" | "Insuffisant";
}

/** Détail d'une recette, tel que renvoyé par `GET /api/cocktails/{id}`. */
export interface CocktailDetail extends Cocktail {
	ingredients: DoseIngredient[];
	/** Dans l'ordre de préparation. */
	etapes: Etape[];
}

/** Un ingrédient de la recette et sa dose pour un verre. */
export interface DoseIngredient {
	ingredientId: Guid;
	/** Nom canonique du référentiel : « angostura » saisi devient « Bitters ». */
	name: string;
	valeur: number;
	unite: UniteDose;
}

export interface Etape {
	/** Commence à 1. */
	ordre: number;
	description: string;
}

/** Contenu envoyé à la création (`POST`) comme à l'édition (`PUT`). */
export interface RecetteSaisie {
	name: string;
	description: string | null;
	ingredients: { name: string; valeur: number; unite: UniteDose }[];
	/** Dans l'ordre : leur position fait leur numéro. */
	etapes: string[];
}

/**
 * Unités de dose acceptées par l'API (`GET /api/cocktails/unites`) : volumes, puis
 * décomptes. Un décompte se vérifie par la seule présence de l'ingrédient dans le bar.
 */
export type UniteDose = "mL" | "cL" | "dL" | "L" | "piece" | "feuille" | "trait" | "pincee";

export interface Volume {
	value: number;
	unit: UniteVolume;
}

export enum UniteVolume {
	Mililitre = "mL",
	Centilitre = "cL",
	Decilitre = "dL",
	Litre = "L",
}
