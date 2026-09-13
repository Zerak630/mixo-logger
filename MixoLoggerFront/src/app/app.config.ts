import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideOptimus } from '@openng/optimus-ui/config';
import { routes } from './app.routes';
import { httpInterceptor } from './core/http-interceptor';
import { MyPreset } from './styles/customTheme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([httpInterceptor])),
    // Pas de provideAnimationsAsync() : OptimusUI (comme PrimeNG 21 dont il est issu)
    // anime ses composants via @openng/optimus-ui-motion, sans @angular/animations.
    provideOptimus({
      theme: {
        preset: MyPreset,
        options: {
          // cssLayer: {
          //   name: 'primeng',
          //   order: 'app-styles, primeng, another-css-library'
          // }
        }
      }
    })
  ]
};
