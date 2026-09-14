import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { catchError, Observable, throwError } from "rxjs";
import { CHEMINS_AUTH } from "./auth.service";
import ConfigService from "./config.service";
import UserService from "./user.service";

export const httpInterceptor = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const baseUrl = inject(ConfigService).config?.apiUrl;
  const router = inject(Router);
  const userService = inject(UserService);

  // `Access-Control-Allow-Origin` est un en-tête de RÉPONSE : le poser ici ne servait
  // à rien, sinon à rendre la requête non simple et à provoquer un préflight inutile.
  // Même logique pour `Content-Type`, qui n'a de sens que s'il y a un corps.
  const headers = req.body === null || req.body === undefined
    ? req.headers.set('Accept', 'application/json')
    : req.headers
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json, application/json-patch+json');

  const clonedReq = req.clone({
    headers,
    url: baseUrl + (req.url.startsWith('/') ? req.url : `/${req.url}`),
    // Le front (:4200) et l'API (:5213) sont deux origines : sans cela, le navigateur
    // n'envoie pas le cookie de session et l'API répond 401 à tout.
    withCredentials: true
  });

  return next(clonedReq).pipe(
    catchError((erreur: unknown) => {
      // Session expirée ou fermée ailleurs : on renvoie vers la connexion, en mémorisant la
      // page pour y revenir. Les appels d'authentification gèrent eux-mêmes leur 401.
      if (erreur instanceof HttpErrorResponse && erreur.status === 401 && !CHEMINS_AUTH.includes(req.url)) {
        userService.emptyUser();
        const retour = router.url.startsWith('/connexion') ? undefined : router.url;
        router.navigate(['/connexion'], { queryParams: retour ? { retour } : {} });
      }

      return throwError(() => erreur);
    })
  );
}
