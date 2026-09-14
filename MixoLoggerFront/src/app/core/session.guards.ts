import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import UserService from "./user.service";

/**
 * Toute l'application exige une session (F6). Sans session, on part vers la connexion en
 * gardant la page demandée, pour y revenir une fois connecté.
 *
 * Ce garde ne protège rien en soi : c'est l'API qui refuse les données (401). Il évite
 * seulement d'afficher un écran vide.
 */
export const sessionRequise: CanActivateFn = (_route, state) => {
	return inject(UserService).isConnected()
		|| inject(Router).createUrlTree(['/connexion'], { queryParams: { retour: state.url } });
};

/** Déjà connecté : la page de connexion n'a pas lieu d'être. */
export const invitesSeulement: CanActivateFn = () => {
	return !inject(UserService).isConnected() || inject(Router).createUrlTree(['/cocktails']);
};

/**
 * N'accepte qu'un chemin interne comme page de retour : un `retour=https://ailleurs` ou
 * `retour=//ailleurs` dans un lien piégé ne doit pas renvoyer vers un autre site après la connexion.
 */
export function retourSur(retour: string | null | undefined): string {
	return retour && retour.startsWith('/') && !retour.startsWith('//') && !retour.startsWith('/\\') && !retour.startsWith('/connexion')
		? retour
		: '/cocktails';
}
