import { Guid } from "../core/base-models";

export interface Cocktail {
	id: Guid;
	name: string;
	description: string;
}

export interface CocktailComponent {
	ingredient: Ingredient;
	volume: Volume;
}
export interface CocktailDetail extends Cocktail {
	ingredients: CocktailComponent[];
	/**
	 * Nom imposé par l'API, qui sérialise encore l'entité de domaine `Cocktail` telle
	 * quelle (docs/MVP.md §7, B5). Il changera probablement avec l'introduction d'un DTO.
	 */
	etapeRecettes: EtapeRecette[];
}

export interface EtapeRecette {
	id: Guid;
	description: string;
	/** Commence à 1. */
	ordre: number;
}

export interface Ingredient {
	id: Guid;
	name: string;
}

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
