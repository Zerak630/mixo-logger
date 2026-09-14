using Domain.Interfaces.Repositories;
using Domain.MyBar;

namespace Infrastructure.Repositories;

/// <summary>Un bar par utilisateur, en mémoire, indexé par <see cref="Bar.OwnerId"/>.</summary>
public class BarRepository : IBarRepository
{
    private static readonly Lock _verrou = new();
    private static readonly Dictionary<Guid, Bar> _bars = [];

    /// <summary>
    /// Renvoie une copie : chaque requête travaille sur sa propre instance et publie
    /// son résultat via <see cref="SaveAsync"/>. Sans cela, deux requêtes concurrentes
    /// mutent le même dictionnaire (cf. docs/MVP.md §7, B2).
    /// </summary>
    /// <remarks>
    /// Un bar vide n'est pas stocké à la simple lecture : consulter son bar ne crée rien.
    /// </remarks>
    public Task<Bar> GetForOwnerAsync(Guid ownerId)
    {
        lock (_verrou)
        {
            return Task.FromResult(_bars.TryGetValue(ownerId, out Bar? bar)
                ? bar.Snapshot()
                : new Bar(ownerId));
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
            // Un bar jamais enregistré est en version 0 : deux premières sauvegardes
            // concurrentes se départagent comme les suivantes.
            int versionStockee = _bars.TryGetValue(bar.OwnerId, out Bar? actuel) ? actuel.Version : 0;

            if (bar.Version != versionStockee)
                throw new ConflitDeConcurrenceException(
                    "Le bar a été modifié entre-temps. Recharge-le puis recommence.");

            Bar publie = bar.Snapshot();
            publie.Version = versionStockee + 1;
            _bars[bar.OwnerId] = publie;        }

        return Task.CompletedTask;
    }
}
