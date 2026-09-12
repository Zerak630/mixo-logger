import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { LigneStock, MyBar, NiveauStock } from "../../models/bar";
import { Volume } from "../../models/cocktail";

@Injectable({
	providedIn: "root"
})
export class MyBarService {
	private readonly http = inject(HttpClient);

	public getMyBar(): Observable<MyBar> {
		return this.http.get<MyBar>("/Bars");
	}

	/**
	 * Déclare la possession d'un ingrédient. Sans `quantity`, la ligne est en
	 * possession simple — c'est le cas nominal.
	 */
	public addIngredient(name: string, niveau?: NiveauStock, quantity?: Volume): Observable<MyBar> {
		return this.http.post<MyBar>("/Bars/ingredients", { name, niveau, quantity });
	}

	public setNiveau(ingredient: LigneStock, niveau: NiveauStock): Observable<MyBar> {
		return this.http.patch<MyBar>(`/Bars/ingredients/${encodeURIComponent(ingredient.name)}`, { niveau });
	}

	public removeIngredient(ingredient: LigneStock): Observable<MyBar> {
		return this.http.delete<MyBar>(`/Bars/ingredients/${encodeURIComponent(ingredient.name)}`);
	}
}
