namespace Domain.Cocktails;

public class CocktailIngredient
{
	public Ingredient Ingredient { get; }

	/// <summary>Quantité pour un verre : volume ou décompte (cf. <see cref="Cocktails.Dose"/>).</summary>
	public Dose Dose { get; }

	/// <summary>Raccourci vers <see cref="Dose.Volume"/> : <c>null</c> pour un décompte.</summary>
	public Volume? Volume => Dose.Volume;

	public CocktailIngredient(Ingredient ingredient, Dose dose)
	{
		Ingredient = ingredient
			?? throw new ArgumentNullException(nameof(ingredient));

		Dose = dose
			?? throw new ArgumentNullException(nameof(dose));
	}

	public CocktailIngredient(Ingredient ingredient, double quantite, UniteVolume unite)
		: this(ingredient, new Dose(quantite, unite?.ToString() ?? throw new ArgumentNullException(nameof(unite))))
	{
	}

	public CocktailIngredient(Ingredient ingredient, double quantite, string unite)
		: this(ingredient, new Dose(quantite, unite))
	{
	}
}
