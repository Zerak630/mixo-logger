import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Cocktail, CocktailDetail } from "../../models/cocktail";
import { Observable } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class CocktailsService {
  private readonly http = inject(HttpClient);

  public getCocktails(): Observable<Cocktail[]> {
    return this.http.get<Cocktail[]>('/Cocktails');
  }

  public getCocktailById(id: string): Observable<CocktailDetail> {
    return this.http.get<CocktailDetail>(`/Cocktails/${id}`);
  }
}
