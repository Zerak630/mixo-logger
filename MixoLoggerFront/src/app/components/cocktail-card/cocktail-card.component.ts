import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, output, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { Button } from '@openng/optimus-ui/button';
import { PartageService } from '../../core/partage.service';
import { CocktailsService } from '../../features/cocktails/cocktails.service';
import { CocktailResume } from '../../models/cocktail';
import { formaterMoyenne, libelleNotes } from '../../utils/libelle-notes';
import { teinteCocktail } from '../../utils/teinte-cocktail';

/**
 * Carte d'un cocktail dans la liste. Le nom est un lien qui couvre toute la carte ; les actions
 * sont de vrais boutons à côté de ce lien (un bouton imbriqué dans un lien est invalide).
 *
 * Exige un `ConfirmationService` et un `<p-confirmpopup>` fournis par l'écran parent.
 */
@Component({
  selector: 'cocktail-card',
  templateUrl: './cocktail-card.component.html',
  styleUrls: ['./cocktail-card.component.scss'],
  imports: [Button, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[style.--teinte]': 'teinte()'
  }
})
export class CocktailCardComponent {
  public readonly cocktail = input.required<CocktailResume>();

  /** Un verre a été préparé : le stock a changé, la liste doit se recharger. */
  public readonly prepare = output<void>();

  private readonly cocktailsService = inject(CocktailsService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly partageService = inject(PartageService);

  /** Couleur du visuel, propre à la recette, en attendant les photos (F9). */
  public readonly teinte = computed(() => teinteCocktail(this.cocktail().name));

  public readonly preparationEnCours = signal(false);

  /** « Menthe, Citron vert » — les ingrédients qui empêchent de préparer un verre. */
  public readonly libelleManques = computed(() =>
    this.cocktail().manques
      .map(manque => manque.raison === 'Insuffisant' ? `${manque.ingredient} (pas assez)` : manque.ingredient)
      .join(', '));

  /** « 4,3 » : virgule décimale française. */
  public readonly moyenneAffichee = computed(() => formaterMoyenne(this.cocktail().notes.moyenne));

  /** « Note moyenne 4,3 sur 5 (3 notes), ta note : 5 » — lu par les lecteurs d'écran et en infobulle. */
  public readonly libelleNote = computed(() => libelleNotes(this.cocktail().notes));

  partager(): void {
    this.partageService.partagerCocktail(this.cocktail());
  }

  /** Prépare un verre après confirmation : l'action retire du stock, un clic égaré ne doit pas suffire. */
  realiser(event: Event): void {
    const cocktail = this.cocktail();

    this.confirmationService.confirm({
      target: event.currentTarget ?? undefined,
      message: `Préparer un verre de « ${cocktail.name} » ? Les volumes suivis seront retirés de ton bar.`,
      icon: 'pi pi-question-circle',
      acceptLabel: 'Préparer',
      rejectLabel: 'Annuler',
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.preparer(cocktail)
    });
  }

  private async preparer(cocktail: CocktailResume): Promise<void> {
    this.preparationEnCours.set(true);
    try {
      await this.cocktailsService.makeCocktail(cocktail.id, 1);
      this.messageService.add({ severity: 'success', summary: 'Santé !', detail: `Un « ${cocktail.name} » préparé, ton bar est à jour.`, life: 4000 });
      this.prepare.emit();
    } catch (erreur) {
      this.messageService.add({
        severity: 'error',
        summary: 'Préparation impossible',
        detail: erreur instanceof HttpErrorResponse && erreur.error?.detail
          ? erreur.error.detail
          : 'La préparation a échoué. Réessaie dans un instant.',
        life: 6000
      });
      // Le stock a pu changer ailleurs (autre onglet) : la liste se remet à jour dans tous les cas.
      this.prepare.emit();
    } finally {
      this.preparationEnCours.set(false);
    }
  }
}
