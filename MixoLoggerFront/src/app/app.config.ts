import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { routes } from './app.routes';
import { httpInterceptor } from './core/http-interceptor';
import { MyPreset } from './styles/customTheme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([httpInterceptor])),
    // Plus de provideAnimationsAsync() : PrimeNG 21 anime ses composants via
    // @primeuix/motion et ne dépend plus de @angular/animations (déprécié).
    providePrimeNG({
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
