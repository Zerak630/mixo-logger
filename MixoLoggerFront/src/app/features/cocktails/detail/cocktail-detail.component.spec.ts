import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import { Confirmation, ConfirmationService, MessageService } from "@openng/optimus-ui/api";
import { of, Subject, throwError } from "rxjs";
import { PartageService } from "../../../core/partage.service";
import { CocktailDetail, Notes } from "../../../models/cocktail";
import { CocktailsService } from "../cocktails.service";
import CocktailDetailComponent from "./cocktail-detail.component";

const MOJITO: CocktailDetail = {
	id: "42",
	name: "Mojito",
	description: "Frais et mentholé",
	photoUrl: null,
	ingredients: [
		{ ingredientId: "r", name: "Rhum blanc", valeur: 4.5, unite: "cL" },
		{ ingredientId: "m", name: "Menthe", valeur: 6, unite: "feuille" },
		{ ingredientId: "s", name: "Sucre", valeur: 1, unite: "pincee" }
	],
	// Volontairement dans le désordre : l'affichage suit `ordre`.
	etapes: [
		{ ordre: 2, description: "Ajouter le rhum." },
		{ ordre: 1, description: "Piler la menthe." }
	],
	auteur: null,
	modifiable: false,
	notes: { moyenne: 4, nombre: 2, maNote: null }
};

describe("CocktailDetailComponent", () => {
	let fixture: ComponentFixture<CocktailDetailComponent>;
	let composant: CocktailDetailComponent;
	let cocktails: jasmine.SpyObj<CocktailsService>;
	let messages: jasmine.SpyObj<MessageService>;

	beforeEach(async () => {
		cocktails = jasmine.createSpyObj<CocktailsService>("CocktailsService", ["makeCocktail", "noter", "retirerNote", "deleteCocktail"]);
		messages = jasmine.createSpyObj<MessageService>("MessageService", ["add"]);

		await TestBed.configureTestingModule({
			imports: [CocktailDetailComponent],
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				{ provide: CocktailsService, useValue: cocktails },
				{ provide: MessageService, useValue: messages },
				{ provide: PartageService, useValue: jasmine.createSpyObj("PartageService", ["partagerCocktail"]) }
			]
		}).compileComponents();

		fixture = TestBed.createComponent(CocktailDetailComponent);
		composant = fixture.componentInstance;
	});

	async function afficher(cocktail: Partial<CocktailDetail> = {}): Promise<HTMLElement> {
		fixture.componentRef.setInput("cocktail", { ...MOJITO, ...cocktail });
		await fixture.whenStable();
		return fixture.nativeElement as HTMLElement;
	}

	function textes(page: HTMLElement, selecteur: string): string[] {
		return [...page.querySelectorAll(selecteur)].map(e => e.textContent!.trim());
	}

	describe("affichage", () => {
		it("présente l'auteur selon qui regarde", async () => {
			expect((await afficher()).querySelector(".detail__surtitre")!.textContent).toContain("Recette d'origine");
			expect((await afficher({ auteur: "Bob" })).querySelector(".detail__surtitre")!.textContent).toContain("Recette de Bob");
			expect((await afficher({ auteur: "Alice", modifiable: true })).querySelector(".detail__surtitre")!.textContent).toContain("Ta recette");
		});

		it("ne propose modifier et supprimer qu'à l'auteur", async () => {
			expect((await afficher()).textContent).not.toContain("Supprimer");

			const page = await afficher({ modifiable: true });
			expect(page.textContent).toContain("Modifier");
			expect(page.textContent).toContain("Supprimer");
		});

		it("range les étapes dans l'ordre de préparation", async () => {
			expect(textes(await afficher(), ".etapes__etape")).toEqual(["Piler la menthe.", "Ajouter le rhum."]);
		});

		it("montre la photo, décrite pour les lecteurs d'écran, et la masque si elle ne charge pas", async () => {
			// Aucun chargement réel : son échec, asynchrone, rendrait le test instable.
			const src = spyOnProperty(HTMLImageElement.prototype, "src", "set");
			const page = await afficher({ photoUrl: "https://images.example/mojito.jpg" });
			const photo = page.querySelector<HTMLImageElement>("img.detail__photo")!;

			expect(src).toHaveBeenCalledWith("https://images.example/mojito.jpg");
			expect(photo.getAttribute("alt")).toBe("Photo de « Mojito »");

			photo.dispatchEvent(new Event("error"));
			await fixture.whenStable();

			expect(page.querySelector("img.detail__photo")).toBeNull();
		});

		it("n'affiche aucune image sans photo", async () => {
			expect((await afficher()).querySelector("img")).toBeNull();
		});

		it("signale une recette sans étape", async () => {
			expect((await afficher({ etapes: [] })).textContent).toContain("Aucune étape renseignée");
		});
	});

	describe("nombre de verres", () => {
		it("multiplie les doses, avec le bon accord", async () => {
			const page = await afficher();
			expect(textes(page, ".ingredients__dose")).toEqual(["4,5 cL", "6 feuilles", "1 pincée"]);

			await composant.augmenterNbVerres();
			await fixture.whenStable();

			expect(textes(page, ".ingredients__dose")).toEqual(["9 cL", "12 feuilles", "2 pincées"]);
		});

		it("ne descend pas sous un verre", async () => {
			await afficher();
			await composant.baisserNbVerres();

			expect(composant.nbVerres()).toBe(1);
		});

		it("prépare le nombre de verres choisi", async () => {
			cocktails.makeCocktail.and.resolveTo({ ingredients: [] });
			await afficher();
			await composant.augmenterNbVerres();
			await composant.augmenterNbVerres();

			await composant.makeThisCocktail();

			expect(cocktails.makeCocktail).toHaveBeenCalledWith("42", 3);
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({
				severity: "success",
				detail: "3 verres de « Mojito » préparés, ton bar est à jour."
			}));
		});

		it("relaie le refus de l'API quand le stock ne suffit pas", async () => {
			cocktails.makeCocktail.and.rejectWith(new HttpErrorResponse({ status: 409, error: { detail: "Pas assez de Rhum blanc." } }));
			await afficher();

			await composant.makeThisCocktail();

			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "error", detail: "Pas assez de Rhum blanc." }));
			expect(composant.preparationEnCours()).toBeFalse();
		});
	});

	describe("notes", () => {
		it("enregistre une note et affiche la moyenne à jour", async () => {
			cocktails.noter.and.returnValue(of({ moyenne: 4.3, nombre: 3, maNote: 5 }));
			const page = await afficher();

			composant.noter(5);
			await fixture.whenStable();

			expect(cocktails.noter).toHaveBeenCalledWith("42", 5);
			expect(page.querySelector(".notes__moyenne")!.textContent).toContain("4,3");
			expect(page.querySelector(".notes__moyenne")!.textContent).toContain("(3 notes)");
		});

		it("n'envoie rien si la note ne change pas", async () => {
			await afficher({ notes: { moyenne: 4, nombre: 2, maNote: 4 } });

			composant.noter(4);

			expect(cocktails.noter).not.toHaveBeenCalled();
		});

		it("retire la note quand l'étoile est désélectionnée", async () => {
			cocktails.retirerNote.and.returnValue(of({ moyenne: 4, nombre: 1, maNote: null }));
			await afficher({ notes: { moyenne: 4, nombre: 2, maNote: 4 } });

			composant.noter(null);

			expect(cocktails.retirerNote).toHaveBeenCalledWith("42");
			expect(composant.notes().maNote).toBeNull();
		});

		it("remet les étoiles sur la note enregistrée si l'API refuse", async () => {
			const reponse = new Subject<Notes>();
			cocktails.noter.and.returnValue(reponse);
			const page = await afficher({ notes: { moyenne: 4, nombre: 2, maNote: 4 } });
			const etoilesActives = () => page.querySelectorAll("p-rating .p-rating-option-active").length;
			expect(etoilesActives()).toBe(4);

			// Le clic allume 2 étoiles avant même la réponse de l'API.
			page.querySelectorAll<HTMLElement>("p-rating .p-rating-option")[1].click();
			await fixture.whenStable();

			expect(cocktails.noter).toHaveBeenCalledWith("42", 2);
			expect(etoilesActives()).toBe(2);

			reponse.error(new HttpErrorResponse({ status: 500 }));
			await fixture.whenStable();

			expect(etoilesActives()).toBe(4);
			expect(composant.notationEnCours()).toBeFalse();
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "error", summary: "Note non enregistrée" }));
		});
	});

	describe("suppression", () => {
		let confirmation: ConfirmationService;
		let navigate: jasmine.Spy;

		beforeEach(async () => {
			await afficher({ modifiable: true });
			confirmation = fixture.debugElement.injector.get(ConfirmationService);
			navigate = spyOn(TestBed.inject(Router), "navigate").and.resolveTo(true);
		});

		function supprimerEtConfirmer(): void {
			spyOn(confirmation, "confirm").and.callFake((c: Confirmation) => { c.accept!(); return confirmation; });
			composant.supprimer(new MouseEvent("click"));
		}

		it("ne supprime rien sans confirmation", () => {
			spyOn(confirmation, "confirm");

			composant.supprimer(new MouseEvent("click"));

			expect(cocktails.deleteCocktail).not.toHaveBeenCalled();
		});

		it("supprime après confirmation et revient à la liste", () => {
			cocktails.deleteCocktail.and.returnValue(of(undefined));

			supprimerEtConfirmer();

			expect(cocktails.deleteCocktail).toHaveBeenCalledWith("42");
			expect(navigate).toHaveBeenCalledWith(["/cocktails"]);
		});

		it("reste sur la page et explique un refus", () => {
			cocktails.deleteCocktail.and.returnValue(throwError(() => new HttpErrorResponse({ status: 403, error: { detail: "Seul l'auteur peut supprimer." } })));

			supprimerEtConfirmer();

			expect(navigate).not.toHaveBeenCalled();
			expect(composant.suppressionEnCours()).toBeFalse();
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "error", detail: "Seul l'auteur peut supprimer." }));
		});
	});
});
