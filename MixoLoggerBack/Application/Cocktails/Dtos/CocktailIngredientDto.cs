namespace Application.Cocktails.Dtos;

public class CocktailIngredientDto
{
	public Guid IngredientId { get; set; }
	public double Quantity { get; set; }
	public string Unit { get; set; } = string.Empty;
}
