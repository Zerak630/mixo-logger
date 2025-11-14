import { Component, input } from "@angular/core";
import { CocktailComponent } from "../../../models/cocktail";

@Component({
	templateUrl: "./mybar.layout.html",
	styleUrl: "./mybar.layout.scss"
})
export default class MyBarLayout {
	public stockList = input.required<CocktailComponent[]>();
}
