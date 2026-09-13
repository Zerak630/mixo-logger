import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CocktailDetail, CocktailResume } from "../../models/cocktail";
import { firstValueFrom, Observable } from "rxjs";
import { Guid } from "../../core/base-models";
import { MyBar } from "../../models/bar";

@Injectable({
  providedIn: 'root'
})
export class CocktailsService {
  private readonly http = inject(HttpClient);

  public getCocktails(): Observable<CocktailResume[]> {
    return this.http.get<CocktailResume[]>("/Cocktails");
  }

  public getCocktailById(id: string): Observable<CocktailDetail> {
    return this.http.get<CocktailDetail>(`/Cocktails/${id}`);
  }

  /**
   * Prépare `quantity` fois ce cocktail et renvoie le bar mis à jour.
   * Tout ou rien : l'API répond 409 sans rien décompter si le stock ne suffit pas.
   */
  makeCocktail(cocktailId: Guid, quantity: number = 1): Promise<MyBar> {
    return firstValueFrom(this.http.post<MyBar>(`/Bars/MakeCocktails`,
      [
        {
          cocktailId: cocktailId,
          quantity: quantity
        }
      ]
    ));
  }
}
