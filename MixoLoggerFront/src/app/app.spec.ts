import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { MessageService } from '@openng/optimus-ui/api';
import { App } from './app';
import UserService from './core/user.service';

describe('App', () => {
	let http: HttpTestingController;
	let userService: UserService;

	beforeEach(async () => {
		await TestBed.configureTestingModule({
			imports: [App],
			providers: [
				provideZonelessChangeDetection(),
				provideRouter([]),
				provideHttpClient(),
				provideHttpClientTesting(),
				MessageService
			]
		}).compileComponents();

		http = TestBed.inject(HttpTestingController);
		userService = TestBed.inject(UserService);
	});

	afterEach(() => http.verify());

	async function rendre(): Promise<HTMLElement> {
		const fixture = TestBed.createComponent(App);
		await fixture.whenStable();
		return fixture.nativeElement as HTMLElement;
	}

	it('affiche le nom de l\'application', async () => {
		expect((await rendre()).querySelector('h1')?.textContent).toContain('MixoLogger');
	});

	it('sans session, ne propose ni navigation ni déconnexion', async () => {
		const page = await rendre();

		expect(page.textContent).not.toContain('Mon bar');
		expect(page.textContent).not.toContain('Se déconnecter');
	});

	it('avec une session, affiche la navigation et le nom de l\'utilisateur', async () => {
		userService.setUser({ id: '1', identifiant: 'alice', nomAffiche: 'Alice Lemaire' });

		const page = await rendre();

		expect(page.textContent).toContain('Cocktails');
		expect(page.textContent).toContain('Mon bar');
		expect(page.textContent).toContain('Alice Lemaire');
		expect(page.textContent).toContain('Se déconnecter');
	});

	it('à la déconnexion, vide la session et renvoie vers la connexion même si l\'API échoue', async () => {
		userService.setUser({ id: '1', identifiant: 'alice', nomAffiche: 'Alice' });
		const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
		const page = await rendre();

		const bouton = [...page.querySelectorAll('button')].find(b => b.textContent?.includes('Se déconnecter'))!;
		bouton.click();
		http.expectOne('/Auth/deconnexion').flush(null, { status: 500, statusText: 'Erreur' });
		await new Promise(resolve => setTimeout(resolve));

		expect(userService.isConnected()).toBeFalse();
		expect(navigate).toHaveBeenCalledWith(['/connexion']);
	});
});
