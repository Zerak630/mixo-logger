import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { CocktailDetail } from '../../../models/cocktail';

@Component({
  selector: 'cocktail-detail',
  templateUrl: './cocktail-detail.component.html',
  styleUrls: ['./cocktail-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CocktailDetailComponent {
  readonly cocktail = input.required<CocktailDetail>();
  nbVerres = signal(1);

  async augmenterNbVerres() {
    this.nbVerres.update(value => value + 1);
  }

  async baisserNbVerres() {
    this.nbVerres.update(value => value > 1 ? value - 1 : value);
  }
}
