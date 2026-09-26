import { DOCUMENT, provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MessageService } from "@openng/optimus-ui/api";
import { PartageService } from "./partage.service";

const COCKTAIL = { id: "42", name: "Mojito" };
const LIEN = "https://mixo.test/cocktails/42";

interface Environnement {
	tactile: boolean;
	share?: jasmine.Spy;
	writeText: jasmine.Spy;
}

describe("PartageService", () => {
	let messages: jasmine.SpyObj<MessageService>;

	function service({ tactile, share, writeText }: Environnement): PartageService {
		const documentFactice = {
			baseURI: "https://mixo.test/",
			defaultView: {
				matchMedia: () => ({ matches: tactile }),
				navigator: { share, clipboard: { writeText } }
			}
		};
		messages = jasmine.createSpyObj<MessageService>("MessageService", ["add"]);

		TestBed.configureTestingModule({
			providers: [
				provideZonelessChangeDetection(),
				{ provide: DOCUMENT, useValue: documentFactice },
				{ provide: MessageService, useValue: messages }
			]
		});
		return TestBed.inject(PartageService);
	}

	it("sur ordinateur, copie le lien même si le partage système existe", async () => {
		const share = jasmine.createSpy("share").and.resolveTo();
		const writeText = jasmine.createSpy("writeText").and.resolveTo();

		await service({ tactile: false, share, writeText }).partagerCocktail(COCKTAIL);

		expect(share).not.toHaveBeenCalled();
		expect(writeText).toHaveBeenCalledWith(LIEN);
		expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "success", summary: "Lien copié" }));
	});

	it("sur un appareil tactile, ouvre la feuille de partage du système", async () => {
		const share = jasmine.createSpy("share").and.resolveTo();
		const writeText = jasmine.createSpy("writeText");

		await service({ tactile: true, share, writeText }).partagerCocktail(COCKTAIL);

		expect(share).toHaveBeenCalledWith(jasmine.objectContaining({ title: "Mojito", url: LIEN }));
		expect(writeText).not.toHaveBeenCalled();
		expect(messages.add).not.toHaveBeenCalled();
	});

	it("ne fait rien de plus si l'utilisateur ferme la feuille de partage", async () => {
		const share = jasmine.createSpy("share").and.rejectWith(new DOMException("fermée", "AbortError"));
		const writeText = jasmine.createSpy("writeText");

		await service({ tactile: true, share, writeText }).partagerCocktail(COCKTAIL);

		expect(writeText).not.toHaveBeenCalled();
		expect(messages.add).not.toHaveBeenCalled();
	});

	it("retombe sur la copie si le partage système échoue", async () => {
		const share = jasmine.createSpy("share").and.rejectWith(new DOMException("refusé", "NotAllowedError"));
		const writeText = jasmine.createSpy("writeText").and.resolveTo();

		await service({ tactile: true, share, writeText }).partagerCocktail(COCKTAIL);

		expect(writeText).toHaveBeenCalledWith(LIEN);
	});

	it("donne le lien à copier à la main si le presse-papiers est refusé", async () => {
		const writeText = jasmine.createSpy("writeText").and.rejectWith(new Error("refusé"));

		await service({ tactile: false, writeText }).partagerCocktail(COCKTAIL);

		expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({
			severity: "warn",
			detail: jasmine.stringContaining(LIEN)
		}));
	});
});
