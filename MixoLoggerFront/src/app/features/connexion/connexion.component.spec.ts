import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import AuthService from "../../core/auth.service";
import ConnexionComponent from "./connexion.component";

describe("ConnexionComponent", () => {
	let fixture: ComponentFixture<ConnexionComponent>;
	let composant: ConnexionComponent;
	let auth: jasmine.SpyObj<AuthService>;
	let navigateByUrl: jasmine.Spy;

	beforeEach(async () => {
		auth = jasmine.createSpyObj<AuthService>("AuthService", ["login"]);

		await TestBed.configureTestingModule({
			imports: [ConnexionComponent],
			providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: AuthService, useValue: auth }]
		}).compileComponents();

		navigateByUrl = spyOn(TestBed.inject(Router), "navigateByUrl").and.resolveTo(true);
		fixture = TestBed.createComponent(ConnexionComponent);
		composant = fixture.componentInstance;
		await fixture.whenStable();
	});

	function saisir(identifiant: string, motDePasse: string): void {
		composant.formulaire.setValue({ identifiant, motDePasse });
	}

	function texte(): string {
		return (fixture.nativeElement as HTMLElement).textContent ?? "";
	}

	function refus(status: number, error: unknown = null): HttpErrorResponse {
		return new HttpErrorResponse({ status, error });
	}

	it("n'appelle pas l'API si un champ est vide, et le dit", async () => {
		saisir("alice", "");
		await composant.seConnecter();
		await fixture.whenStable();

		expect(auth.login).not.toHaveBeenCalled();
		expect(texte()).toContain("Renseigne ton identifiant et ton mot de passe.");
	});

	it("traite un identifiant fait d'espaces comme vide", async () => {
		saisir("   ", "secret");
		await composant.seConnecter();
		await fixture.whenStable();

		expect(auth.login).not.toHaveBeenCalled();
		expect(texte()).toContain("Renseigne ton identifiant et ton mot de passe.");
		expect(fixture.nativeElement.querySelector("#connexion-identifiant").getAttribute("aria-invalid")).toBe("true");
	});

	it("connecte avec l'identifiant sans espaces autour, puis va à la page demandée", async () => {
		auth.login.and.resolveTo({ id: "1", identifiant: "alice", nomAffiche: "Alice" });
		fixture.componentRef.setInput("retour", "/my_bar");
		saisir("  alice ", "secret");

		await composant.seConnecter();

		expect(auth.login).toHaveBeenCalledWith("alice", "secret");
		expect(navigateByUrl).toHaveBeenCalledWith("/my_bar");
		expect(composant.envoiEnCours()).toBeFalse();
	});

	it("ignore une page de retour qui sortirait du site", async () => {
		auth.login.and.resolveTo({ id: "1", identifiant: "alice", nomAffiche: "Alice" });
		fixture.componentRef.setInput("retour", "//ailleurs.example");
		saisir("alice", "secret");

		await composant.seConnecter();

		expect(navigateByUrl).toHaveBeenCalledWith("/cocktails");
	});

	it("sur un refus, affiche l'erreur et efface le seul mot de passe", async () => {
		auth.login.and.rejectWith(refus(401));
		saisir("alice", "faux");

		await composant.seConnecter();
		await fixture.whenStable();

		expect(texte()).toContain("Identifiant ou mot de passe incorrect.");
		expect(composant.formulaire.getRawValue()).toEqual({ identifiant: "alice", motDePasse: "" });
		expect(navigateByUrl).not.toHaveBeenCalled();
	});

	it("donne un message propre à chaque échec", async () => {
		const cas: [HttpErrorResponse | Error, string][] = [
			[refus(429, { detail: "Réessaie dans 42 secondes." }), "Réessaie dans 42 secondes."],
			[refus(429), "Trop de tentatives. Réessaie dans une minute."],
			[refus(0), "Le serveur ne répond pas. Vérifie ta connexion puis réessaie."],
			[refus(500), "La connexion a échoué. Réessaie dans un instant."],
			[new Error("inattendu"), "La connexion a échoué. Réessaie dans un instant."]
		];

		for (const [erreur, message] of cas) {
			auth.login.and.rejectWith(erreur);
			saisir("alice", "secret");

			await composant.seConnecter();

			expect(composant.erreur()).withContext(String(erreur)).toBe(message);
		}
	});
});
