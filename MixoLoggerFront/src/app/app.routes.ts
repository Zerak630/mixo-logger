
import { Routes } from '@angular/router';
import { invitesSeulement, sessionRequise } from './core/session.guards';
import { myBarResolver } from './features/mybar/mybar.resolver';

export const routes: Routes = [
	{
		path: 'connexion',
		loadComponent: () => import('./features/connexion/connexion.component'),
		canActivate: [invitesSeulement],
		title: 'Connexion'
	},
	{
		// Toute l'application exige une session (F6).
		path: '',
		canActivateChild: [sessionRequise],
		children: [
			{
				path: '',
				pathMatch: 'full',
				redirectTo: 'cocktails'
			},
			{
				path: 'cocktails',
				loadChildren: () => import('./features/cocktails/cocktails.routes')
			},
			{
				path: 'my_bar',
				loadComponent: () => import('./features/mybar/layout/mybar.layout'),
				resolve: { bar: myBarResolver },
				title: 'Mon bar'
			},
		]
	},
	{
		// Adresse inconnue (ancien lien, faute de frappe) : retour à l'accueil plutôt qu'un écran vide.
		path: '**',
		redirectTo: ''
	},
];
