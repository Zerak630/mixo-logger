import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import { MessageService } from "@openng/optimus-ui/api";
import { of, throwError } from "rxjs";
import { CocktailDetail } from "../../../models/cocktail";
import { MyBarService } from "../../mybar/mybar.service";
import { CocktailsService } from "../cocktails.service";
import CocktailEditionComponent from "./cocktail-edition.component";

const DAIQUIRI: CocktailDetail = {
	id: "7",
	name: "Daiquiri",
	description: null,
	ingredients: [
		{ ingredientId: "r", name: "Rhum blanc", valeur: 6, unite: "cL" },
		{ ingredientId: "c", name: "Citron vert", valeur: 3, unite: "cL" }
	],
	etapes: [
		{ ordre: 2, description: "Filtrer." },
		{ ordre: 1, description: "Frapper au shaker." }
	],
	auteur: "Alice",
	modifiable: true,
	notes: { moyenne: null, nombre: 0, maNote: null }
};

describe("CocktailEditionComponent", () => {
	let fixture: ComponentFixture<CocktailEditionComponent>;
	let composant: CocktailEditionComponent;
	let cocktails: jasmine.SpyObj<CocktailsService>;
	let navigate: jasmine.Spy;

	beforeEach(async () => {
		cocktails = jasmine.createSpyObj<CocktailsService>("CocktailsService", ["getUnites", "createCocktail", "updateCocktail"]);
		cocktails.getUnites.and.returnValue(of(["mL", "cL", "feuille"]));
		const bar = jasmine.createSpyObj<MyBarService>("MyBarService", ["getIngredients"]);
		bar.getIngredients.and.returnValue(of([
			{ id: "r", name: "Rhum blanc", aliases: ["rhum", "white rum"] },
			{ id: "m", name: "Menthe", aliases: [] }
		]));

		await TestBed.configureTestingModule({
			imports: [CocktailEditionComponent],
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				{ provide: CocktailsService, useValue: cocktails },
				{ provide: MyBarService, useValue: bar },
				{ provide: MessageService, useValue: jasmine.createSpyObj("MessageService", ["add"]) }
			]
		}).compileComponents();

		navigate = spyOn(TestBed.inject(Router), "navigate").and.resolveTo(true);
		fixture = TestBed.createComponent(CocktailEditionComponent);
		composant = fixture.componentInstance;
	});

	async function ouvrir(cocktail?: CocktailDetail): Promise<void> {
		if (cocktail) fixture.componentRef.setInput("cocktail", cocktail);
		await fixture.whenStable();
	}

	/** Remplit une recette minimale valide, sur la ligne et l'étape proposées d'office. */
	function remplirRecette(): void {
		composant.formulaire.controls.name.setValue("  Mojito ");
		composant.ingredients.at(0).setValue({ ingredient: "Rhum blanc", valeur: 4.5, unite: "cL" });
		composant.etapes.at(0).setValue(" Piler la menthe. ");
	}

	describe("création", () => {
		beforeEach(() => ouvrir());

		it("propose d'emblée une ligne d'ingrédient et une étape", () => {
			expect(composant.enEdition()).toBeFalse();
			expect(composant.ingredients.length).toBe(1);
			expect(composant.etapes.length).toBe(1);
		});

		it("libelle les unités proposées par l'API", () => {
			expect(composant.optionsUniteLibellees()).toEqual([
				{ label: "mL", value: "mL" },
				{ label: "cL", value: "cL" },
				{ label: "feuille(s)", value: "feuille" }
			]);
		});

		it("n'envoie pas une recette incomplète", () => {
			composant.enregistrer();

			expect(cocktails.createCocktail).not.toHaveBeenCalled();
			expect(composant.tentative()).toBeTrue();
		});

		it("refuse un nom fait d'espaces", () => {
			remplirRecette();
			composant.formulaire.controls.name.setValue("   ");

			composant.enregistrer();

			expect(cocktails.createCocktail).not.toHaveBeenCalled();
		});

		it("envoie une recette nettoyée puis ouvre son détail", () => {
			cocktails.createCocktail.and.returnValue(of({ ...DAIQUIRI, id: "99", name: "Mojito" }));
			remplirRecette();
			composant.ajouterIngredient("Menthe", 6, "feuille");

			composant.enregistrer();

			expect(cocktails.createCocktail).toHaveBeenCalledWith({
				name: "Mojito",
				description: null,
				ingredients: [
					{ name: "Rhum blanc", valeur: 4.5, unite: "cL" },
					{ name: "Menthe", valeur: 6, unite: "feuille" }
				],
				etapes: ["Piler la menthe."]
			});
			expect(navigate).toHaveBeenCalledWith(["/cocktails", "99"]);
		});

		it("accepte un ingrédient choisi dans l'autocomplétion", () => {
			cocktails.createCocktail.and.returnValue(of(DAIQUIRI));
			remplirRecette();
			composant.ingredients.at(0).controls.ingredient.setValue({ id: "m", name: "Menthe", aliases: [] });

			composant.enregistrer();

			expect(cocktails.createCocktail.calls.mostRecent().args[0].ingredients[0].name).toBe("Menthe");
		});

		it("repère un ingrédient saisi deux fois, alias compris, et bloque l'envoi", () => {
			remplirRecette();
			composant.ajouterIngredient("white rum", 2, "cL");

			expect(composant.doublons()).toEqual(new Set([1]));
			composant.enregistrer();
			expect(cocktails.createCocktail).not.toHaveBeenCalled();
		});

		it("affiche l'erreur de l'API et reste sur le formulaire", () => {
			cocktails.createCocktail.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { detail: "Une recette porte déjà ce nom." } })));
			remplirRecette();

			composant.enregistrer();

			expect(composant.erreurServeur()).toBe("Une recette porte déjà ce nom.");
			expect(composant.enregistrementEnCours()).toBeFalse();
			expect(navigate).not.toHaveBeenCalled();
		});

		it("ne repropose pas dans une ligne un ingrédient déjà présent dans une autre", () => {
			composant.ingredients.at(0).controls.ingredient.setValue("rhum");
			composant.ajouterIngredient();

			composant.rechercher({ query: "", originalEvent: new Event("input") }, 1);

			expect(composant.suggestions().map(i => i.name)).toEqual(["Menthe"]);
		});

		it("déplace les étapes sans sortir des bornes", () => {
			composant.etapes.at(0).setValue("A");
			composant.ajouterEtape("B");
			composant.ajouterEtape("C");

			composant.deplacerEtape(2, -1);
			expect(composant.etapes.getRawValue()).toEqual(["A", "C", "B"]);

			composant.deplacerEtape(0, -1);
			composant.deplacerEtape(2, 1);
			expect(composant.etapes.getRawValue()).toEqual(["A", "C", "B"]);
		});
	});

	describe("édition", () => {
		it("pré-remplit le formulaire, étapes dans l'ordre", async () => {
			await ouvrir(DAIQUIRI);

			expect(composant.enEdition()).toBeTrue();
			expect(composant.formulaire.controls.name.value).toBe("Daiquiri");
			expect(composant.ingredients.getRawValue().map(l => l.ingredient)).toEqual(["Rhum blanc", "Citron vert"]);
			expect(composant.etapes.getRawValue()).toEqual(["Frapper au shaker.", "Filtrer."]);
		});

		it("enregistre par une modification, pas une création", async () => {
			cocktails.updateCocktail.and.returnValue(of(DAIQUIRI));
			await ouvrir(DAIQUIRI);

			composant.enregistrer();

			expect(cocktails.createCocktail).not.toHaveBeenCalled();
			expect(cocktails.updateCocktail).toHaveBeenCalledWith("7", jasmine.objectContaining({ name: "Daiquiri" }));
			expect(navigate).toHaveBeenCalledWith(["/cocktails", "7"]);
		});

		it("remplace le formulaire par une explication pour la recette d'un autre", async () => {
			await ouvrir({ ...DAIQUIRI, modifiable: false });

			const page = fixture.nativeElement as HTMLElement;
			expect(composant.interdit()).toBeTrue();
			expect(page.querySelector("form")).toBeNull();
		});
	});
});
