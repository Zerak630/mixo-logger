import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from "@angular/router";
import { invitesSeulement, retourSur, sessionRequise } from "./session.guards";
import UserService from "./user.service";

describe("retourSur", () => {
	it("accepte un chemin interne, requête comprise", () => {
		expect(retourSur("/my_bar")).toBe("/my_bar");
		expect(retourSur("/cocktails/42?x=1")).toBe("/cocktails/42?x=1");
	});

	it("renvoie vers la liste sans page de retour", () => {
		expect(retourSur(undefined)).toBe("/cocktails");
		expect(retourSur(null)).toBe("/cocktails");
		expect(retourSur("")).toBe("/cocktails");
	});

	it("refuse toute adresse qui sortirait du site", () => {
		expect(retourSur("https://ailleurs.example")).toBe("/cocktails");
		expect(retourSur("//ailleurs.example")).toBe("/cocktails");
		expect(retourSur("/\\ailleurs.example")).toBe("/cocktails");
		expect(retourSur("javascript:alert(1)")).toBe("/cocktails");
		expect(retourSur("my_bar")).toBe("/cocktails");
	});

	it("ne revient pas sur la page de connexion", () => {
		expect(retourSur("/connexion")).toBe("/cocktails");
		expect(retourSur("/connexion?retour=/my_bar")).toBe("/cocktails");
	});
});

describe("gardes de session", () => {
	let userService: UserService;
	let router: Router;

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [provideZonelessChangeDetection(), provideRouter([])]
		});
		userService = TestBed.inject(UserService);
		router = TestBed.inject(Router);
	});

	function executer(garde: typeof sessionRequise, url = "/"): ReturnType<typeof sessionRequise> {
		return TestBed.runInInjectionContext(() =>
			garde({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot));
	}

	describe("sessionRequise", () => {
		it("laisse passer un utilisateur connecté", () => {
			userService.setUser({ id: "1", identifiant: "alice", nomAffiche: "Alice" });

			expect(executer(sessionRequise, "/my_bar")).toBeTrue();
		});

		it("renvoie vers la connexion en mémorisant la page demandée", () => {
			const resultat = executer(sessionRequise, "/cocktails/42");

			expect(resultat).toBeInstanceOf(UrlTree);
			expect(router.serializeUrl(resultat as UrlTree)).toBe("/connexion?retour=%2Fcocktails%2F42");
		});
	});

	describe("invitesSeulement", () => {
		it("laisse un invité voir la page de connexion", () => {
			expect(executer(invitesSeulement)).toBeTrue();
		});

		it("renvoie un utilisateur déjà connecté vers la liste", () => {
			userService.setUser({ id: "1", identifiant: "alice", nomAffiche: "Alice" });

			const resultat = executer(invitesSeulement);

			expect(resultat).toBeInstanceOf(UrlTree);
			expect(router.serializeUrl(resultat as UrlTree)).toBe("/cocktails");
		});
	});
});
