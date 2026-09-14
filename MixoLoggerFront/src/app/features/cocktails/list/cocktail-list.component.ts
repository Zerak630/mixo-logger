import { Component, computed, inject, input, ChangeDetectionStrategy, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonDirective } from '@openng/optimus-ui/button';
import { IconField } from '@openng/optimus-ui/iconfield';
import { InputIcon } from '@openng/optimus-ui/inputicon';
import { InputText } from '@openng/optimus-ui/inputtext';
import { Select } from '@openng/optimus-ui/select';
import { ToggleSwitch } from '@openng/optimus-ui/toggleswitch';
import { CocktailCardComponent } from "../../../components/cocktail-card/cocktail-card.component";
import { IngredientReference } from '../../../models/bar';
import { CocktailResume } from '../../../models/cocktail';
import { filtrerCocktails } from '../../../utils/recherche-cocktails';
import { MyBarService } from '../../mybar/mybar.service';

@Component({
  selector: 'cocktail-list',
  templateUrl: './cocktail-list.component.html',
  styleUrls: ['./cocktail-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [CocktailCardComponent, FormsModule, IconField, InputIcon, InputText, Select, ToggleSwitch, RouterLink, ButtonDirective]
})
export default class CocktailListComponent {
  /** Déjà triés par l'API : réalisables d'abord, puis par nombre d'ingrédients manquants. */
  readonly cocktails = input.required<CocktailResume[]>();

  /** Pour retrouver une recette par un alias d'ingrédient (« white rum »). Sans lui, la recherche reste utilisable. */
  private readonly referentiel = toSignal(inject(MyBarService).getIngredients(), { initialValue: [] as IngredientReference[] });

  /** F8 — nom, description ou ingrédient. */
  readonly texte = signal('');

  /** F4 — « qu'est-ce que je peux faire avec ce que j'ai ? » */
  readonly seulementRealisables = signal(false);

  readonly seulementMesRecettes = signal(false);

  readonly noteMinimale = signal<number | null>(null);

  readonly optionsNote = [
    { label: 'Toutes les notes', value: null },
    { label: '3 étoiles et plus', value: 3 },
    { label: '4 étoiles et plus', value: 4 },
    { label: '4,5 étoiles et plus', value: 4.5 }
  ];

  readonly nombreRealisables = computed(() => this.cocktails().filter(cocktail => cocktail.realisable).length);

  readonly cocktailsAffiches = computed(() => filtrerCocktails(
    this.cocktails(),
    {
      texte: this.texte(),
      seulementRealisables: this.seulementRealisables(),
      seulementMesRecettes: this.seulementMesRecettes(),
      noteMinimale: this.noteMinimale()
    },
    this.referentiel()));

  readonly filtresActifs = computed(() =>
    !!this.texte().trim() || this.seulementRealisables() || this.seulementMesRecettes() || this.noteMinimale() !== null);

  reinitialiserFiltres(): void {
    this.texte.set('');
    this.seulementRealisables.set(false);
    this.seulementMesRecettes.set(false);
    this.noteMinimale.set(null);
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
