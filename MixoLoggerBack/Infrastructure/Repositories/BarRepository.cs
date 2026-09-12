using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;

namespace Infrastructure.Repositories;

public class BarRepository : IBarRepository
{
    // Bar unique et global : le multi-utilisateur (Bar.OwnerId) est au lot C2.
    private static readonly Lock _verrou = new();
    private static Bar _bar = Seed();

    /// <summary>
    /// Renvoie une copie : chaque requête travaille sur sa propre instance et publie
    /// son résultat via <see cref="SaveAsync"/>. Sans cela, deux requêtes concurrentes
    /// mutent le même dictionnaire (cf. docs/MVP.md §7, B2).
    /// </summary>
    public Task<Bar> GetBar()
    {
        lock (_verrou)
        {
            return Task.FromResult(_bar.Snapshot());
        }
    }

    /// <summary>
    /// Publie le bar, à condition qu'il ait été lu dans la version actuellement stockée.
    /// Sinon une autre requête a écrit entre-temps : on refuse plutôt que d'écraser
    /// silencieusement sa modification.
    /// </summary>
    /// <exception cref="ConflitDeConcurrenceException">La version lue est périmée.</exception>
    public Task SaveAsync(Bar bar)
    {
        ArgumentNullException.ThrowIfNull(bar);

        lock (_verrou)
        {
            if (bar.Version != _bar.Version)
                throw new ConflitDeConcurrenceException(
                    "Le bar a été modifié entre-temps. Recharge-le puis recommence.");

            Bar publie = bar.Snapshot();
            publie.Version = _bar.Version + 1;
            _bar = publie;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stock de départ. Volontairement en possession simple (sans volume) pour la
    /// plupart des lignes : c'est le mode nominal du grand public. Quelques bouteilles
    /// sont en suivi précis pour que le décompte de `MakeCocktails` reste démontrable.
    /// </summary>
    private static Bar Seed()
    {
        Bar bar = new() { CreatedAt = DateTime.MinValue };

        string[] possessionSimple =
        [
            "Eau", "Sirop de sucre", "Citron", "Vodka", "Gin", "Tequila", "Triple sec",
            "Jus d'orange", "Jus de cranberry", "Jus d'ananas", "Citron vert",
            "Crème de coco", "Menthe", "Eau gazeuse", "Glace pilée"
        ];

        foreach (string name in possessionSimple)
        {
            bar.AddIngredient(IngredientReferentiel.Resolve(name));
        }

        bar.AddIngredient(IngredientReferentiel.Resolve("Rhum blanc"), new Volume(700, UniteVolume.Mililitre));
        bar.AddIngredient(IngredientReferentiel.Resolve("Sirop de grenadine"), new Volume(250, UniteVolume.Mililitre));

        return bar;
    }
}
