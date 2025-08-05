import { HttpEvent, HttpHandlerFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { Observable } from "rxjs";
import ConfigService from "./config.service";

export const httpInterceptor = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const baseUrl = inject(ConfigService).config?.apiUrl;

  const clonedReq = req.clone({
    headers: req.headers
      .set("Access-Control-Allow-Origin", '*')
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json, application/json-patch+json'),
      url: baseUrl + (req.url.startsWith('/') ? req.url : `/${req.url}`),
  });
  return next(clonedReq);
}
