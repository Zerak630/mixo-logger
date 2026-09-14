import { Guid } from "../core/base-models";

export interface Cocktail {
	id: Guid;
	name: string;
	description: string | null;
}

/** Un cocktail de la liste, évalué contre le bar de l'utilisateur connecté (F4). */
export interface CocktailResume extends Cocktail {
	/** Noms canoniques des ingrédients : la recherche porte aussi sur eux (F8). */
	ingredients: string[];
	realisable: boolean;
	/** Ce qui manque pour un verre, dans l'ordre de la recette. Vide si réalisable. */
	manques: Manque[];
	/** Vrai si l'utilisateur connecté en est l'auteur (filtre « Mes recettes »). */
	modifiable: boolean;
	notes: Notes;
}

/** Notes d'un cocktail vues par l'utilisateur connecté (F7). */
export interface Notes {
	/** Moyenne arrondie au dixième, `null` sans aucune note. */
	moyenne: number | null;
	nombre: number;
	/** De 1 à 5, `null` si l'utilisateur n'a pas noté. */
	maNote: number | null;
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
	/** Nom affiché de l'auteur ; `null` pour une recette d'origine (ou un compte retiré). */
	auteur: string | null;
	/** Vrai si l'utilisateur connecté en est l'auteur : lui seul peut la modifier ou la supprimer. */
	modifiable: boolean;
	notes: Notes;
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
