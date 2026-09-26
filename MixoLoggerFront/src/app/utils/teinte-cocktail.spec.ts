import { teinteCocktail } from "./teinte-cocktail";

describe("teinteCocktail", () => {
	it("donne une teinte entière de 0 à 359", () => {
		for (const nom of ["", "Mojito", "Piña Colada", "Old Fashioned", "Espresso Martini", "a".repeat(200)]) {
			const teinte = teinteCocktail(nom);
			expect(Number.isInteger(teinte)).withContext(nom).toBeTrue();
			expect(teinte).withContext(nom).toBeGreaterThanOrEqual(0);
			expect(teinte).withContext(nom).toBeLessThan(360);
		}
	});

	it("est stable d'un appel à l'autre", () => {
		expect(teinteCocktail("Mojito")).toBe(teinteCocktail("Mojito"));
	});

	it("ignore accents, casse et ponctuation", () => {
		expect(teinteCocktail("Piña Colada")).toBe(teinteCocktail("pina  colada"));
		expect(teinteCocktail("Piña Colada")).toBe(teinteCocktail("PINA-COLADA"));
	});

	it("distingue des recettes différentes", () => {
		const teintes = new Set(["Mojito", "Daiquiri", "Margarita", "Negroni", "Caipirinha"].map(teinteCocktail));
		expect(teintes.size).toBe(5);
	});
});
