import { IngredientReference } from "../models/bar";
import { normaliserNom } from "./normaliser-nom";

/**
 * Recherche dans le référentiel, sur le nom et sur les alias, sans tenir compte des accents
 * ni de la casse. Les correspondances en début de nom passent en premier : « gin » doit
 * proposer « Gin » avant « Bière ginger ».
 *
 * Partagée par la saisie du stock (Mon Bar) et celle des recettes, pour qu'un même mot
 * propose les mêmes ingrédients partout.
 *
 * @param exclus noms normalisés à ne pas reproposer (déjà présents dans le bar ou la recette).
 */
export function rechercherIngredients(
	referentiel: readonly IngredientReference[],
	requete: string,
	exclus: ReadonlySet<string> = new Set(),
	maximum = 8
): IngredientReference[] {
	const cle = normaliserNom(requete);

	return referentiel
		.filter(ingredient => !exclus.has(normaliserNom(ingredient.name)))
		.map(ingredient => ({ ingredient, rang: rang(ingredient, cle) }))
		.filter(candidat => candidat.rang < Number.POSITIVE_INFINITY)
		.sort((a, b) => a.rang - b.rang || a.ingredient.name.localeCompare(b.ingredient.name))
		.slice(0, maximum)
		.map(candidat => candidat.ingredient);
}

/**
 * Le nom canonique qu'un libellé libre désignera une fois résolu par l'API : « white rum »
 * et « Rhum blanc » donnent la même clé. Un nom inconnu du référentiel est sa propre clé.
 */
export function cleIngredient(referentiel: readonly IngredientReference[], nom: string): string {
	const cle = normaliserNom(nom);
	const connu = referentiel.find(ingredient => normaliserNom(ingredient.name) === cle || ingredient.aliases.includes(cle));

	return connu ? normaliserNom(connu.name) : cle;
}

/** 0 : le nom commence par la requête ; 1 : un mot du nom ; 2 : un alias ; ∞ : aucune correspondance. */
function rang(ingredient: IngredientReference, requete: string): number {
	const nom = normaliserNom(ingredient.name);

	if (!requete || nom.startsWith(requete)) return 0;
	if (nom.split(" ").some(mot => mot.startsWith(requete))) return 1;
	if (ingredient.aliases.some(alias => alias.includes(requete))) return 2;

	return Number.POSITIVE_INFINITY;
}
