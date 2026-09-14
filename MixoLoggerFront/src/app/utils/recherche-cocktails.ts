import { IngredientReference } from "../models/bar";
import { CocktailResume } from "../models/cocktail";
import { normaliserNom } from "./normaliser-nom";

/** Critères de la liste des cocktails (F4, F8). Un critère vide ou faux ne filtre rien. */
export interface CriteresCocktails {
	texte: string;
	seulementRealisables: boolean;
	seulementMesRecettes: boolean;
	/** Moyenne minimale ; `null` pour ne pas filtrer. Un cocktail sans note est alors exclu. */
	noteMinimale: number | null;
}

/**
 * Filtre la liste sans en changer l'ordre (celui de l'API : réalisables d'abord).
 *
 * La recherche texte ignore accents et casse, et chaque mot doit être trouvé : « rhum menthe »
 * trouve le Mojito. Un mot est trouvé dans le nom, la description ou un ingrédient — y compris
 * par un alias du référentiel : « white rum » trouve les recettes au « Rhum blanc ».
 */
export function filtrerCocktails(
	cocktails: readonly CocktailResume[],
	criteres: CriteresCocktails,
	referentiel: readonly IngredientReference[]
): CocktailResume[] {
	const mots = normaliserNom(criteres.texte).split(" ").filter(Boolean);
	const aliasParNom = new Map(referentiel.map(ingredient => [normaliserNom(ingredient.name), ingredient.aliases]));

	return cocktails.filter(cocktail =>
		(!criteres.seulementRealisables || cocktail.realisable)
		&& (!criteres.seulementMesRecettes || cocktail.modifiable)
		&& (criteres.noteMinimale === null || (cocktail.notes.moyenne ?? 0) >= criteres.noteMinimale)
		&& (mots.length === 0 || correspond(cocktail, mots, aliasParNom)));
}

function correspond(cocktail: CocktailResume, mots: string[], aliasParNom: ReadonlyMap<string, readonly string[]>): boolean {
	const textes = [cocktail.name, cocktail.description ?? ""].map(normaliserNom);
	const ingredients = cocktail.ingredients.map(normaliserNom);
	// Chaque ingrédient compte pour son nom et tous ses alias : « white » et « rum » se trouvent
	// tous deux dans l'alias « white rum » du « Rhum blanc ».
	const libellesIngredients = ingredients.flatMap(nom => [nom, ...(aliasParNom.get(nom) ?? [])]);

	return mots.every(mot => textes.some(texte => texte.includes(mot)) || libellesIngredients.some(libelle => libelle.includes(mot)));
}
