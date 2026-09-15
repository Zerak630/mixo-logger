import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { Button, ButtonDirective } from '@openng/optimus-ui/button';
import { ConfirmPopup } from '@openng/optimus-ui/confirmpopup';
import { Rating } from '@openng/optimus-ui/rating';
import { Observable } from 'rxjs';
import { PartageService } from '../../../core/partage.service';
import { CocktailDetail, DoseIngredient, Notes } from '../../../models/cocktail';
import { libelleDose } from '../../../utils/libelle-dose';
import { formaterMoyenne } from '../../../utils/libelle-notes';
import { CocktailsService } from '../cocktails.service';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
  imports: [Button, ButtonDirective, ConfirmPopup, FormsModule, Rating, RouterLink],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CocktailDetailComponent {
  readonly cocktail = input.required<CocktailDetail>();

  /** Triées par `ordre` : l'affichage ne doit pas dépendre de l'ordre de sérialisation de l'API. */
  readonly etapes = computed(() =>
    [...(this.cocktail().etapes ?? [])].sort((a, b) => a.ordre - b.ordre));

  nbVerres = signal(1);

  readonly suppressionEnCours = signal(false);

  /** Part de celles du cocktail résolu, puis suit les réponses de l'API après chaque notation. */
  readonly notes = linkedSignal(() => this.cocktail().notes);

  readonly moyenneAffichee = computed(() => formaterMoyenne(this.notes().moyenne));

  readonly notationEnCours = signal(false);

  /** « 12 feuilles » pour 2 verres de Mojito. */
  doseAffichee(ingredient: DoseIngredient): string {
    return libelleDose(ingredient.valeur * this.nbVerres(), ingredient.unite);
  }

  readonly cocktailService = inject(CocktailsService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly partageService = inject(PartageService);

  async augmenterNbVerres() {
    this.nbVerres.update(value => value + 1);
  }

  async baisserNbVerres() {
    this.nbVerres.update(value => value > 1 ? value - 1 : value);
  }

  readonly preparationEnCours = signal(false);

  async makeThisCocktail() {
    const cocktail = this.cocktail();
    const verres = this.nbVerres();

    this.preparationEnCours.set(true);
    try {
      // Tout ou rien : l'API décompte la commande entière, ou rien si le stock ne suffit pas.
      await this.cocktailService.makeCocktail(cocktail.id, verres);
      this.messageService.add({
        severity: 'success',
        summary: 'Santé !',
        detail: `${verres > 1 ? `${verres} verres` : 'Un verre'} de « ${cocktail.name} » préparé${verres > 1 ? 's' : ''}, ton bar est à jour.`,
        life: 4000
      });
    } catch (erreur) {
      this.messageService.add({
        severity: 'error',
        summary: 'Préparation impossible',
        detail: erreur instanceof HttpErrorResponse && erreur.error?.detail
          ? erreur.error.detail
          : 'La préparation a échoué. Réessaie dans un instant.',
        life: 6000
      });
    } finally {
      this.preparationEnCours.set(false);
    }
  }

  partager(): void {
    this.partageService.partagerCocktail(this.cocktail());
  }

  /** Donne ou change sa note. Une valeur vide (étoile désélectionnée) retire la note. */
  noter(valeur: number | null): void {
    if (valeur === this.notes().maNote) return;
    if (valeur === null) {
      this.retirerNote();
      return;
    }

    this.envoyerNotation(this.cocktailService.noter(this.cocktail().id, valeur));
  }

  retirerNote(): void {
    this.envoyerNotation(this.cocktailService.retirerNote(this.cocktail().id));
  }

  private envoyerNotation(requete: Observable<Notes>): void {
    const avant = this.notes();
    this.notationEnCours.set(true);

    requete.subscribe({
      next: notes => {
        this.notes.set(notes);
        this.notationEnCours.set(false);
      },
      error: (erreur: HttpErrorResponse) => {
        // Les étoiles reviennent à la note réellement enregistrée.
        this.notes.set({ ...avant });
        this.notationEnCours.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Note non enregistrée',
          detail: erreur.error?.detail ?? "La note n'a pas pu être enregistrée. Réessaie dans un instant.",
          life: 6000
        });
      }
    });
  }

  /** Suppression définitive, après confirmation : il n'y a pas de corbeille. */
  supprimer(event: Event): void {
    const cocktail = this.cocktail();

    this.confirmationService.confirm({
      target: event.currentTarget ?? undefined,
      message: `Supprimer « ${cocktail.name} » ? La recette sera définitivement perdue.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Supprimer',
      rejectLabel: 'Annuler',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => {
        this.suppressionEnCours.set(true);
        this.cocktailService.deleteCocktail(cocktail.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Recette supprimée', detail: cocktail.name, life: 3000 });
            this.router.navigate(['/cocktails']);
          },
          error: (erreur: HttpErrorResponse) => {
            this.suppressionEnCours.set(false);
            this.messageService.add({
              severity: 'error',
              summary: 'Suppression impossible',
              detail: erreur.error?.detail ?? 'La suppression a échoué. Réessaie dans un instant.',
              life: 6000
            });
          }
        });
      }
    });
  }
}
