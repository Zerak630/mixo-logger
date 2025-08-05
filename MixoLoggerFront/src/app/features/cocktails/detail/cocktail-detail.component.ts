import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { CocktailDetail } from '../../../models/cocktail';
import { CocktailsService } from '../cocktails.service';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
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
    if(await this.cocktailService.makeCocktail(this.cocktail().id)) {
      alert('Cocktail en cours de préparation !');
    } else {
      alert('Erreur lors de la préparation du cocktail.');
    }
  }
}
