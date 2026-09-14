import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, ElementRef, inject, Injector, input, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Button } from '@openng/optimus-ui/button';
import { InputText } from '@openng/optimus-ui/inputtext';
import AuthService from '../../core/auth.service';
import { retourSur } from '../../core/session.guards';

/** Page de connexion : seul écran accessible sans session (F6). */
@Component({
  selector: 'connexion',
  templateUrl: './connexion.component.html',
  styleUrls: ['./connexion.component.scss'],
  imports: [ReactiveFormsModule, Button, InputText],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class ConnexionComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  /** Page demandée avant d'être renvoyé ici (paramètre `?retour=`). */
  readonly retour = input<string>();

  readonly envoiEnCours = signal(false);
  readonly erreur = signal<string | null>(null);
  readonly motDePasseVisible = signal(false);
  readonly tentative = signal(false);

  private readonly champIdentifiant = viewChild<ElementRef<HTMLInputElement>>('champIdentifiant');
  private readonly champMotDePasse = viewChild<ElementRef<HTMLInputElement>>('champMotDePasse');

  readonly formulaire = new FormGroup({
    identifiant: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    motDePasse: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  constructor() {
    afterNextRender(() => this.champIdentifiant()?.nativeElement.focus());
  }

  async seConnecter(): Promise<void> {
    this.tentative.set(true);
    this.erreur.set(null);

    const { identifiant, motDePasse } = this.formulaire.getRawValue();
    if (!identifiant.trim() || !motDePasse) {
      this.formulaire.markAllAsTouched();
      (identifiant.trim() ? this.champMotDePasse() : this.champIdentifiant())?.nativeElement.focus();
      return;
    }

    this.envoiEnCours.set(true);
    try {
      await this.authService.login(identifiant.trim(), motDePasse);
      await this.router.navigateByUrl(retourSur(this.retour()));
    } catch (erreur) {
      this.erreur.set(this.messageErreur(erreur));
      // Un mot de passe refusé est effacé et le focus y revient, pour retaper sans détour.
      this.formulaire.controls.motDePasse.reset();
      afterNextRender(() => this.champMotDePasse()?.nativeElement.focus(), { injector: this.injector });
    } finally {
      this.envoiEnCours.set(false);
    }
  }

  private messageErreur(erreur: unknown): string {
    if (erreur instanceof HttpErrorResponse) {
      if (erreur.status === 401) return 'Identifiant ou mot de passe incorrect.';
      if (erreur.status === 429) return erreur.error?.detail ?? 'Trop de tentatives. Réessaie dans une minute.';
      if (erreur.status === 0) return 'Le serveur ne répond pas. Vérifie ta connexion puis réessaie.';
    }
    return 'La connexion a échoué. Réessaie dans un instant.';
  }
}
