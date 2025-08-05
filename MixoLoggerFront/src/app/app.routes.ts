
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'cocktails',
    loadChildren: () => import('./features/cocktails/cocktails.routes')
  }
];
