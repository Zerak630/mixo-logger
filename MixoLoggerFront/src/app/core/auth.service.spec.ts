import { HttpErrorResponse, provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { Utilisateur } from "../models/utilisateur";
import AuthService from "./auth.service";
import UserService from "./user.service";

const ALICE: Utilisateur = { id: "1", identifiant: "alice", nomAffiche: "Alice" };

describe("AuthService", () => {
	let service: AuthService;
	let userService: UserService;
	let http: HttpTestingController;

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()]
		});
		service = TestBed.inject(AuthService);
		userService = TestBed.inject(UserService);
		http = TestBed.inject(HttpTestingController);
	});

	afterEach(() => http.verify());

	describe("login", () => {
		it("envoie les identifiants et retient l'utilisateur renvoyé", async () => {
			const connexion = service.login("alice", "secret");

			const requete = http.expectOne("/Auth/connexion");
			expect(requete.request.method).toBe("POST");
			expect(requete.request.body).toEqual({ identifiant: "alice", motDePasse: "secret" });
			requete.flush(ALICE);

			expect(await connexion).toEqual(ALICE);
			expect(userService.utilisateur()).toEqual(ALICE);
		});

		it("rejette avec l'erreur de l'API sans ouvrir de session", async () => {
			const connexion = service.login("alice", "faux");

			http.expectOne("/Auth/connexion").flush(null, { status: 401, statusText: "Unauthorized" });

			await expectAsync(connexion).toBeRejectedWith(jasmine.objectContaining({ status: 401 }));
			expect(userService.isConnected()).toBeFalse();
		});
	});

	describe("logout", () => {
		it("ferme la session côté API et côté front", async () => {
			userService.setUser(ALICE);
			const deconnexion = service.logout();

			const requete = http.expectOne("/Auth/deconnexion");
			expect(requete.request.method).toBe("POST");
			requete.flush(null);
			await deconnexion;

			expect(userService.isConnected()).toBeFalse();
		});

		it("vide la session locale même si l'API échoue", async () => {
			userService.setUser(ALICE);
			const deconnexion = service.logout();

			http.expectOne("/Auth/deconnexion").error(new ProgressEvent("error"));

			await expectAsync(deconnexion).toBeRejectedWith(jasmine.any(HttpErrorResponse));
			expect(userService.isConnected()).toBeFalse();
		});
	});

	describe("compte", () => {
		it("modifie l'identifiant et le nom, et met à jour l'utilisateur connecté", async () => {
			userService.setUser(ALICE);
			const modification = service.modifierCompte("alicia", "Alicia");

			const requete = http.expectOne("/compte");
			expect(requete.request.method).toBe("PUT");
			expect(requete.request.body).toEqual({ identifiant: "alicia", nomAffiche: "Alicia" });
			requete.flush({ ...ALICE, identifiant: "alicia", nomAffiche: "Alicia" });
			await modification;

			expect(userService.utilisateur()?.identifiant).toBe("alicia");
			expect(userService.initiales()).toBe("A");
		});

		it("change le mot de passe sans toucher à la session locale", async () => {
			userService.setUser(ALICE);
			const changement = service.changerMotDePasse("ancien-mot-de-passe", "nouveau-mot-de-passe");

			const requete = http.expectOne("/compte/mot-de-passe");
			expect(requete.request.method).toBe("PUT");
			expect(requete.request.body).toEqual({ actuel: "ancien-mot-de-passe", nouveau: "nouveau-mot-de-passe" });
			requete.flush(null, { status: 204, statusText: "No Content" });
			await changement;

			expect(userService.utilisateur()).toEqual(ALICE);
		});
	});

	describe("restaurerSession", () => {
		it("reprend une session encore valide", async () => {
			const restauration = service.restaurerSession();

			http.expectOne("/Auth/moi").flush(ALICE);
			await restauration;

			expect(userService.utilisateur()).toEqual(ALICE);
		});

		it("laisse déconnecté sur un 401, sans avertissement", async () => {
			const avertissement = spyOn(console, "warn");
			const restauration = service.restaurerSession();

			http.expectOne("/Auth/moi").flush(null, { status: 401, statusText: "Unauthorized" });
			await expectAsync(restauration).toBeResolved();

			expect(userService.isConnected()).toBeFalse();
			expect(avertissement).not.toHaveBeenCalled();
		});

		it("ne lève pas si l'API est injoignable, mais le signale en console", async () => {
			const avertissement = spyOn(console, "warn");
			userService.setUser(ALICE);
			const restauration = service.restaurerSession();

			http.expectOne("/Auth/moi").error(new ProgressEvent("error"));
			await expectAsync(restauration).toBeResolved();

			expect(userService.isConnected()).toBeFalse();
			expect(avertissement).toHaveBeenCalled();
		});
	});
});
