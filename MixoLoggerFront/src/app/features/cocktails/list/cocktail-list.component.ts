import { Component, computed, input, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ToggleSwitch, ToggleSwitchChangeEvent } from '@openng/optimus-ui/toggleswitch';
import { CocktailCardComponent } from "../../../components/cocktail-card/cocktail-card.component";
import { CocktailResume } from '../../../models/cocktail';

@Component({
  selector: 'cocktail-list',
  templateUrl: './cocktail-list.component.html',
  styleUrls: ['./cocktail-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CocktailCardComponent, FormsModule, ToggleSwitch, RouterLink]
})
export default class CocktailListComponent {
  /** Déjà triés par l'API : réalisables d'abord, puis par nombre d'ingrédients manquants. */
  readonly cocktails = input.required<CocktailResume[]>();

  /** F4 — « qu'est-ce que je peux faire avec ce que j'ai ? » */
  readonly seulementRealisables = signal(false);

  readonly nombreRealisables = computed(() => this.cocktails().filter(cocktail => cocktail.realisable).length);

  readonly cocktailsAffiches = computed(() => this.seulementRealisables()
    ? this.cocktails().filter(cocktail => cocktail.realisable)
    : this.cocktails());

  basculerFiltre(event: ToggleSwitchChangeEvent) {
    this.seulementRealisables.set(event.checked);
  }

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
