import { HttpErrorResponse } from "@angular/common/http";
import { Component, computed, inject, input, linkedSignal, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { ConfirmationService, MessageService } from "@openng/optimus-ui/api";
import { AutoComplete, AutoCompleteCompleteEvent } from "@openng/optimus-ui/autocomplete";
import { Button } from "@openng/optimus-ui/button";
import { ConfirmPopup } from "@openng/optimus-ui/confirmpopup";
import { InputNumber } from "@openng/optimus-ui/inputnumber";
import { SelectButton } from "@openng/optimus-ui/selectbutton";
import { ToggleSwitch } from "@openng/optimus-ui/toggleswitch";
import { Observable } from "rxjs";
import { IngredientReference, LIBELLES_NIVEAU, LigneStock, MyBar, NIVEAUX_STOCK, NiveauStock } from "../../../models/bar";
import { UniteVolume } from "../../../models/cocktail";
import { normaliserNom } from "../../../utils/normaliser-nom";
import { MyBarService } from "../mybar.service";

/** Nombre maximal de suggestions affichées par l'autocomplétion. */
const SUGGESTIONS_MAX = 8;

@Component({
	templateUrl: "./mybar.layout.html",
	styleUrl: "./mybar.layout.scss",
	imports: [FormsModule, ReactiveFormsModule, AutoComplete, Button, ConfirmPopup, InputNumber, SelectButton, ToggleSwitch],
	providers: [ConfirmationService]
})
export default class MyBarLayout {
	private readonly myBarService = inject(MyBarService);
	private readonly messageService = inject(MessageService);
	private readonly confirmationService = inject(ConfirmationService);

	/** Résolu par `myBarResolver`. */
	public bar = input.required<MyBar>();

	/**
	 * État courant du bar : initialisé par le resolver, puis remplacé par la réponse de
	 * chaque modification — l'API renvoie toujours le bar complet, jamais un delta.
	 */
	private readonly etat = linkedSignal(() => this.bar());

	public readonly stockList = computed(() => this.etat().ingredients);

	/** Lignes dont une requête est en cours, pour désactiver leurs contrôles. */
	public readonly enCours = signal<ReadonlySet<string>>(new Set());

	public readonly ajoutEnCours = signal(false);

	/**
	 * Rechargé après chaque ajout : un nom inconnu est créé côté API, et doit pouvoir être
	 * reproposé par l'autocomplétion s'il est retiré puis rajouté plus tard.
	 */
	private readonly referentiel = signal<IngredientReference[]>([]);

	constructor() {
		this.chargerReferentiel();
	}

	public readonly suggestions = signal<IngredientReference[]>([]);

	public readonly optionsNiveau = NIVEAUX_STOCK.map(niveau => ({ label: LIBELLES_NIVEAU[niveau], value: niveau }));

	public readonly optionsUnite = [UniteVolume.Mililitre, UniteVolume.Centilitre, UniteVolume.Litre]
		.map(unite => ({ label: unite, value: unite }));

	public readonly formulaire = new FormGroup({
		// Une chaîne libre est acceptée : un ingrédient inconnu est créé par l'API.
		ingredient: new FormControl<IngredientReference | string | null>(null, Validators.required),
		niveau: new FormControl<NiveauStock>("Pleine", { nonNullable: true }),
		suiviPrecis: new FormControl(false, { nonNullable: true }),
		volume: new FormControl<number | null>(null),
		unite: new FormControl<UniteVolume>(UniteVolume.Centilitre, { nonNullable: true })
	});

	private readonly suiviPrecisSaisi = toSignal(this.formulaire.controls.suiviPrecis.valueChanges, { initialValue: false });

	public readonly afficherVolume = computed(() => this.suiviPrecisSaisi());

	public libelleVolume(ligne: LigneStock): string | null {
		return ligne.quantity ? `${ligne.quantity.value} ${ligne.quantity.unit}` : null;
	}

	public estEnCours(ligne: LigneStock): boolean {
		return this.enCours().has(ligne.name);
	}

	/**
	 * Recherche sur le nom et sur les alias, sans tenir compte des accents ni de la casse,
	 * en écartant ce qui est déjà dans le bar. Les correspondances en début de nom passent
	 * en premier : « gin » doit proposer « Gin » avant « Bière ginger ».
	 */
	public rechercher(event: AutoCompleteCompleteEvent): void {
		const requete = normaliserNom(event.query);
		const dejaPresents = new Set(this.stockList().map(ligne => normaliserNom(ligne.name)));

		const candidats = this.referentiel()
			.filter(ingredient => !dejaPresents.has(normaliserNom(ingredient.name)))
			.map(ingredient => ({ ingredient, rang: this.rang(ingredient, requete) }))
			.filter(candidat => candidat.rang < Number.POSITIVE_INFINITY)
			.sort((a, b) => a.rang - b.rang || a.ingredient.name.localeCompare(b.ingredient.name));

		this.suggestions.set(candidats.slice(0, SUGGESTIONS_MAX).map(candidat => candidat.ingredient));
	}

	public ajouter(): void {
		const { ingredient, niveau, suiviPrecis, volume, unite } = this.formulaire.getRawValue();
		const nom = (typeof ingredient === "string" ? ingredient : ingredient?.name ?? "").trim();

		if (!nom) {
			this.formulaire.controls.ingredient.markAsTouched();
			return;
		}

		if (suiviPrecis && !(volume && volume > 0)) {
			this.formulaire.controls.volume.setErrors({ requis: true });
			this.formulaire.controls.volume.markAsTouched();
			return;
		}

		this.ajoutEnCours.set(true);
		this.myBarService.addIngredient(nom, niveau, suiviPrecis ? { value: volume!, unit: unite } : undefined)
			.subscribe({
				next: bar => {
					this.etat.set(bar);
					this.formulaire.reset();
					this.ajoutEnCours.set(false);
					this.chargerReferentiel();
					this.messageService.add({ severity: "success", summary: "Ajouté au bar", detail: nom, life: 2500 });
				},
				error: (erreur: HttpErrorResponse) => {
					this.ajoutEnCours.set(false);
					this.gererErreur(erreur);
				}
			});
	}

	public changerNiveau(ligne: LigneStock, niveau: NiveauStock | null): void {
		// SelectButton émet `null` quand on reclique l'option active : on l'ignore.
		if (!niveau || niveau === ligne.niveau) {
			return;
		}

		this.executer(ligne, this.myBarService.setNiveau(ligne, niveau));
	}

	/**
	 * Une ligne en possession simple se retire directement : la rajouter ne coûte rien.
	 * Une ligne en suivi précis porte un volume qui serait perdu — on demande confirmation.
	 */
	public retirer(ligne: LigneStock, event: Event): void {
		if (!ligne.suiviPrecis) {
			this.executer(ligne, this.myBarService.removeIngredient(ligne));
			return;
		}

		this.confirmationService.confirm({
			target: event.currentTarget ?? undefined,
			message: `Retirer « ${ligne.name} » ? Le volume suivi (${this.libelleVolume(ligne)}) sera perdu.`,
			icon: "pi pi-exclamation-triangle",
			acceptLabel: "Retirer",
			rejectLabel: "Annuler",
			// Sans cela les deux boutons ont le même style : l'action destructive doit se distinguer.
			acceptButtonProps: { severity: "danger" },
			rejectButtonProps: { severity: "secondary", text: true },
			accept: () => this.executer(ligne, this.myBarService.removeIngredient(ligne))
		});
	}

	private executer(ligne: LigneStock, requete: Observable<MyBar>): void {
		this.marquerEnCours(ligne.name, true);

		requete.subscribe({
			next: bar => {
				this.etat.set(bar);
				this.marquerEnCours(ligne.name, false);
			},
			error: (erreur: HttpErrorResponse) => {
				this.marquerEnCours(ligne.name, false);
				this.gererErreur(erreur);
			}
		});
	}

	/**
	 * 409 (le bar a été modifié entre-temps) et 404 (la ligne n'existe plus) signifient que
	 * l'écran affiche un état périmé : on recharge plutôt que de laisser l'utilisateur
	 * agir sur des données fausses.
	 */
	private gererErreur(erreur: HttpErrorResponse): void {
		const detail = erreur.error?.detail ?? "Une erreur inattendue est survenue.";

		if (erreur.status === 409 || erreur.status === 404) {
			this.messageService.add({ severity: "warn", summary: "Bar rechargé", detail });
			this.myBarService.getMyBar().subscribe(bar => this.etat.set(bar));
			return;
		}

		this.messageService.add({ severity: "error", summary: "Échec", detail });
	}

	/** Un échec est sans gravité : l'autocomplétion reste vide, la saisie libre fonctionne toujours. */
	private chargerReferentiel(): void {
		this.myBarService.getIngredients().subscribe({
			next: ingredients => this.referentiel.set(ingredients),
			error: () => this.referentiel.set([])
		});
	}

	private marquerEnCours(nom: string, actif: boolean): void {
		this.enCours.update(courant => {
			const suivant = new Set(courant);
			actif ? suivant.add(nom) : suivant.delete(nom);
			return suivant;
		});
	}

	/** 0 : le nom commence par la requête ; 1 : un mot du nom ; 2 : un alias ; ∞ : aucune correspondance. */
	private rang(ingredient: IngredientReference, requete: string): number {
		const nom = normaliserNom(ingredient.name);

		if (!requete || nom.startsWith(requete)) return 0;
		if (nom.split(" ").some(mot => mot.startsWith(requete))) return 1;
		if (ingredient.aliases.some(alias => alias.includes(requete))) return 2;

		return Number.POSITIVE_INFINITY;
	}
}
