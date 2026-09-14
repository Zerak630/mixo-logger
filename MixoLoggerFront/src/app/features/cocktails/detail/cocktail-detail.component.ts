import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { Button, ButtonDirective } from '@openng/optimus-ui/button';
import { ConfirmPopup } from '@openng/optimus-ui/confirmpopup';
import { CocktailDetail, DoseIngredient } from '../../../models/cocktail';
import { libelleDose } from '../../../utils/libelle-dose';
import { CocktailsService } from '../cocktails.service';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
  imports: [Button, ButtonDirective, ConfirmPopup, RouterLink],
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

  /** « 12 feuilles » pour 2 verres de Mojito. */
  doseAffichee(ingredient: DoseIngredient): string {
    return libelleDose(ingredient.valeur * this.nbVerres(), ingredient.unite);
  }

  readonly cocktailService = inject(CocktailsService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  async augmenterNbVerres() {
    this.nbVerres.update(value => value + 1);
  }

  async baisserNbVerres() {
    this.nbVerres.update(value => value > 1 ? value - 1 : value);
  }

  async makeThisCocktail() {
    try {
      // Le nombre de verres est désormais transmis : l'API décompte la commande
      // entière, ou n'en décompte aucune part si le stock ne suffit pas.
      await this.cocktailService.makeCocktail(this.cocktail().id, this.nbVerres());
      alert('Cocktail en cours de préparation !');
    } catch (erreur) {
      alert(erreur instanceof HttpErrorResponse && erreur.error?.detail
        ? erreur.error.detail
        : 'Erreur lors de la préparation du cocktail.');
    }
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
