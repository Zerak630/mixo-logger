import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Avatar } from 'primeng/avatar';
import { Menubar } from 'primeng/menubar';

@Component({
	selector: 'app-root',
	imports: [
		RouterOutlet,
		Menubar,
		Avatar
	],
	templateUrl: './app.html',
	styleUrl: './app.scss'
})
export class App {
	protected readonly title = signal('MixoLoggerFront');

	protected readonly menuItems: MenuItem[] = [
		{
			label: "Cocktails",
			routerLink: "/cocktails"
		},
		{
			label: "My bar",
			routerLink: "/my_bar"
		},
		{
			label: "About",
			routerLink: "/about"
		},
		{
			label: "Contact",
			routerLink: "/contact"
		},
	];
}
