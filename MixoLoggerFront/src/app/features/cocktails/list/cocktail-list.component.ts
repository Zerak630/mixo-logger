
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Cocktail } from '../../../models/cocktail';

@Component({
  selector: 'cocktail-list',
  templateUrl: './cocktail-list.component.html',
  styleUrls: ['./cocktail-list.component.scss'],
  imports: [RouterLink]
})
export default class CocktailListComponent {
  readonly cocktails = input.required<Cocktail[]>();

  ngOnInit() {
    console.log('CocktailListComponent initialized');
    console.log('Cocktails:', this.cocktails());
  }
}
