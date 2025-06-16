namespace Domain.Cocktails;

public class CocktailIngredient
{
	public Ingredient Ingredient { get; }
	public Volume Volume { get; } // unité de mesure

	public CocktailIngredient(Ingredient ingredient, double quantite, UniteVolume unite)
	{
		if (quantite <= 0)
			throw new ArgumentOutOfRangeException(nameof(quantite), "La quantité doit être supérieure à zéro.");

		Ingredient = ingredient
			?? throw new ArgumentNullException(nameof(ingredient));

		Volume = new Volume(quantite, unite);
	}
}
