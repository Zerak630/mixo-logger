import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem, MessageService } from 'primeng/api';
import { Avatar } from 'primeng/avatar';
import { DialogService } from 'primeng/dynamicdialog';
import { Menubar } from 'primeng/menubar';
import LoginModalComponent from './components/login-modal/login-modal';
import UserService from './core/user.service';
import { ToastModule } from 'primeng/toast';

@Component({
	selector: 'app-root',
	imports: [
		RouterOutlet,
		Menubar,
		Avatar,
		LoginModalComponent,
		ToastModule
	],
	providers: [
		DialogService,
		MessageService
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

	protected readonly userService = inject(UserService);
}
