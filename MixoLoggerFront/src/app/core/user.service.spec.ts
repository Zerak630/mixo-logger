import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import UserService from "./user.service";

describe("UserService", () => {
	let service: UserService;

	beforeEach(() => {
		TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
		service = TestBed.inject(UserService);
	});

	function connecter(nomAffiche: string): void {
		service.setUser({ id: "1", identifiant: "alice", nomAffiche });
	}

	it("n'est pas connecté au départ", () => {
		expect(service.isConnected()).toBeFalse();
		expect(service.utilisateur()).toBeNull();
		expect(service.initiales()).toBe("");
	});

	it("suit la connexion puis la déconnexion", () => {
		connecter("Alice");
		expect(service.isConnected()).toBeTrue();
		expect(service.utilisateur()?.identifiant).toBe("alice");

		service.emptyUser();
		expect(service.isConnected()).toBeFalse();
		expect(service.utilisateur()).toBeNull();
	});

	describe("initiales", () => {
		it("prend l'initiale des deux premiers mots, en majuscules", () => {
			connecter("alice lemaire");
			expect(service.initiales()).toBe("AL");
		});

		it("se contente d'une lettre pour un seul mot", () => {
			connecter("alice");
			expect(service.initiales()).toBe("A");
		});

		it("ignore les mots au-delà du deuxième et les espaces superflues", () => {
			connecter("  Jean   Pierre  Martin ");
			expect(service.initiales()).toBe("JP");
		});

		it("garde les lettres accentuées", () => {
			connecter("élodie ößer");
			expect(service.initiales()).toBe("ÉÖ");
		});
	});
});
