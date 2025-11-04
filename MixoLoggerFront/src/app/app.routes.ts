
import { Routes } from '@angular/router';

export const routes: Routes = [
	{
		path: 'cocktails',
		loadChildren: () => import('./features/cocktails/cocktails.routes')
	},
	{
		path: 'my_bar',
		loadComponent: () => import('./features/mybar/layout/mybar.layout')
	},
];
