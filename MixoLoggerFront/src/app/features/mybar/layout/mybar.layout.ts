import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { LIBELLES_NIVEAU, LigneStock, MyBar } from "../../../models/bar";

@Component({
	templateUrl: "./mybar.layout.html",
	changeDetection: ChangeDetectionStrategy.Eager,
	styleUrl: "./mybar.layout.scss"
})
export default class MyBarLayout {
	/** Résolu par `myBarResolver` — provient désormais de l'API, plus d'un mock. */
	public bar = input.required<MyBar>();

	public stockList = computed<LigneStock[]>(() => this.bar().ingredients);

	public libelleNiveau(ligne: LigneStock): string {
		return LIBELLES_NIVEAU[ligne.niveau];
	}

	public libelleQuantite(ligne: LigneStock): string {
		return ligne.quantity
			? `${ligne.quantity.value} ${ligne.quantity.unit}`
			: this.libelleNiveau(ligne);
	}
}
