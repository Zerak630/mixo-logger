import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { Button } from '@openng/optimus-ui/button';
import { CocktailDetail } from '../../../models/cocktail';
import { CocktailsService } from '../cocktails.service';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
  imports: [Button],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CocktailDetailComponent {
  readonly cocktail = input.required<CocktailDetail>();

  nbVerres = signal(1);

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
