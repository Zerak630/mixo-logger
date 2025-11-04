
import { Component, input } from '@angular/core';
import { CocktailCardComponent } from "../../../components/cocktail-card/cocktail-card.component";
import { Cocktail } from '../../../models/cocktail';

@Component({
  selector: 'cocktail-list',
  templateUrl: './cocktail-list.component.html',
  styleUrls: ['./cocktail-list.component.scss'],
  imports: [CocktailCardComponent]
})
export default class CocktailListComponent {
  readonly cocktails = input.required<Cocktail[]>();

  handleKeyPress($event: KeyboardEvent, card: CocktailCardComponent, link: HTMLAnchorElement) {
    switch ($event.code) {
      case 'Space':
        card.isFocused.toggle();
        $event.preventDefault();
        break;
      case 'Enter':
        link.click();
        break;
      default:
        break;
    }
  }
}
