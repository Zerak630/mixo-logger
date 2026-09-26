import { formaterMoyenne, libelleNotes } from "./libelle-notes";

describe("formaterMoyenne", () => {
	it("affiche toujours une décimale, avec une virgule", () => {
		expect(formaterMoyenne(4)).toBe("4,0");
		expect(formaterMoyenne(4.3)).toBe("4,3");
	});

	it("rend une chaîne vide sans note", () => {
		expect(formaterMoyenne(null)).toBe("");
	});
});

describe("libelleNotes", () => {
	it("signale une recette pas encore notée", () => {
		expect(libelleNotes({ moyenne: null, nombre: 0, maNote: null })).toBe("Pas encore noté");
	});

	it("donne la moyenne, le nombre de notes et la note personnelle", () => {
		expect(libelleNotes({ moyenne: 4.3, nombre: 3, maNote: 5 }))
			.toBe("Note moyenne 4,3 sur 5 (3 notes), ta note : 5");
	});

	it("accorde « note » au singulier et omet la note personnelle absente", () => {
		expect(libelleNotes({ moyenne: 5, nombre: 1, maNote: null }))
			.toBe("Note moyenne 5,0 sur 5 (1 note)");
	});
});
