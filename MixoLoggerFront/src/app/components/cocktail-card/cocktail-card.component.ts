import { Component, inject, input } from '@angular/core';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { Cocktail } from '../../models/cocktail';
import { toggle } from '../../utils/toggle-signal';
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
