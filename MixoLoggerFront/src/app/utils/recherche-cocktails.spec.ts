import { IngredientReference } from "../models/bar";
import { CocktailResume } from "../models/cocktail";
import { CriteresCocktails, filtrerCocktails } from "./recherche-cocktails";

function cocktail(name: string, champs: Partial<CocktailResume> = {}): CocktailResume {
	return {
		id: name,
		name,
		description: null,
		photoUrl: null,
		ingredients: [],
		realisable: false,
		manques: [],
		modifiable: false,
		notes: { moyenne: null, nombre: 0, maNote: null },
		...champs
	};
}

const MOJITO = cocktail("Mojito", {
	description: "Rafraîchissant, à la menthe",
	ingredients: ["Rhum blanc", "Menthe", "Citron vert", "Sucre de canne"],
	realisable: true,
	notes: { moyenne: 4.5, nombre: 2, maNote: null }
});
const DAIQUIRI = cocktail("Daiquiri", {
	ingredients: ["Rhum blanc", "Citron vert", "Sirop de sucre"],
	realisable: true,
	modifiable: true,
	notes: { moyenne: 3, nombre: 1, maNote: 3 }
});
const NEGRONI = cocktail("Negroni", {
	description: "Amer et équilibré",
	ingredients: ["Gin", "Campari", "Vermouth rouge"]
});
const PINA_COLADA = cocktail("Piña Colada", {
	ingredients: ["Rhum blanc", "Crème de coco", "Jus d'ananas"],
	modifiable: true,
	notes: { moyenne: 4, nombre: 3, maNote: null }
});

const LISTE = [MOJITO, DAIQUIRI, NEGRONI, PINA_COLADA];

const REFERENTIEL: IngredientReference[] = [
	{ id: "1", name: "Rhum blanc", aliases: ["rhum", "white rum"] },
	{ id: "2", name: "Citron vert", aliases: ["lime"] }
];

const AUCUN: CriteresCocktails = { texte: "", seulementRealisables: false, seulementMesRecettes: false, noteMinimale: null };

function filtrer(criteres: Partial<CriteresCocktails>, referentiel = REFERENTIEL): string[] {
	return filtrerCocktails(LISTE, { ...AUCUN, ...criteres }, referentiel).map(c => c.name);
}

describe("filtrerCocktails", () => {
	it("sans critère, rend toute la liste dans son ordre", () => {
		expect(filtrer({})).toEqual(["Mojito", "Daiquiri", "Negroni", "Piña Colada"]);
	});

	it("un texte fait seulement d'espaces ne filtre rien", () => {
		expect(filtrer({ texte: "   " })).toEqual(["Mojito", "Daiquiri", "Negroni", "Piña Colada"]);
	});

	describe("recherche texte", () => {
		it("cherche dans le nom, sans accents ni casse", () => {
			expect(filtrer({ texte: "PINA" })).toEqual(["Piña Colada"]);
			expect(filtrer({ texte: "negr" })).toEqual(["Negroni"]);
		});

		it("cherche dans la description", () => {
			expect(filtrer({ texte: "equilibre" })).toEqual(["Negroni"]);
		});

		it("cherche dans les ingrédients", () => {
			expect(filtrer({ texte: "campari" })).toEqual(["Negroni"]);
			expect(filtrer({ texte: "coco" })).toEqual(["Piña Colada"]);
		});

		it("exige chaque mot, où qu'il soit trouvé", () => {
			// « rhum » dans les ingrédients de trois recettes, « menthe » seulement dans le Mojito.
			expect(filtrer({ texte: "rhum menthe" })).toEqual(["Mojito"]);
			expect(filtrer({ texte: "rhum citron" })).toEqual(["Mojito", "Daiquiri"]);
			expect(filtrer({ texte: "rhum campari" })).toEqual([]);
		});

		it("trouve un ingrédient par un alias du référentiel", () => {
			expect(filtrer({ texte: "white rum" })).toEqual(["Mojito", "Daiquiri", "Piña Colada"]);
			expect(filtrer({ texte: "lime" })).toEqual(["Mojito", "Daiquiri"]);
		});

		it("reste utilisable sans référentiel, sur les seuls noms", () => {
			expect(filtrer({ texte: "white rum" }, [])).toEqual([]);
			expect(filtrer({ texte: "rhum" }, [])).toEqual(["Mojito", "Daiquiri", "Piña Colada"]);
		});
	});

	it("« Seulement ce que je peux faire » ne garde que les réalisables", () => {
		expect(filtrer({ seulementRealisables: true })).toEqual(["Mojito", "Daiquiri"]);
	});

	it("« Mes recettes » ne garde que celles de l'utilisateur", () => {
		expect(filtrer({ seulementMesRecettes: true })).toEqual(["Daiquiri", "Piña Colada"]);
	});

	it("la note minimale est inclusive et exclut les recettes sans note", () => {
		expect(filtrer({ noteMinimale: 4 })).toEqual(["Mojito", "Piña Colada"]);
		expect(filtrer({ noteMinimale: 4.5 })).toEqual(["Mojito"]);
		expect(filtrer({ noteMinimale: 3 })).not.toContain("Negroni");
	});

	it("cumule tous les critères", () => {
		expect(filtrer({ texte: "rhum", seulementRealisables: true, seulementMesRecettes: true })).toEqual(["Daiquiri"]);
		expect(filtrer({ texte: "rhum", seulementMesRecettes: true, noteMinimale: 4 })).toEqual(["Piña Colada"]);
	});

	it("ne modifie pas la liste reçue", () => {
		const copie = [...LISTE];
		filtrerCocktails(LISTE, { ...AUCUN, texte: "rhum" }, REFERENTIEL);
		expect(LISTE).toEqual(copie);
	});
});
