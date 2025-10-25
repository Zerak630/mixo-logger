import { Component, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { Cocktail } from '../../models/cocktail';

@Component({
  selector: 'cocktail-card',
  templateUrl: './cocktail-card.component.html',
  styleUrls: ['./cocktail-card.component.scss'],
  imports: [
    CardModule,
    ButtonModule,
  ],
})
export class CocktailCardComponent {
  public cocktail = input.required<Cocktail>();
}
