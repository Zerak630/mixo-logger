using Domain.Cocktails;

namespace Domain.MyBar;

/// <summary>Une ligne de commande : N fois le même cocktail.</summary>
public record CommandeCocktail
{
    public Cocktail Cocktail { get; }
    public int Quantite { get; }

    public CommandeCocktail(Cocktail cocktail, int quantite = 1)
    {
        Cocktail = cocktail ?? throw new ArgumentNullException(nameof(cocktail), "Cocktail cannot be null");

        if (quantite < 1)
            throw new ArgumentOutOfRangeException(nameof(quantite), "La quantité doit être supérieure ou égale à 1.");

        Quantite = quantite;
    }
}
