import { libelleDose, libelleUnite } from "./libelle-dose";

describe("libelleUnite", () => {
	it("marque le pluriel possible des décomptes", () => {
		expect(libelleUnite("feuille")).toBe("feuille(s)");
		expect(libelleUnite("pincee")).toBe("pincée(s)");
	});

	it("laisse les unités de volume telles quelles", () => {
		expect(libelleUnite("cL")).toBe("cL");
		expect(libelleUnite("L")).toBe("L");
	});
});

describe("libelleDose", () => {
	it("écrit les volumes avec une virgule décimale", () => {
		expect(libelleDose(4.5, "cL")).toBe("4,5 cL");
		expect(libelleDose(6, "cL")).toBe("6 cL");
	});

	it("arrondit au centième", () => {
		expect(libelleDose(1 / 3, "cL")).toBe("0,33 cL");
	});

	it("accorde les décomptes, le pluriel commençant à 2", () => {
		expect(libelleDose(1, "trait")).toBe("1 trait");
		expect(libelleDose(1.5, "pincee")).toBe("1,5 pincée");
		expect(libelleDose(2, "piece")).toBe("2 pièces");
		expect(libelleDose(6, "feuille")).toBe("6 feuilles");
	});
});
