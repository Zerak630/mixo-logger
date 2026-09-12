using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.MyBar;

/// <summary>
/// Ce que le bar possède d'un ingrédient donné.
/// </summary>
/// <remarks>
/// Deux modes coexistent volontairement :
/// <list type="bullet">
///   <item>
///     <b>possession simple</b> (<see cref="Volume"/> à <c>null</c>) : on sait que
///     l'ingrédient est là et à peu près à quel <see cref="Niveau"/>. C'est le mode
///     par défaut du grand public.
///   </item>
///   <item>
///     <b>suivi précis</b> : un <see cref="Volume"/> est renseigné et décrémenté à
///     chaque cocktail préparé. Réservé à ceux qui le veulent — et à la piste B2B,
///     où le coût matière justifie la saisie.
///   </item>
/// </list>
/// Une ligne absente du stock signifie « je n'ai pas cet ingrédient ».
/// </remarks>
public record LigneStock : IValueObject
{
    public NiveauStock Niveau { get; }
    public Volume? Volume { get; }

    public LigneStock(NiveauStock niveau, Volume? volume = null)
    {
        Niveau = niveau ?? throw new ArgumentNullException(nameof(niveau), "Le niveau ne peut pas être nul.");
        Volume = volume;
    }

    /// <summary>Vrai si cette ligne suit un volume exact, et peut donc être décrémentée.</summary>
    public bool SuiviPrecis => Volume is not null;

    /// <summary>
    /// Vrai si la ligne couvre <paramref name="requis"/>. En possession simple on répond
    /// toujours vrai : l'ingrédient est là, et un stock que personne ne tient à jour ne
    /// doit pas faire répondre « non » à tort.
    /// </summary>
    public bool Couvre(Volume requis)
    {
        ArgumentNullException.ThrowIfNull(requis);

        return !SuiviPrecis || !(Volume! < requis);
    }

    /// <summary>Retire <paramref name="consomme"/> du volume suivi ; sans effet en possession simple.</summary>
    public LigneStock Retirer(Volume consomme)
    {
        ArgumentNullException.ThrowIfNull(consomme);

        if (!SuiviPrecis)
            return this;

        if (!Couvre(consomme))
            throw new InvalidOperationException("Volume insuffisant sur cette ligne de stock.");

        return new LigneStock(Niveau, Volume! - consomme);
    }

    /// <summary>Ajoute <paramref name="ajoute"/> au volume suivi et repasse la ligne en « pleine ».</summary>
    public LigneStock Ajouter(Volume ajoute)
    {
        ArgumentNullException.ThrowIfNull(ajoute);

        return new LigneStock(NiveauStock.Pleine, SuiviPrecis ? Volume! + ajoute : ajoute);
    }
}
