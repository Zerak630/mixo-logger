import { Guid } from "../core/base-models";

export interface Cocktail {
  id: Guid;
  name: string;
  description: string;
}

export interface CocktailIngredient {
  ingredient: Ingredient;
  volume: Volume;
}
export interface CocktailDetail extends Cocktail {
  ingredients: CocktailIngredient[];
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
