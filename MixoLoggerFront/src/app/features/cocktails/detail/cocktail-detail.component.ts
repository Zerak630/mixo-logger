import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button, ButtonDirective } from '@openng/optimus-ui/button';
import { CocktailDetail, DoseIngredient } from '../../../models/cocktail';
import { libelleDose } from '../../../utils/libelle-dose';
import { CocktailsService } from '../cocktails.service';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
  imports: [Button, ButtonDirective, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CocktailDetailComponent {
  readonly cocktail = input.required<CocktailDetail>();

  /** Triées par `ordre` : l'affichage ne doit pas dépendre de l'ordre de sérialisation de l'API. */
  readonly etapes = computed(() =>
    [...(this.cocktail().etapes ?? [])].sort((a, b) => a.ordre - b.ordre));

  nbVerres = signal(1);

  /** « 12 feuilles » pour 2 verres de Mojito. */
  doseAffichee(ingredient: DoseIngredient): string {
    return libelleDose(ingredient.valeur * this.nbVerres(), ingredient.unite);
  }

  readonly cocktailService = inject(CocktailsService);

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
}
