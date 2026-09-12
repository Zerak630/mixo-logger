import { HttpEvent, HttpHandlerFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { Observable } from "rxjs";
import ConfigService from "./config.service";

export const httpInterceptor = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const baseUrl = inject(ConfigService).config?.apiUrl;

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
  });
  return next(clonedReq);
}
