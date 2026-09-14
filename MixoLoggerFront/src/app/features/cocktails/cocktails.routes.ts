import { Routes } from '@angular/router';
import { cocktailByIdResolver, cocktailsListResolver } from './cocktails.resolver';

export default [
  {
    path: '',
    loadComponent: () => import('./list/cocktail-list.component'),
    resolve: { cocktails: cocktailsListResolver },
    title: 'Cocktails'
  },
  {
    // Avant ':id', sinon « new » serait pris pour un identifiant.
    path: 'new',
    loadComponent: () => import('./edition/cocktail-edition.component'),
    title: 'Nouvelle recette'
  },
  {
    path: ':id',
    loadComponent: () => import('./detail/cocktail-detail.component'),
    resolve: { cocktail: cocktailByIdResolver },
    // Chaque route porte un titre : sans cela, l'onglet garde celui de la page précédente
    // (« Nouvelle recette » restait affiché sur le détail après une création).
    title: 'Détail du cocktail'
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./edition/cocktail-edition.component'),
    resolve: { cocktail: cocktailByIdResolver },
    title: 'Modifier la recette'
  }
] as Routes;
