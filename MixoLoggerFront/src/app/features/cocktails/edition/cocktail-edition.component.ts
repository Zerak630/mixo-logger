import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, Injector, input, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MessageService } from '@openng/optimus-ui/api';
import { AutoComplete, AutoCompleteCompleteEvent } from '@openng/optimus-ui/autocomplete';
import { Button, ButtonDirective } from '@openng/optimus-ui/button';
import { InputNumber } from '@openng/optimus-ui/inputnumber';
import { InputText } from '@openng/optimus-ui/inputtext';
import { Select } from '@openng/optimus-ui/select';
import { Textarea } from '@openng/optimus-ui/textarea';
import { IngredientReference } from '../../../models/bar';
import { CocktailDetail, RecetteSaisie, UniteDose } from '../../../models/cocktail';
import { libelleUnite } from '../../../utils/libelle-dose';
import { cleIngredient, rechercherIngredients } from '../../../utils/recherche-ingredients';
import { MyBarService } from '../../mybar/mybar.service';
import { CocktailsService } from '../cocktails.service';

type LigneIngredient = FormGroup<{
  ingredient: FormControl<IngredientReference | string | null>;
  valeur: FormControl<number | null>;
  unite: FormControl<UniteDose>;
}>;

/** Refuse une chaîne faite uniquement d'espaces, que `Validators.required` laisse passer. */
const nonVide: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() === '' ? { required: true } : null;

function nomSaisi(valeur: IngredientReference | string | null): string {
  return (typeof valeur === 'string' ? valeur : valeur?.name ?? '').trim();
}

/**
 * Création (`/cocktails/new`) et édition (`/cocktails/:id/edit`) d'une recette (F5).
 * Le mode dépend de la présence d'un cocktail résolu par la route.
 */
@Component({
  selector: 'cocktail-edition',
  templateUrl: './cocktail-edition.component.html',
  styleUrls: ['./cocktail-edition.component.scss'],
  imports: [ReactiveFormsModule, RouterLink, AutoComplete, Button, ButtonDirective, InputNumber, InputText, Select, Textarea],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CocktailEditionComponent {
  private readonly cocktailsService = inject(CocktailsService);
  private readonly myBarService = inject(MyBarService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  private readonly bandeauErreur = viewChild<ElementRef<HTMLElement>>('bandeauErreur');

  /** Absent en création. */
  readonly cocktail = input<CocktailDetail>();

  readonly enEdition = computed(() => !!this.cocktail());

  /** Recette d'un autre auteur, ou d'origine : l'API refuserait l'enregistrement (403). */
  readonly interdit = computed(() => this.cocktail()?.modifiable === false);

  private readonly referentiel = toSignal(this.myBarService.getIngredients(), { initialValue: [] as IngredientReference[] });

  readonly optionsUnite = toSignal(
    this.cocktailsService.getUnites(),
    { initialValue: ['cL'] as UniteDose[] });

  readonly optionsUniteLibellees = computed(() =>
    this.optionsUnite().map(unite => ({ label: libelleUnite(unite), value: unite })));

  readonly suggestions = signal<IngredientReference[]>([]);

  readonly enregistrementEnCours = signal(false);

  /** Erreur renvoyée par l'API (nom déjà pris, contenu refusé), affichée en tête de formulaire. */
  readonly erreurServeur = signal<string | null>(null);

  /** Passe à vrai à la première tentative d'envoi : les erreurs ne s'affichent pas avant. */
  readonly tentative = signal(false);

  readonly formulaire = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, nonVide, Validators.maxLength(80)] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
    ingredients: new FormArray<LigneIngredient>([], [Validators.required]),
    etapes: new FormArray<FormControl<string>>([], [Validators.required])
  });

  get ingredients() { return this.formulaire.controls.ingredients; }
  get etapes() { return this.formulaire.controls.etapes; }

  /**
   * Indices des lignes qui désignent un ingrédient déjà présent plus haut dans la recette.
   * Tient compte des alias : « rhum » puis « white rum » sont le même ingrédient pour l'API.
   */
  private readonly valeursIngredients = toSignal(this.formulaire.controls.ingredients.valueChanges, { initialValue: [] });

  readonly doublons = computed(() => {
    this.valeursIngredients();
    const vus = new Set<string>();
    const doublons = new Set<number>();

    this.ingredients.controls.forEach((ligne, index) => {
      const nom = nomSaisi(ligne.controls.ingredient.value);
      if (!nom) return;

      const cle = cleIngredient(this.referentiel(), nom);
      vus.has(cle) ? doublons.add(index) : vus.add(cle);
    });

    return doublons;
  });

  constructor() {
    // Le cocktail arrive par liaison d'entrée de routeur : on pré-remplit dès qu'il est connu.
    effect(() => this.remplir(this.cocktail()));
  }

  rechercher(event: AutoCompleteCompleteEvent, indexLigne: number): void {
    // Les ingrédients des autres lignes ne sont pas reproposés.
    const autres = new Set(this.ingredients.controls
      .filter((_, index) => index !== indexLigne)
      .map(ligne => nomSaisi(ligne.controls.ingredient.value))
      .filter(nom => nom)
      .map(nom => cleIngredient(this.referentiel(), nom)));

    this.suggestions.set(rechercherIngredients(this.referentiel(), event.query, autres));
  }

  ajouterIngredient(nom = '', valeur: number | null = null, unite: UniteDose = 'cL'): void {
    this.ingredients.push(new FormGroup({
      ingredient: new FormControl<IngredientReference | string | null>(nom || null, [Validators.required, nonVide]),
      valeur: new FormControl<number | null>(valeur, [Validators.required, Validators.min(0.01)]),
      unite: new FormControl<UniteDose>(unite, { nonNullable: true, validators: [Validators.required] })
    }));
  }

  retirerIngredient(index: number): void {
    this.ingredients.removeAt(index);
  }

  ajouterEtape(description = ''): void {
    this.etapes.push(new FormControl(description, { nonNullable: true, validators: [Validators.required, nonVide] }));
  }

  retirerEtape(index: number): void {
    this.etapes.removeAt(index);
  }

  /** Échange une étape avec sa voisine. `sens` : -1 vers le haut, +1 vers le bas. */
  deplacerEtape(index: number, sens: -1 | 1): void {
    const cible = index + sens;
    if (cible < 0 || cible >= this.etapes.length) return;

    const etape = this.etapes.at(index);
    this.etapes.removeAt(index);
    this.etapes.insert(cible, etape);
  }

  aDesErreurs(control: AbstractControl): boolean {
    return control.invalid && (control.touched || this.tentative());
  }

  enregistrer(): void {
    this.tentative.set(true);
    this.erreurServeur.set(null);
    this.formulaire.markAllAsTouched();

    if (this.formulaire.invalid || this.doublons().size > 0) {
      this.focaliserPremiereErreur();
      return;
    }

    const valeur = this.formulaire.getRawValue();
    const recette: RecetteSaisie = {
      name: valeur.name.trim(),
      description: valeur.description.trim() || null,
      ingredients: valeur.ingredients.map(ligne => ({
        name: nomSaisi(ligne.ingredient),
        valeur: ligne.valeur!,
        unite: ligne.unite
      })),
      etapes: valeur.etapes.map(etape => etape.trim())
    };

    const existant = this.cocktail();
    const requete = existant
      ? this.cocktailsService.updateCocktail(existant.id, recette)
      : this.cocktailsService.createCocktail(recette);

    this.enregistrementEnCours.set(true);
    requete.subscribe({
      next: enregistre => {
        this.enregistrementEnCours.set(false);
        this.messageService.add({
          severity: 'success',
          summary: existant ? 'Recette modifiée' : 'Recette créée',
          detail: enregistre.name,
          life: 3000
        });
        this.router.navigate(['/cocktails', enregistre.id]);
      },
      error: (erreur: HttpErrorResponse) => {
        this.enregistrementEnCours.set(false);
        this.erreurServeur.set(erreur.error?.detail ?? "L'enregistrement a échoué. Réessaie dans un instant.");
        // Le bouton est en bas, le bandeau en haut : sans cela l'erreur passe inaperçue.
        afterNextRender(() => this.bandeauErreur()?.nativeElement.focus(), { injector: this.injector });
      }
    });
  }

  private remplir(cocktail: CocktailDetail | undefined): void {
    this.ingredients.clear();
    this.etapes.clear();

    if (!cocktail) {
      this.formulaire.reset();
      // Une recette vide n'a pas de sens : on propose d'emblée une ligne de chaque.
      this.ajouterIngredient();
      this.ajouterEtape();
      return;
    }

    this.formulaire.patchValue({ name: cocktail.name, description: cocktail.description ?? '' });
    cocktail.ingredients.forEach(ingredient => this.ajouterIngredient(ingredient.name, ingredient.valeur, ingredient.unite));
    [...cocktail.etapes].sort((a, b) => a.ordre - b.ordre).forEach(etape => this.ajouterEtape(etape.description));
  }

  /** Amène le focus sur le premier champ en erreur, pour les utilisateurs au clavier. */
  private focaliserPremiereErreur(): void {
    queueMicrotask(() => {
      const champ = document.querySelector<HTMLElement>(
        '.edition .ng-invalid input, .edition input.ng-invalid, .edition textarea.ng-invalid, .edition [aria-invalid="true"]');
      champ?.focus();
    });
  }
}
