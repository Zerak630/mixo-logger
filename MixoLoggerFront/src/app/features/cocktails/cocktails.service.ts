import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Cocktail, CocktailDetail } from "../../models/cocktail";
import { firstValueFrom, Observable, of } from "rxjs";
import { Guid } from "../../core/base-models";

@Injectable({
  providedIn: 'root'
})
export class CocktailsService {
  private readonly http = inject(HttpClient);

  private readonly cocktails: Cocktail[] = [
    { id: '1', name: 'Mojito', description: 'A refreshing cocktail with lime, mint, and rum.' },
    { id: '2', name: 'Old Fashioned', description: 'A classic cocktail with whiskey, bitters, and sugar.' },
    { id: '3', name: 'Margarita', description: 'A tangy cocktail with tequila, lime juice, and triple sec.' },
    { id: '4', name: 'Cosmopolitan', description: 'A stylish cocktail with vodka, cranberry juice, and lime.' },
    { id: '5', name: 'Pina Colada', description: 'A tropical cocktail with rum, coconut cream, and pineapple juice.' },
    { id: '6', name: 'Daiquiri', description: 'A simple cocktail with rum, lime juice, and sugar.' },
    { id: '7', name: 'Manhattan', description: 'A sophisticated cocktail with whiskey, sweet vermouth, and bitters.' },
    { id: '8', name: 'Negroni', description: 'A bitter cocktail with gin, Campari, and sweet vermouth.' },
    { id: '9', name: 'Whiskey Sour', description: 'A tangy cocktail with whiskey, lemon juice, and sugar.' },
    { id: '10', name: 'Bloody Mary', description: 'A savory cocktail with vodka, tomato juice, and spices.' },
    { id: '11', name: 'Gin and Tonic', description: 'A classic cocktail with gin and tonic water.' },
    { id: '12', name: 'Mai Tai', description: 'A tropical cocktail with rum, lime juice, orgeat syrup, and orange liqueur.' },
    { id: '13', name: 'Caipirinha', description: 'A Brazilian cocktail with cachaça, lime, and sugar.' },
    { id: '14', name: 'Mint Julep', description: 'A Southern cocktail with bourbon, mint, sugar, and crushed ice.' },
    { id: '15', name: 'Tom Collins', description: 'A refreshing cocktail with gin, lemon juice, sugar, and soda water.' },
    { id: '16', name: 'Aperol Spritz', description: 'An Italian cocktail with Aperol, prosecco, and soda water.' },
    { id: '17', name: 'Sazerac', description: 'A New Orleans cocktail with rye whiskey, absinthe, and bitters.' },
    { id: '18', name: 'French 75', description: 'A sparkling cocktail with gin, lemon juice, sugar, and champagne.' },
    { id: '19', name: 'Rum Punch', description: 'A fruity cocktail with rum, pineapple juice, orange juice, and grenadine.' },
    { id: '20', name: 'Paloma', description: 'A Mexican cocktail with tequila, grapefruit soda, and lime.' },
    { id: '21', name: 'Sidecar', description: 'A classic cocktail with cognac, orange liqueur, and lemon juice.' },
    { id: '22', name: 'Moscow Mule', description: 'A spicy cocktail with vodka, ginger beer, and lime.' },
    { id: '23', name: 'Tequila Sunrise', description: 'A colorful cocktail with tequila, orange juice, and grenadine.' },
    { id: '24', name: 'Irish Coffee', description: 'A warming cocktail with Irish whiskey, coffee, sugar, and cream.' },
    { id: '25', name: 'Cuba Libre', description: 'A simple cocktail with rum, cola, and lime.' },
    { id: '26', name: 'Amaretto Sour', description: 'A sweet and sour cocktail with amaretto, lemon juice, and sugar.' }
  ];

  public getCocktails(): Observable<Cocktail[]> {
    return of(this.cocktails);
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
