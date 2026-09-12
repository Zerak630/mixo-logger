using Domain.MyBar;

namespace Domain.Interfaces.Repositories;

public interface IBarRepository
{
    Task<Bar> GetBar();

    /// <summary>
    /// Persiste l'état du bar. Indispensable même en stockage mémoire : sans cet appel,
    /// les commandes ne « marchent » que par l'effet de bord d'une instance partagée,
    /// qui disparaîtra au passage à une vraie base (cf. docs/MVP.md §7, B10).
    /// </summary>
    Task SaveAsync(Bar bar);
}
