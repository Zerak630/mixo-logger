import { HttpErrorResponse } from "@angular/common/http";
import { provideZonelessChangeDetection } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MessageService } from "@openng/optimus-ui/api";
import AuthService from "../../core/auth.service";
import UserService from "../../core/user.service";
import CompteComponent from "./compte.component";

describe("CompteComponent", () => {
	let fixture: ComponentFixture<CompteComponent>;
	let composant: CompteComponent;
	let auth: jasmine.SpyObj<AuthService>;
	let messages: jasmine.SpyObj<MessageService>;

	beforeEach(async () => {
		auth = jasmine.createSpyObj<AuthService>("AuthService", ["modifierCompte", "changerMotDePasse"]);
		messages = jasmine.createSpyObj<MessageService>("MessageService", ["add"]);

		await TestBed.configureTestingModule({
			imports: [CompteComponent],
			providers: [
				provideZonelessChangeDetection(),
				{ provide: AuthService, useValue: auth },
				{ provide: MessageService, useValue: messages }
			]
		}).compileComponents();

		TestBed.inject(UserService).setUser({ id: "1", identifiant: "alice", nomAffiche: "Alice L." });
		fixture = TestBed.createComponent(CompteComponent);
		composant = fixture.componentInstance;
		await fixture.whenStable();
	});

	function texte(): string {
		return (fixture.nativeElement as HTMLElement).textContent ?? "";
	}

	describe("identité", () => {
		it("part du compte connecté", () => {
			expect(composant.profil.getRawValue()).toEqual({ identifiant: "alice", nomAffiche: "Alice L." });
		});

		it("n'envoie pas un identifiant vide ou fait d'espaces", async () => {
			composant.profil.controls.identifiant.setValue("   ");

			await composant.enregistrerProfil();
			await fixture.whenStable();

			expect(auth.modifierCompte).not.toHaveBeenCalled();
			expect(texte()).toContain("L'identifiant est obligatoire.");
		});

		it("envoie les valeurs sans espaces autour et confirme le nouvel identifiant", async () => {
			auth.modifierCompte.and.resolveTo({ id: "1", identifiant: "Alicia", nomAffiche: "Alicia" });
			composant.profil.setValue({ identifiant: " Alicia ", nomAffiche: " Alicia " });

			await composant.enregistrerProfil();

			expect(auth.modifierCompte).toHaveBeenCalledWith("Alicia", "Alicia");
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({
				severity: "success",
				detail: "Connecte-toi désormais avec « Alicia »."
			}));
			expect(composant.profil.getRawValue()).toEqual({ identifiant: "Alicia", nomAffiche: "Alicia" });
		});

		it("affiche le refus de l'API", async () => {
			auth.modifierCompte.and.rejectWith(new HttpErrorResponse({ status: 409, error: { detail: "L'identifiant « bob » est déjà pris." } }));
			composant.profil.controls.identifiant.setValue("bob");

			await composant.enregistrerProfil();
			await fixture.whenStable();

			expect(texte()).toContain("L'identifiant « bob » est déjà pris.");
			expect(composant.profilEnCours()).toBeFalse();
		});
	});

	describe("mot de passe", () => {
		function saisir(actuel: string, nouveau: string, confirmation = nouveau): void {
			composant.motDePasse.setValue({ actuel, nouveau, confirmation });
		}

		it("exige une confirmation identique", async () => {
			saisir("ancien-mot-de-passe", "nouveau-mot-de-passe", "autre-chose-encore");

			await composant.changerMotDePasse();
			await fixture.whenStable();

			expect(auth.changerMotDePasse).not.toHaveBeenCalled();
			expect(texte()).toContain("Les deux saisies ne sont pas identiques.");
		});

		it("exige 12 caractères, comme l'API", async () => {
			saisir("ancien-mot-de-passe", "onze-carac.");

			await composant.changerMotDePasse();
			await fixture.whenStable();

			expect(auth.changerMotDePasse).not.toHaveBeenCalled();
			expect(texte()).toContain("12 caractères au moins.");
		});

		it("change le mot de passe, vide le formulaire et prévient que les autres sessions sont fermées", async () => {
			auth.changerMotDePasse.and.resolveTo();
			saisir("ancien-mot-de-passe", "nouveau-mot-de-passe");

			await composant.changerMotDePasse();

			expect(auth.changerMotDePasse).toHaveBeenCalledWith("ancien-mot-de-passe", "nouveau-mot-de-passe");
			expect(composant.motDePasse.getRawValue()).toEqual({ actuel: "", nouveau: "", confirmation: "" });
			expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ severity: "success", summary: "Mot de passe changé" }));
		});

		it("affiche le refus de l'API et efface le seul mot de passe actuel", async () => {
			auth.changerMotDePasse.and.rejectWith(new HttpErrorResponse({ status: 400, error: { detail: "Le mot de passe actuel est incorrect." } }));
			saisir("pas-le-bon-mot-de-passe", "nouveau-mot-de-passe");

			await composant.changerMotDePasse();
			await fixture.whenStable();

			expect(texte()).toContain("Le mot de passe actuel est incorrect.");
			// Le champ vidé n'ajoute pas son propre message sous celui du serveur.
			expect(texte()).not.toContain("Renseigne ton mot de passe actuel");
			expect(composant.motDePasse.getRawValue()).toEqual({ actuel: "", nouveau: "nouveau-mot-de-passe", confirmation: "nouveau-mot-de-passe" });
		});

		it("signale un serveur injoignable", async () => {
			auth.changerMotDePasse.and.rejectWith(new HttpErrorResponse({ status: 0 }));
			saisir("ancien-mot-de-passe", "nouveau-mot-de-passe");

			await composant.changerMotDePasse();

			expect(composant.erreurMotDePasse()).toBe("Le serveur ne répond pas. Vérifie ta connexion puis réessaie.");
		});
	});
});
