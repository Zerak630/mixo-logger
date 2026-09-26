using System.Collections.Concurrent;
using Domain.Utilisateurs;

namespace Web.Securite;

/// <summary>
/// Freine le test de mots de passe sur un même compte, quelle que soit l'adresse IP d'où il vient :
/// au-delà de <see cref="EchecsMax"/> échecs sur <see cref="Fenetre"/>, les connexions à ce compte
/// sont refusées (429) jusqu'à ce que le plus ancien échec sorte de la fenêtre.
/// </summary>
/// <remarks>
/// <para>Complète la limite par IP, qui ne voit pas un essai réparti sur plusieurs adresses.</para>
/// <para>
/// Les identifiants inconnus sont comptés exactement comme les autres : la réponse ne dit jamais si
/// un compte existe. Une connexion réussie remet le compteur à zéro. Contrepartie assumée : un tiers
/// peut bloquer un compte en échouant exprès, le temps de la fenêtre seulement.
/// </para>
/// <para>En mémoire : un redémarrage remet les compteurs à zéro.</para>
/// </remarks>
public class LimiteurEchecsParCompte(int echecsMax, TimeSpan fenetre, TimeProvider horloge)
{
    /// <summary>Au-delà, les entrées expirées sont purgées : un balayage d'identifiants ne remplit pas la mémoire.</summary>
    private const int EntreesAvantPurge = 1000;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _echecs = new();

    public int EchecsMax => echecsMax;
    public TimeSpan Fenetre => fenetre;

    /// <summary>Vrai si ce compte est bloqué ; <paramref name="attente"/> dit pour combien de temps encore.</summary>
    public bool EstBloque(string identifiant, out TimeSpan attente)
    {
        attente = TimeSpan.Zero;
        if (Cle(identifiant) is not { } cle || !_echecs.TryGetValue(cle, out Queue<DateTimeOffset>? file))
            return false;

        lock (file)
        {
            DateTimeOffset maintenant = horloge.GetUtcNow();
            Purger(file, maintenant);

            if (file.Count < echecsMax)
                return false;

            attente = file.Peek() + fenetre - maintenant;
            return true;
        }
    }

    public void EnregistrerEchec(string identifiant)
    {
        if (Cle(identifiant) is not { } cle)
            return;

        if (_echecs.Count > EntreesAvantPurge)
            PurgerTout();

        Queue<DateTimeOffset> file = _echecs.GetOrAdd(cle, _ => new Queue<DateTimeOffset>());
        lock (file)
        {
            DateTimeOffset maintenant = horloge.GetUtcNow();
            Purger(file, maintenant);
            file.Enqueue(maintenant);
        }
    }

    /// <summary>Après une connexion réussie : les échecs précédents ne comptent plus.</summary>
    public void Reinitialiser(string identifiant)
    {
        if (Cle(identifiant) is { } cle)
            _echecs.TryRemove(cle, out _);
    }

    /// <summary>« Alice » et « alice » sont le même compte, pour le blocage comme pour la connexion.</summary>
    private static string? Cle(string identifiant) =>
        Utilisateur.Normaliser(identifiant) is { Length: > 0 } cle ? cle : null;

    private void Purger(Queue<DateTimeOffset> file, DateTimeOffset maintenant)
    {
        while (file.Count > 0 && file.Peek() + fenetre <= maintenant)
            file.Dequeue();
    }

    private void PurgerTout()
    {
        DateTimeOffset maintenant = horloge.GetUtcNow();

        foreach ((string cle, Queue<DateTimeOffset> file) in _echecs)
        {
            lock (file)
            {
                Purger(file, maintenant);
                if (file.Count == 0)
                    _echecs.TryRemove(cle, out _);
            }
        }
    }
}
