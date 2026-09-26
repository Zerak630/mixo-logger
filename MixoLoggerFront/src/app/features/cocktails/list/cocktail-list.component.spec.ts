import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { MessageService } from "@openng/optimus-ui/api";
import { of } from "rxjs";
import { CocktailResume } from "../../../models/cocktail";
import { MyBarService } from "../../mybar/mybar.service";
import { CocktailsService } from "../cocktails.service";
import CocktailListComponent from "./cocktail-list.component";

function cocktail(id: string, name: string, champs: Partial<CocktailResume> = {}): CocktailResume {
	return {
		id, name, description: null, photoUrl: null, ingredients: [], realisable: false, manques: [], modifiable: false,
		notes: { moyenne: null, nombre: 0, maNote: null },
		...champs
	};
}

const LISTE = [
	cocktail("1", "Mojito", { realisable: true, ingredients: ["Rhum blanc", "Menthe"] }),
	cocktail("2", "Daiquiri", { realisable: true, ingredients: ["Rhum blanc"], modifiable: true }),
	cocktail("3", "Negroni", { ingredients: ["Gin", "Campari"] })
];

describe("CocktailListComponent", () => {
	let fixture: ComponentFixture<CocktailListComponent>;
	let composant: CocktailListComponent;
	let cocktails: jasmine.SpyObj<CocktailsService>;

	beforeEach(async () => {
		cocktails = jasmine.createSpyObj<CocktailsService>("CocktailsService", ["getCocktails"]);
		const bar = jasmine.createSpyObj<MyBarService>("MyBarService", ["getIngredients"]);
		bar.getIngredients.and.returnValue(of([{ id: "r", name: "Rhum blanc", aliases: ["white rum"] }]));

		await TestBed.configureTestingModule({
			imports: [CocktailListComponent],
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				MessageService,
				{ provide: CocktailsService, useValue: cocktails },
				{ provide: MyBarService, useValue: bar }
			]
		}).compileComponents();

		fixture = TestBed.createComponent(CocktailListComponent);
		composant = fixture.componentInstance;
		fixture.componentRef.setInput("cocktails", LISTE);
		await fixture.whenStable();
	});

	function page(): HTMLElement {
		return fixture.nativeElement as HTMLElement;
	}

	function noms(): string[] {
		return [...page().querySelectorAll("cocktail-card .title__lien")].map(lien => lien.textContent!.trim());
	}

	function compte(): string {
		return page().querySelector(".entete__compte")!.textContent!.replace(/\s+/g, " ").trim();
	}

	it("affiche toutes les recettes et le nombre de réalisables", () => {
		expect(noms()).toEqual(["Mojito", "Daiquiri", "Negroni"]);
		expect(compte()).toBe("2 cocktails réalisables sur 3");
	});

	it("signale l'absence de recette réalisable", async () => {
		fixture.componentRef.setInput("cocktails", [LISTE[2]]);
		await fixture.whenStable();

		expect(compte()).toBe("Aucun cocktail réalisable avec ton bar");
	});

	it("filtre à la saisie et compte les résultats", async () => {
		composant.texte.set("white rum");
		await fixture.whenStable();

		expect(noms()).toEqual(["Mojito", "Daiquiri"]);
		expect(compte()).toBe("2 cocktails sur 3");
	});

	it("propose de compléter le bar quand le seul filtre « réalisable » ne laisse rien", async () => {
		fixture.componentRef.setInput("cocktails", [LISTE[2]]);
		composant.seulementRealisables.set(true);
		await fixture.whenStable();

		expect(page().querySelector<HTMLAnchorElement>(".vide a")?.getAttribute("href")).toBe("/my_bar");
	});

	it("propose de réinitialiser des filtres qui ne laissent rien", async () => {
		composant.texte.set("tequila");
		composant.seulementMesRecettes.set(true);
		composant.noteMinimale.set(4);
		await fixture.whenStable();
		expect(noms()).toEqual([]);

		page().querySelector<HTMLButtonElement>(".vide__lien")!.click();
		await fixture.whenStable();

		expect(composant.filtresActifs()).toBeFalse();
		expect(noms()).toEqual(["Mojito", "Daiquiri", "Negroni"]);
	});

	it("recharge la liste après une préparation, filtres conservés", async () => {
		composant.seulementRealisables.set(true);
		cocktails.getCocktails.and.returnValue(of([{ ...LISTE[0], realisable: false }, LISTE[1], LISTE[2]]));

		composant.recharger();
		await fixture.whenStable();

		expect(noms()).toEqual(["Daiquiri"]);
	});
});
