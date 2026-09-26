import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { Confirmation, ConfirmationService, MessageService } from "@openng/optimus-ui/api";
import { PartageService } from "../../core/partage.service";
import { CocktailsService } from "../../features/cocktails/cocktails.service";
import { CocktailResume } from "../../models/cocktail";
import { CocktailCardComponent } from "./cocktail-card.component";

const MOJITO: CocktailResume = {
	id: "42",
	name: "Mojito",
	description: null,
	ingredients: ["Rhum blanc", "Menthe", "Citron vert"],
	realisable: true,
	manques: [],
	modifiable: false,
	notes: { moyenne: null, nombre: 0, maNote: null }
};

describe("CocktailCardComponent", () => {
	let fixture: ComponentFixture<CocktailCardComponent>;
	let cocktails: jasmine.SpyObj<CocktailsService>;
	let messages: jasmine.SpyObj<MessageService>;
	let confirmation: ConfirmationService;
	let prepare: jasmine.Spy;

	beforeEach(async () => {
		cocktails = jasmine.createSpyObj<CocktailsService>("CocktailsService", ["makeCocktail"]);
		messages = jasmine.createSpyObj<MessageService>("MessageService", ["add"]);

		await TestBed.configureTestingModule({
			imports: [CocktailCardComponent],
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				ConfirmationService,
				{ provide: CocktailsService, useValue: cocktails },
				{ provide: MessageService, useValue: messages },
				{ provide: PartageService, useValue: jasmine.createSpyObj("PartageService", ["partagerCocktail"]) }
			]
		}).compileComponents();

		confirmation = TestBed.inject(ConfirmationService);
		fixture = TestBed.createComponent(CocktailCardComponent);
		prepare = jasmine.createSpy("prepare");
		fixture.componentInstance.prepare.subscribe(prepare);
	});

	async function afficher(cocktail: Partial<CocktailResume> = {}): Promise<HTMLElement> {
		fixture.componentRef.setInput("cocktail", { ...MOJITO, ...cocktail });
		await fixture.whenStable();
		return fixture.nativeElement as HTMLElement;
	}

	function boutonRealiser(carte: HTMLElement): HTMLButtonElement {
		return [...carte.querySelectorAll("button")].find(b => b.textContent?.includes("Réaliser"))!;
	}

	it("lie le nom au détail de la recette", async () => {
		const lien = (await afficher()).querySelector<HTMLAnchorElement>("a.title__lien")!;

		expect(lien.textContent?.trim()).toBe("Mojito");
		expect(lien.getAttribute("href")).toBe("/cocktails/42");
	});

	it("signale une recette réalisable et permet de la préparer", async () => {
		const carte = await afficher();

		expect(carte.textContent).toContain("Réalisable");
		expect(boutonRealiser(carte).disabled).toBeFalse();
	});

	it("détaille ce qui manque et empêche la préparation", async () => {
		const carte = await afficher({
			realisable: false,
			manques: [{ ingredient: "Menthe", raison: "Absent" }, { ingredient: "Rhum blanc", raison: "Insuffisant" }]
		});

		expect(carte.textContent).toContain("Il manque 2 ingrédients");
		expect(carte.textContent).toContain("Il manque : Menthe, Rhum blanc (pas assez)");
		expect(boutonRealiser(carte).disabled).toBeTrue();
	});

	it("accorde « ingrédient » au singulier", async () => {
		const carte = await afficher({ realisable: false, manques: [{ ingredient: "Menthe", raison: "Absent" }] });

		expect(carte.textContent).toContain("Il manque 1 ingrédient");
		expect(carte.textContent).not.toContain("Il manque 1 ingrédients");
	});

	it("n'affiche la note moyenne que si la recette est notée", async () => {
		expect((await afficher()).querySelector(".badge--note")).toBeNull();

		const note = (await afficher({ notes: { moyenne: 4.3, nombre: 3, maNote: null } })).querySelector(".badge--note")!;
		expect(note.textContent).toContain("4,3");
		expect(note.getAttribute("title")).toBe("Note moyenne 4,3 sur 5 (3 notes)");
	});

	describe("Réaliser", () => {
		let accepter: () => Promise<void>;

		beforeEach(async () => {
			spyOn(confirmation, "confirm").and.callFake((c: Confirmation) => {
				accepter = async () => { c.accept!(); await new Promise(resolve => setTimeout(resolve)); };
				return confirmation;
			});
			boutonRealiser(await afficher()).click();
		});

		it("demande confirmation avant de toucher au stock", () => {
			expect(confirmation.confirm).toHaveBeenCalled();
			expect(cocktails.makeCocktail).not.toHaveBeenCalled();
		});

		it("prépare un verre, le confirme et demande le rechargement de la liste", async () => {
			cocktails.makeCocktail.and.resolveTo({ ingredients: [] });

			await accepter();

			expect(cocktails.makeCocktail).toHaveBeenCalledWith("42", 1);
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "success" }));
			expect(prepare).toHaveBeenCalled();
			expect(fixture.componentInstance.preparationEnCours()).toBeFalse();
		});

		it("affiche le refus de l'API et recharge quand même la liste", async () => {
			cocktails.makeCocktail.and.rejectWith(new HttpErrorResponse({ status: 409, error: { detail: "Il manque de la Menthe." } }));

			await accepter();

			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "error", detail: "Il manque de la Menthe." }));
			expect(prepare).toHaveBeenCalled();
		});
	});
});
