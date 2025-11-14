
import { Routes } from '@angular/router';
import { myBarResolver } from './features/mybar/mybar.resolver';

export const routes: Routes = [
	{
		path: 'cocktails',
		loadChildren: () => import('./features/cocktails/cocktails.routes')
	},
	{
		path: 'my_bar',
		loadComponent: () => import('./features/mybar/layout/mybar.layout'),
		resolve: { stockList: myBarResolver }
	},
];
