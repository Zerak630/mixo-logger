import { Component, input, ChangeDetectionStrategy } from "@angular/core";
import { CocktailComponent } from "../../../models/cocktail";

@Component({
	templateUrl: "./mybar.layout.html",
	changeDetection: ChangeDetectionStrategy.Eager,
	styleUrl: "./mybar.layout.scss"
})
export default class MyBarLayout {
	public stockList = input.required<CocktailComponent[]>();
}
