import { HttpClient, provideHttpClient, withInterceptors } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideZonelessChangeDetection } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import ConfigService from "./config.service";
import { httpInterceptor } from "./http-interceptor";
import UserService from "./user.service";

const API = "http://api.test/api";

describe("httpInterceptor", () => {
	let client: HttpClient;
	let http: HttpTestingController;
	let router: Router;
	let userService: UserService;

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				provideHttpClient(withInterceptors([httpInterceptor])),
				provideHttpClientTesting(),
				{ provide: ConfigService, useValue: { config: { apiUrl: API } } }
			]
		});
		client = TestBed.inject(HttpClient);
		http = TestBed.inject(HttpTestingController);
		router = TestBed.inject(Router);
		userService = TestBed.inject(UserService);
	});

	afterEach(() => http.verify());

	it("préfixe l'adresse de l'API, avec ou sans barre initiale", () => {
		client.get("/Cocktails").subscribe();
		client.get("Bars").subscribe();

		http.expectOne(`${API}/Cocktails`).flush([]);
		http.expectOne(`${API}/Bars`).flush({});
	});

	it("envoie le cookie de session à l'API, qui est une autre origine", () => {
		client.get("/Cocktails").subscribe();

		expect(http.expectOne(`${API}/Cocktails`).request.withCredentials).toBeTrue();
	});

	it("ne pose Content-Type que s'il y a un corps", () => {
		client.get("/Cocktails").subscribe();
		client.post("/Cocktails", { name: "Mojito" }).subscribe();

		const lecture = http.expectOne(r => r.method === "GET");
		expect(lecture.request.headers.has("Content-Type")).toBeFalse();
		expect(lecture.request.headers.get("Accept")).toBe("application/json");

		const ecriture = http.expectOne(r => r.method === "POST");
		expect(ecriture.request.headers.get("Content-Type")).toBe("application/json");
	});

	it("ne pose jamais d'en-tête de réponse CORS sur la requête", () => {
		client.get("/Cocktails").subscribe();

		expect(http.expectOne(`${API}/Cocktails`).request.headers.has("Access-Control-Allow-Origin")).toBeFalse();
	});

	describe("sur un 401", () => {
		let navigate: jasmine.Spy;

		beforeEach(() => {
			navigate = spyOn(router, "navigate").and.resolveTo(true);
			userService.setUser({ id: "1", identifiant: "alice", nomAffiche: "Alice" });
		});

		it("vide la session et renvoie vers la connexion en mémorisant la page", () => {
			spyOnProperty(router, "url").and.returnValue("/my_bar");
			let erreur: unknown;
			client.get("/Bars").subscribe({ error: e => erreur = e });

			http.expectOne(`${API}/Bars`).flush(null, { status: 401, statusText: "Unauthorized" });

			expect(userService.isConnected()).toBeFalse();
			expect(navigate).toHaveBeenCalledWith(["/connexion"], { queryParams: { retour: "/my_bar" } });
			// L'appelant reçoit quand même l'erreur.
			expect(erreur).toEqual(jasmine.objectContaining({ status: 401 }));
		});

		it("ne mémorise pas la page de connexion elle-même", () => {
			spyOnProperty(router, "url").and.returnValue("/connexion?retour=%2Fmy_bar");
			client.get("/Bars").subscribe({ error: () => undefined });

			http.expectOne(`${API}/Bars`).flush(null, { status: 401, statusText: "Unauthorized" });

			expect(navigate).toHaveBeenCalledWith(["/connexion"], { queryParams: {} });
		});

		it("laisse les appels d'authentification gérer eux-mêmes leur 401", () => {
			client.post("/Auth/connexion", { identifiant: "alice", motDePasse: "faux" }).subscribe({ error: () => undefined });

			http.expectOne(`${API}/Auth/connexion`).flush(null, { status: 401, statusText: "Unauthorized" });

			expect(navigate).not.toHaveBeenCalled();
			expect(userService.isConnected()).toBeTrue();
		});
	});

	it("ne touche pas à la session sur une autre erreur", () => {
		const navigate = spyOn(router, "navigate");
		userService.setUser({ id: "1", identifiant: "alice", nomAffiche: "Alice" });
		client.get("/Bars").subscribe({ error: () => undefined });

		http.expectOne(`${API}/Bars`).flush(null, { status: 403, statusText: "Forbidden" });

		expect(navigate).not.toHaveBeenCalled();
		expect(userService.isConnected()).toBeTrue();
	});
});
