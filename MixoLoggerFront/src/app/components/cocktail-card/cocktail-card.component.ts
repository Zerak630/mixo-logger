import { Component, inject, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { Cocktail } from '../../models/cocktail';
import { toggle } from '../../utils/toggle-signal';
import { MessageModule } from 'primeng/message';
import { MessageService } from 'primeng/api';

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

  private readonly messageService = inject(MessageService);

  public toggleFavorite(event: Event): void {
    this.messageService.add({severity:'info', summary:'Favorite toggled', detail:`${this.cocktail().name} favorite status changed.`});
    event.preventDefault();
  }
}
