import { ResolveFn } from "@angular/router";
import { CocktailComponent } from "../../models/cocktail";
import { inject } from "@angular/core";
import { MyBarService } from "./mybar.service";

export const myBarResolver: ResolveFn<CocktailComponent[]> = (route, state) => {
    return inject(MyBarService).getMyStock();
} 