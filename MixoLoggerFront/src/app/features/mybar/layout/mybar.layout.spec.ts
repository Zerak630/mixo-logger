import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Confirmation, ConfirmationService, MessageService } from "@openng/optimus-ui/api";
import { of, throwError } from "rxjs";
import { LigneStock, MyBar } from "../../../models/bar";
import { UniteVolume } from "../../../models/cocktail";
import { MyBarService } from "../mybar.service";
import MyBarLayout from "./mybar.layout";

const GIN: LigneStock = { id: "g", name: "Gin", niveau: "Pleine", quantity: null, suiviPrecis: false };
const RHUM: LigneStock = { id: "r", name: "Rhum blanc", niveau: "Entamee", quantity: { value: 70, unit: UniteVolume.Centilitre }, suiviPrecis: true };
const BAR: MyBar = { ingredients: [GIN, RHUM] };

describe("MyBarLayout", () => {
	let fixture: ComponentFixture<MyBarLayout>;
	let composant: MyBarLayout;
	let service: jasmine.SpyObj<MyBarService>;
	let messages: jasmine.SpyObj<MessageService>;

	beforeEach(async () => {
		service = jasmine.createSpyObj<MyBarService>("MyBarService", ["getMyBar", "getIngredients", "addIngredient", "setNiveau", "removeIngredient"]);
		service.getIngredients.and.returnValue(of([
			{ id: "g", name: "Gin", aliases: [] },
			{ id: "gb", name: "Bière ginger", aliases: ["ginger beer"] }
		]));
		messages = jasmine.createSpyObj<MessageService>("MessageService", ["add"]);

		await TestBed.configureTestingModule({
			imports: [MyBarLayout],
			providers: [
				provideZonelessChangeDetection(),
				{ provide: MyBarService, useValue: service },
				{ provide: MessageService, useValue: messages }
			]
		}).compileComponents();

		fixture = TestBed.createComponent(MyBarLayout);
		composant = fixture.componentInstance;
		fixture.componentRef.setInput("bar", BAR);
		await fixture.whenStable();
	});

	function page(): HTMLElement {
		return fixture.nativeElement as HTMLElement;
	}

	it("liste le stock, avec le volume des lignes suivies", () => {
		const lignes = [...page().querySelectorAll(".stock__ligne")].map(l => l.textContent!);

		expect(lignes.length).toBe(2);
		expect(lignes[0]).toContain("Gin");
		expect(lignes[1]).toContain("Rhum blanc");
		expect(lignes[1]).toContain("70 cL");
	});

	it("invite à remplir un bar vide", async () => {
		fixture.componentRef.setInput("bar", { ingredients: [] });
		await fixture.whenStable();

		expect(page().textContent).toContain("Ton bar est vide.");
	});

	it("ne repropose pas à l'ajout ce qui est déjà dans le bar", () => {
		composant.rechercher({ query: "gin", originalEvent: new Event("input") });

		expect(composant.suggestions().map(i => i.name)).toEqual(["Bière ginger"]);
	});

	describe("ajout", () => {
		it("n'envoie rien sans nom", () => {
			composant.formulaire.controls.ingredient.setValue("   ");

			composant.ajouter();

			expect(service.addIngredient).not.toHaveBeenCalled();
		});

		it("ajoute en possession simple, puis vide le formulaire et recharge le référentiel", () => {
			service.addIngredient.and.returnValue(of({ ingredients: [...BAR.ingredients, { ...GIN, id: "v", name: "Vodka" }] }));
			composant.formulaire.patchValue({ ingredient: " Vodka ", niveau: "Entamee" });

			composant.ajouter();

			expect(service.addIngredient).toHaveBeenCalledWith("Vodka", "Entamee", undefined);
			expect(composant.stockList().map(l => l.name)).toContain("Vodka");
			expect(composant.formulaire.controls.ingredient.value).toBeNull();
			expect(service.getIngredients).toHaveBeenCalledTimes(2);
		});

		it("exige un volume positif en suivi précis", () => {
			composant.formulaire.patchValue({ ingredient: "Vodka", suiviPrecis: true, volume: 0 });

			composant.ajouter();

			expect(service.addIngredient).not.toHaveBeenCalled();
			expect(composant.formulaire.controls.volume.hasError("requis")).toBeTrue();
		});

		it("envoie le volume en suivi précis", () => {
			service.addIngredient.and.returnValue(of(BAR));
			composant.formulaire.patchValue({ ingredient: "Vodka", suiviPrecis: true, volume: 1, unite: UniteVolume.Litre });

			composant.ajouter();

			expect(service.addIngredient).toHaveBeenCalledWith("Vodka", "Pleine", { value: 1, unit: UniteVolume.Litre });
		});
	});

	describe("niveau", () => {
		it("ignore la désélection et le niveau inchangé", () => {
			composant.changerNiveau(GIN, null);
			composant.changerNiveau(GIN, "Pleine");

			expect(service.setNiveau).not.toHaveBeenCalled();
		});

		it("applique le bar renvoyé par l'API", () => {
			service.setNiveau.and.returnValue(of({ ingredients: [{ ...GIN, niveau: "PresqueFinie" }, RHUM] }));

			composant.changerNiveau(GIN, "PresqueFinie");

			expect(service.setNiveau).toHaveBeenCalledWith(GIN, "PresqueFinie");
			expect(composant.stockList()[0].niveau).toBe("PresqueFinie");
			expect(composant.estEnCours(GIN)).toBeFalse();
		});

		it("recharge le bar si l'écran était périmé (409)", () => {
			service.setNiveau.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { detail: "Le bar a changé." } })));
			service.getMyBar.and.returnValue(of({ ingredients: [RHUM] }));

			composant.changerNiveau(GIN, "Entamee");

			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "warn", summary: "Bar rechargé" }));
			expect(composant.stockList()).toEqual([RHUM]);
		});

		it("signale une autre erreur sans recharger", () => {
			service.setNiveau.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { detail: "Niveau inconnu." } })));

			composant.changerNiveau(GIN, "Entamee");

			expect(service.getMyBar).not.toHaveBeenCalled();
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "error", detail: "Niveau inconnu." }));
		});
	});

	describe("retrait", () => {
		let confirmation: ConfirmationService;

		beforeEach(() => {
			confirmation = fixture.debugElement.injector.get(ConfirmationService);
			service.removeIngredient.and.returnValue(of({ ingredients: [RHUM] }));
		});

		it("retire directement une ligne en possession simple", () => {
			spyOn(confirmation, "confirm");

			composant.retirer(GIN, new MouseEvent("click"));

			expect(confirmation.confirm).not.toHaveBeenCalled();
			expect(service.removeIngredient).toHaveBeenCalledWith(GIN);
		});

		it("demande confirmation avant de perdre un volume suivi", () => {
			let confirmer!: Confirmation;
			spyOn(confirmation, "confirm").and.callFake((c: Confirmation) => { confirmer = c; return confirmation; });

			composant.retirer(RHUM, new MouseEvent("click"));

			expect(service.removeIngredient).not.toHaveBeenCalled();
			expect(confirmer.message).toContain("70 cL");

			confirmer.accept!();
			expect(service.removeIngredient).toHaveBeenCalledWith(RHUM);
		});
	});
});
