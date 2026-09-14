import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CocktailDetail, CocktailResume, RecetteSaisie, UniteDose } from "../../models/cocktail";
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

  /** Unités de dose acceptées, dans l'ordre où les proposer. */
  public getUnites(): Observable<UniteDose[]> {
    return this.http.get<UniteDose[]>("/Cocktails/unites");
  }

  /** 400 si la recette est invalide, 409 si son nom est déjà pris. */
  public createCocktail(recette: RecetteSaisie): Observable<CocktailDetail> {
    return this.http.post<CocktailDetail>("/Cocktails", recette);
  }

  public updateCocktail(id: Guid, recette: RecetteSaisie): Observable<CocktailDetail> {
    return this.http.put<CocktailDetail>(`/Cocktails/${id}`, recette);
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
