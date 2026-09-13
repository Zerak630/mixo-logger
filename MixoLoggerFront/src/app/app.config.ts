import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { provideOptimus } from '@openng/optimus-ui/config';
import { routes } from './app.routes';
import { httpInterceptor } from './core/http-interceptor';
import { MyPreset } from './styles/customTheme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withXhr(), withInterceptors([httpInterceptor])),
    // Pas de provideAnimationsAsync() : OptimusUI (comme PrimeNG 21 dont il est issu)
    // anime ses composants via @openng/optimus-ui-motion, sans @angular/animations.
    provideOptimus({
      theme: {
        preset: MyPreset,
        options: {
          // L'application est sombre par conception (docs/MVP.md §5.1). Avec la valeur par
          // défaut `system`, les composants suivaient le réglage clair/sombre du poste, et
          // les écrans aux couleurs codées en dur devenaient illisibles en mode clair (B14).
          // La classe est posée une fois pour toutes sur <html> dans index.html.
          darkModeSelector: '.app-dark',
          // cssLayer: {
          //   name: 'primeng',
          //   order: 'app-styles, primeng, another-css-library'
          // }
        }
      }
    })
  ]
};
