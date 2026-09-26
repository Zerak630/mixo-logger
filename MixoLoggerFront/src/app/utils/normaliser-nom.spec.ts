import { normaliserNom } from "./normaliser-nom";

describe("normaliserNom", () => {
	it("retire les accents et passe en minuscules", () => {
		expect(normaliserNom("Crème de Coco")).toBe("creme de coco");
		expect(normaliserNom("PIÑA COLADA")).toBe("pina colada");
	});

	it("réduit toute suite de ponctuation ou d'espaces à une espace", () => {
		expect(normaliserNom("  creme  de   coco ")).toBe("creme de coco");
		expect(normaliserNom("Ginger-beer")).toBe("ginger beer");
		expect(normaliserNom("Sirop (sucre de canne) !")).toBe("sirop sucre de canne");
		expect(normaliserNom("Jus d'orange")).toBe("jus d orange");
	});

	it("garde les chiffres", () => {
		expect(normaliserNom("7 Up")).toBe("7 up");
	});

	it("rend une chaîne vide pour une saisie sans lettre ni chiffre", () => {
		expect(normaliserNom("")).toBe("");
		expect(normaliserNom(" - ! ")).toBe("");
	});
});
