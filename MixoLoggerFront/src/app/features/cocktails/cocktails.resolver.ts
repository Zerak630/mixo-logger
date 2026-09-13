import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { CocktailDetail, CocktailResume } from '../../models/cocktail';
import { CocktailsService } from './cocktails.service';

export const cocktailsListResolver: ResolveFn<CocktailResume[]> = (route, state) => {
  return inject(CocktailsService).getCocktails();
};

export const cocktailByIdResolver: ResolveFn<CocktailDetail> = (route, state) => {
  return inject(CocktailsService).getCocktailById(route.paramMap.get('id')!);
}
