import { Component, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { Cocktail } from '../../models/cocktail';
import { toggle } from '../../utils/toggle-signal';

@Component({
  selector: 'cocktail-card',
  templateUrl: './cocktail-card.component.html',
  styleUrls: ['./cocktail-card.component.scss'],
  imports: [
    CardModule,
    ButtonModule,
  ],
  host: {
    '[class.--focused]': 'isFocused()',
    '(focusout)':'this.isFocused.set(false)',
    '(focusin)':'this.isFocused.set(true)'
  }
})
export class CocktailCardComponent {
  public cocktail = input.required<Cocktail>();
  public isFocused = toggle(false);
}
