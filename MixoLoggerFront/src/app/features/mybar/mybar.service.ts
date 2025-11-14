import { Observable, of } from "rxjs";
import { CocktailComponent, UniteVolume } from "../../models/cocktail";
import { Injectable } from "@angular/core";

@Injectable({
	providedIn: "root"
})
export class MyBarService {
	private readonly INGREDIENTS: CocktailComponent[] = [
		{
			ingredient: {
				id: "a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8",
				name: "Vodka"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "b2c3d4e5-f6g7-8901-h2i3-j4k5l6m7n8o9",
				name: "Gin"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "c3d4e5f6-g7h8-9012-i3j4-k5l6m7n8o9p0",
				name: "Rhum blanc"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "d4e5f6g7-h8i9-0123-j4k5-l6m7n8o9p0q1",
				name: "Rhum ambré"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "e5f6g7h8-i9j0-1234-k5l6-m7n8o9p0q1r2",
				name: "Tequila"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "f6g7h8i9-j0k1-2345-l6m7-n8o9p0q1r2s3",
				name: "Whisky"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "g7h8i9j0-k1l2-3456-m7n8-o9p0q1r2s3t4",
				name: "Bourbon"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "h8i9j0k1-l2m3-4567-n8o9-p0q1r2s3t4u5",
				name: "Cognac"
			},
			volume: { value: 40, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "i9j0k1l2-m3n4-5678-o9p0-q1r2s3t4u5v6",
				name: "Triple sec"
			},
			volume: { value: 25, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "j0k1l2m3-n4o5-6789-p0q1-r2s3t4u5v6w7",
				name: "Curaçao bleu"
			},
			volume: { value: 25, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "k1l2m3n4-o5p6-7890-q1r2-s3t4u5v6w7x8",
				name: "Jus de citron"
			},
			volume: { value: 1, unit: UniteVolume.Litre }
		},
		{
			ingredient: {
				id: "l2m3n4o5-p6q7-8901-r2s3-t4u5v6w7x8y9",
				name: "Jus de lime"
			},
			volume: { value: 1, unit: UniteVolume.Litre }
		},
		{
			ingredient: {
				id: "m3n4o5p6-q7r8-9012-s3t4-u5v6w7x8y9z0",
				name: "Sirop de sucre"
			},
			volume: { value: 75, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "n4o5p6q7-r8s9-0123-t4u5-v6w7x8y9z0a1",
				name: "Sirop de grenadine"
			},
			volume: { value: 75, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "o5p6q7r8-s9t0-1234-u5v6-w7x8y9z0a1b2",
				name: "Sirop de menthe"
			},
			volume: { value: 75, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "p6q7r8s9-t0u1-2345-v6w7-x8y9z0a1b2c3",
				name: "Bière ginger"
			},
			volume: { value: 33, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "q7r8s9t0-u1v2-3456-w7x8-y9z0a1b2c3d4",
				name: "Soda"
			},
			volume: { value: 33, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "r8s9t0u1-v2w3-4567-x8y9-z0a1b2c3d4e5",
				name: "Tonic"
			},
			volume: { value: 33, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "s9t0u1v2-w3x4-5678-y9z0-a1b2c3d4e5f6",
				name: "Jus d'ananas"
			},
			volume: { value: 1, unit: UniteVolume.Litre }
		},
		{
			ingredient: {
				id: "t0u1v2w3-x4y5-6789-z0a1-b2c3d4e5f6g7",
				name: "Jus d'orange"
			},
			volume: { value: 1, unit: UniteVolume.Litre }
		},
		{
			ingredient: {
				id: "u1v2w3x4-y5z6-7890-a1b2-c3d4e5f6g7h8",
				name: "Jus de cranberry"
			},
			volume: { value: 1, unit: UniteVolume.Litre }
		},
		{
			ingredient: {
				id: "v2w3x4y5-z6a7-8901-b2c3-d4e5f6g7h8i9",
				name: "Crème de coco"
			},
			volume: { value: 25, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "w3x4y5z6-a7b8-9012-c3d4-e5f6g7h8i9j0",
				name: "Lait de coco"
			},
			volume: { value: 20, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "x4y5z6a7-b8c9-0123-d4e5-f6g7h8i9j0k1",
				name: "Crème fraîche"
			},
			volume: { value: 20, unit: UniteVolume.Centilitre }
		},
		// {
		// 	ingredient: {
		// 		id: "y5z6a7b8-c9d0-1234-e5f6-g7h8i9j0k1l2",
		// 		name: "Menthe fraîche"
		// 	},
		// 	volume: { value: 10, unit: UniteVolume. }
		// },
		// {
		// 	ingredient: {
		// 		id: "z6a7b8c9-d0e1-2345-f6g7-h8i9j0k1l2m3",
		// 		name: "Glaçons"
		// 	},
		// 	volume: { value: 1, unit: "kg" }
		// },
		{
			ingredient: {
				id: "a7b8c9d0-e1f2-3456-g7h8-i9j0k1l2m3n4",
				name: "Bitters"
			},
			volume: { value: 10, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "b8c9d0e1-f2g3-4567-h8i9-j0k1l2m3n4o5",
				name: "Champagne"
			},
			volume: { value: 75, unit: UniteVolume.Centilitre }
		},
		{
			ingredient: {
				id: "c9d0e1f2-g3h4-5678-i9j0-k1l2m3n4o5p6",
				name: "Vin mousseux"
			},
			volume: { value: 75, unit: UniteVolume.Centilitre }
		}
	];

	public getMyStock(): Observable<CocktailComponent[]> {
		return of(this.INGREDIENTS);
	}
}
