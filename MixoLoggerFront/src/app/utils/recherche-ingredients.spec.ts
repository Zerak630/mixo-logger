import { IngredientReference } from "../models/bar";
import { cleIngredient, rechercherIngredients } from "./recherche-ingredients";

function ingredient(name: string, aliases: string[] = []): IngredientReference {
	return { id: name, name, aliases };
}

const REFERENTIEL: IngredientReference[] = [
	ingredient("Bière ginger", ["ginger beer"]),
	ingredient("Gin"),
	ingredient("Rhum blanc", ["rhum", "white rum"]),
	ingredient("Rhum ambré", ["dark rum"]),
	ingredient("Menthe", ["feuilles de menthe"]),
	ingredient("Crème de coco")
];

function noms(resultat: IngredientReference[]): string[] {
	return resultat.map(i => i.name);
}

describe("rechercherIngredients", () => {
	it("place le début de nom avant un mot du nom, lui-même avant un alias", () => {
		// « Gin » commence par « gin », « Bière ginger » a un mot qui commence par « gin ».
		expect(noms(rechercherIngredients(REFERENTIEL, "gin"))).toEqual(["Gin", "Bière ginger"]);
		// « Rhum blanc » : par le nom ; « Rhum ambré » : par l'alias « dark rum ».
		expect(noms(rechercherIngredients(REFERENTIEL, "blanc"))).toEqual(["Rhum blanc"]);
		expect(noms(rechercherIngredients(REFERENTIEL, "rum"))).toEqual(["Rhum ambré", "Rhum blanc"]);
	});

	it("place un mot du nom avant un alias, même contre l'ordre alphabétique", () => {
		const referentiel = [ingredient("Cassonade", ["sucre roux"]), ingredient("Sirop de sucre")];

		expect(noms(rechercherIngredients(referentiel, "sucre"))).toEqual(["Sirop de sucre", "Cassonade"]);
	});

	it("classe par ordre alphabétique à rang égal", () => {
		expect(noms(rechercherIngredients(REFERENTIEL, "rhum"))).toEqual(["Rhum ambré", "Rhum blanc"]);
	});

	it("ignore accents et casse", () => {
		expect(noms(rechercherIngredients(REFERENTIEL, "CREME"))).toEqual(["Crème de coco"]);
		expect(noms(rechercherIngredients(REFERENTIEL, "biere"))).toEqual(["Bière ginger"]);
	});

	it("cherche dans les alias au milieu du texte", () => {
		expect(noms(rechercherIngredients(REFERENTIEL, "feuilles"))).toEqual(["Menthe"]);
	});

	it("écarte les exclus, désignés par leur nom normalisé", () => {
		expect(noms(rechercherIngredients(REFERENTIEL, "rhum", new Set(["rhum blanc"])))).toEqual(["Rhum ambré"]);
	});

	it("propose tout le référentiel à une requête vide, dans la limite du maximum", () => {
		expect(rechercherIngredients(REFERENTIEL, "").length).toBe(REFERENTIEL.length);
		expect(noms(rechercherIngredients(REFERENTIEL, "", new Set(), 2))).toEqual(["Bière ginger", "Crème de coco"]);
	});

	it("ne propose rien sans correspondance", () => {
		expect(rechercherIngredients(REFERENTIEL, "tequila")).toEqual([]);
	});
});

describe("cleIngredient", () => {
	it("ramène un alias au nom canonique normalisé", () => {
		expect(cleIngredient(REFERENTIEL, "White Rum")).toBe("rhum blanc");
		expect(cleIngredient(REFERENTIEL, "Rhum blanc")).toBe("rhum blanc");
	});

	it("garde un nom inconnu tel quel, normalisé", () => {
		expect(cleIngredient(REFERENTIEL, "Sirop d'Orgeat")).toBe("sirop d orgeat");
	});
});
