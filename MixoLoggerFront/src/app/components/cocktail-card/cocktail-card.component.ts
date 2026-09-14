import { Component, computed, inject, input, ChangeDetectionStrategy } from '@angular/core';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { CocktailResume } from '../../models/cocktail';
import { toggle } from '../../utils/toggle-signal';
import { formaterMoyenne, libelleNotes } from '../../utils/libelle-notes';
import { MessageModule } from '@openng/optimus-ui/message';
import { MessageService } from '@openng/optimus-ui/api';

@Component({
  selector: 'cocktail-card',
  templateUrl: './cocktail-card.component.html',
  styleUrls: ['./cocktail-card.component.scss'],
  imports: [
    CardModule,
    ButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.Eager,
  host: {
    '[class.--focused]': 'isFocused()',
    '(focusout)':'this.isFocused.set(false)',
    '(focusin)':'this.isFocused.set(true)'
  }
})
export class CocktailCardComponent {
  public cocktail = input.required<CocktailResume>();

  /** « Menthe, Citron vert » — les ingrédients qui empêchent de préparer un verre. */
  public readonly libelleManques = computed(() =>
    this.cocktail().manques
      .map(manque => manque.raison === 'Insuffisant' ? `${manque.ingredient} (pas assez)` : manque.ingredient)
      .join(', '));

  /** « 4,3 » : virgule décimale française. */
  public readonly moyenneAffichee = computed(() => formaterMoyenne(this.cocktail().notes.moyenne));

  /** « Note moyenne 4,3 sur 5 (3 notes), ta note : 5 » — lu par les lecteurs d'écran et en infobulle. */
  public readonly libelleNote = computed(() => libelleNotes(this.cocktail().notes));

  public isFocused = toggle(false);

  private readonly messageService = inject(MessageService);

  public toggleFavorite(event: Event): void {
    this.messageService.add({severity:'info', summary:'Favorite toggled', detail:`${this.cocktail().name} favorite status changed.`});
    event.preventDefault();
  }
}
