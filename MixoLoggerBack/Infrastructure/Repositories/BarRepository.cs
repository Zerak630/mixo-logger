using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;

namespace Infrastructure.Repositories;

public class BarRepository : IBarRepository
{
    private static readonly Bar DefaultBar = new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.MinValue,
        Ingredients = new Dictionary<Ingredient, Volume>
        {
            { new Ingredient("Eau"), new Volume(1000, UniteVolume.Mililitre) },
            { new Ingredient("Sucre"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Citron"), new Volume(300, UniteVolume.Mililitre) },
            { new Ingredient("Rhum"), new Volume(700, UniteVolume.Mililitre) },
            { new Ingredient("Vodka"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Gin"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Tequila"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Triple sec"), new Volume(300, UniteVolume.Mililitre) },
            { new Ingredient("Jus d'orange"), new Volume(1000, UniteVolume.Mililitre) },
            { new Ingredient("Jus de cranberry"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Jus d'ananas"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Jus de citron vert"), new Volume(500, UniteVolume.Mililitre) },
            { new Ingredient("Crème de coco"), new Volume(300, UniteVolume.Mililitre) },
            { new Ingredient("Menthe"), new Volume(200, UniteVolume.Mililitre) }
        }
    };

    public Task<Bar> GetBar()
    {
        // In a real application, this would retrieve the bar from a database or other storage
        return Task.FromResult(DefaultBar);
    }
}