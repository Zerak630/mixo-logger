import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem, MessageService } from '@openng/optimus-ui/api';
import { Avatar } from '@openng/optimus-ui/avatar';
import { DialogService } from '@openng/optimus-ui/dynamicdialog';
import { Menubar } from '@openng/optimus-ui/menubar';
import LoginModalComponent from './components/login-modal/login-modal';
import UserService from './core/user.service';
import { ToastModule } from '@openng/optimus-ui/toast';

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
	changeDetection: ChangeDetectionStrategy.Eager,
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
