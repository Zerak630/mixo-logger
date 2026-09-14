import { Notes } from "../models/cocktail";

const FORMAT_MOYENNE = new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** « 4,3 », « 4,0 » ; chaîne vide sans note. */
export function formaterMoyenne(moyenne: number | null): string {
	return moyenne === null ? "" : FORMAT_MOYENNE.format(moyenne);
}

/** « Note moyenne 4,3 sur 5 (3 notes), ta note : 5 », ou « Pas encore noté ». */
export function libelleNotes(notes: Notes): string {
	if (notes.moyenne === null) return "Pas encore noté";

	const nombre = `${notes.nombre} note${notes.nombre > 1 ? "s" : ""}`;
	const perso = notes.maNote === null ? "" : `, ta note : ${notes.maNote}`;

	return `Note moyenne ${formaterMoyenne(notes.moyenne)} sur 5 (${nombre})${perso}`;
}
