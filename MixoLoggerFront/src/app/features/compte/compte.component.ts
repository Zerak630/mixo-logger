import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, ElementRef, inject, Injector, signal, viewChild } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MessageService } from '@openng/optimus-ui/api';
import { Button } from '@openng/optimus-ui/button';
import { InputText } from '@openng/optimus-ui/inputtext';
import AuthService from '../../core/auth.service';
import UserService from '../../core/user.service';

/** Même minimum que l'API (`Utilisateur.LongueurMinimaleMotDePasse`). */
export const LONGUEUR_MINIMALE_MOT_DE_PASSE = 12;

/** Refuse une chaîne faite uniquement d'espaces, que `Validators.required` laisse passer. */
const nonVide: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() === '' ? { required: true } : null;

const confirmationIdentique: ValidatorFn = (groupe: AbstractControl): ValidationErrors | null =>
  groupe.get('nouveau')?.value === groupe.get('confirmation')?.value ? null : { confirmation: true };

/**
 * « Mon compte » : identifiant de connexion, nom affiché et mot de passe. Le compte reste le même
 * quoi qu'on y change : bar, recettes et notes suivent.
 */
@Component({
  selector: 'compte',
  templateUrl: './compte.component.html',
  styleUrls: ['./compte.component.scss'],
  imports: [ReactiveFormsModule, Button, InputText],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class CompteComponent {
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);
  protected readonly userService = inject(UserService);
  private readonly injector = inject(Injector);

  private readonly champActuel = viewChild<ElementRef<HTMLInputElement>>('champActuel');

  readonly longueurMinimale = LONGUEUR_MINIMALE_MOT_DE_PASSE;

  readonly profil = new FormGroup({
    identifiant: new FormControl(this.userService.utilisateur()?.identifiant ?? '', { nonNullable: true, validators: [Validators.required, nonVide] }),
    nomAffiche: new FormControl(this.userService.utilisateur()?.nomAffiche ?? '', { nonNullable: true })
  });

  readonly motDePasse = new FormGroup({
    actuel: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nouveau: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(LONGUEUR_MINIMALE_MOT_DE_PASSE)] }),
    confirmation: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  }, { validators: [confirmationIdentique] });

  readonly profilEnCours = signal(false);
  readonly erreurProfil = signal<string | null>(null);
  readonly tentativeProfil = signal(false);

  readonly motDePasseEnCours = signal(false);
  readonly erreurMotDePasse = signal<string | null>(null);
  readonly tentativeMotDePasse = signal(false);

  async enregistrerProfil(): Promise<void> {
    this.tentativeProfil.set(true);
    this.erreurProfil.set(null);
    if (this.profil.invalid) return;

    const { identifiant, nomAffiche } = this.profil.getRawValue();
    this.profilEnCours.set(true);
    try {
      const compte = await this.authService.modifierCompte(identifiant.trim(), nomAffiche.trim());
      this.profil.reset({ identifiant: compte.identifiant, nomAffiche: compte.nomAffiche });
      this.tentativeProfil.set(false);
      this.messageService.add({
        severity: 'success',
        summary: 'Compte mis à jour',
        detail: `Connecte-toi désormais avec « ${compte.identifiant} ».`,
        life: 5000
      });
    } catch (erreur) {
      this.erreurProfil.set(this.message(erreur, "La modification a échoué. Réessaie dans un instant."));
    } finally {
      this.profilEnCours.set(false);
    }
  }

  async changerMotDePasse(): Promise<void> {
    this.tentativeMotDePasse.set(true);
    this.erreurMotDePasse.set(null);
    if (this.motDePasse.invalid) return;

    const { actuel, nouveau } = this.motDePasse.getRawValue();
    this.motDePasseEnCours.set(true);
    try {
      await this.authService.changerMotDePasse(actuel, nouveau);
      this.motDePasse.reset();
      this.tentativeMotDePasse.set(false);
      this.messageService.add({
        severity: 'success',
        summary: 'Mot de passe changé',
        detail: 'Tes sessions ouvertes sur d\'autres appareils sont fermées.',
        life: 5000
      });
    } catch (erreur) {
      this.erreurMotDePasse.set(this.message(erreur, "Le changement a échoué. Réessaie dans un instant."));
      // Un mot de passe actuel refusé est effacé, pour le retaper sans détour. Le champ vide ne doit
      // pas ajouter « renseigne ton mot de passe » sous l'erreur du serveur, qui suffit.
      this.motDePasse.controls.actuel.reset();
      this.tentativeMotDePasse.set(false);
      afterNextRender(() => this.champActuel()?.nativeElement.focus(), { injector: this.injector });
    } finally {
      this.motDePasseEnCours.set(false);
    }
  }

  private message(erreur: unknown, parDefaut: string): string {
    if (erreur instanceof HttpErrorResponse) {
      if (erreur.status === 0) return 'Le serveur ne répond pas. Vérifie ta connexion puis réessaie.';
      if (erreur.error?.detail) return erreur.error.detail;
    }
    return parDefaut;
  }
}
