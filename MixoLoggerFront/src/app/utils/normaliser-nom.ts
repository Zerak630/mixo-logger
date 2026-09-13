/**
 * Même règle que `IngredientName.Normalize` côté API : accents retirés, minuscules,
 * toute suite de caractères non alphanumériques réduite à une espace.
 *
 * « Crème de Coco » et « creme  de coco » donnent « creme de coco ». Garder les deux
 * implémentations alignées : c'est ce qui permet à la recherche locale de retrouver
 * les alias renvoyés par l'API, eux-mêmes stockés sous cette forme.
 */
export function normaliserNom(nom: string): string {
	return nom
		.normalize("NFD")
		.replace(/\p{Mn}/gu, "")
		.toLowerCase()
		.replace(/[^\p{L}\p{N}]+/gu, " ")
		.trim();
}
