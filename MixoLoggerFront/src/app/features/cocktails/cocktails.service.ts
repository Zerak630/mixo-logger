import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Cocktail, CocktailDetail } from "../../models/cocktail";
import { firstValueFrom, Observable } from "rxjs";
import { Guid } from "../../core/base-models";

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

  makeCocktail(cocktailId: Guid): Promise<boolean> {
    return firstValueFrom(this.http.post<boolean>(`/Bars/MakeCocktails`,
      [
        {
          cocktailId: cocktailId,
          quantity: 1
        }
      ]
    ));
  }
}
