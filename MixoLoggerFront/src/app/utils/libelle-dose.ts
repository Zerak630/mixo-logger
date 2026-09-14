import { UniteDose } from "../models/cocktail";

/** Libellés singulier / pluriel des unités de décompte. Les volumes s'affichent tels quels. */
const LIBELLES_DECOMPTE: Partial<Record<UniteDose, [singulier: string, pluriel: string]>> = {
	piece: ["pièce", "pièces"],
	feuille: ["feuille", "feuilles"],
	trait: ["trait", "traits"],
	pincee: ["pincée", "pincées"]
};

const NOMBRE = new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 2 });

/** Libellé d'une unité seule, pour une liste de choix : « feuille(s) », « cL ». */
export function libelleUnite(unite: UniteDose): string {
	const decompte = LIBELLES_DECOMPTE[unite];
	return decompte ? `${decompte[0]}(s)` : unite;
}

/** « 6 feuilles », « 1 trait », « 4,5 cL ». */
export function libelleDose(valeur: number, unite: UniteDose): string {
	const decompte = LIBELLES_DECOMPTE[unite];
	const nombre = NOMBRE.format(valeur);

	if (!decompte) {
		return `${nombre} ${unite}`;
	}

	// En français, le pluriel commence à 2 : « 1,5 pincée ».
	return `${nombre} ${valeur >= 2 ? decompte[1] : decompte[0]}`;
}
