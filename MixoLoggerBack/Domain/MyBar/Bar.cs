
using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.MyBar;

public class Bar : IEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Dictionary<Ingredient, Volume> Ingredients { get; init; } = [];

    public Bar()
    {
        Id = Guid.NewGuid();
    }

    public void AddIngredient(Ingredient ingredient, Volume addedVolume)
    {
        if (ingredient == null)
            throw new ArgumentNullException(nameof(ingredient), "Ingredient cannot be null");

        Volume? storedVolume = Ingredients.GetValueOrDefault(ingredient);
        Ingredients[ingredient] = addedVolume + (storedVolume ?? Volume.Zero);
    }

    public bool CanMake(Cocktail cocktail)
    {
        if (cocktail == null)
            throw new ArgumentNullException(nameof(cocktail), "Cocktail cannot be null");

        foreach (var ingredient in cocktail.Ingredients)
        {
            if (!Ingredients.TryGetValue(ingredient.Ingredient, out var availableVolume) ||
                availableVolume < ingredient.Volume)
            {
                return false;
            }
        }
        return true;
    }

    public void MakeCocktail(Cocktail cocktail)
    {
        if (cocktail == null)
            throw new ArgumentNullException(nameof(cocktail), "Cocktail cannot be null");

        if (!CanMake(cocktail))
            throw new InvalidOperationException("Cannot make cocktail, not enough ingredients.");

        foreach (var ingredient in cocktail.Ingredients)
        {
            if (Ingredients.TryGetValue(ingredient.Ingredient, out var availableVolume))
            {
                Ingredients[ingredient.Ingredient] = availableVolume - ingredient.Volume;
            }
        }
    }
}