import { Routes } from '@angular/router';
import { cocktailByIdResolver, cocktailsListResolver } from './cocktails.resolver';

export default [
  {
    path: '',
    loadComponent: () => import('./list/cocktail-list.component'),
    resolve: { cocktails: cocktailsListResolver }
  },
  {
    path: ':id',
    loadComponent: () => import('./detail/cocktail-detail.component'),
    resolve: { cocktail: cocktailByIdResolver }
  }
] as Routes;
