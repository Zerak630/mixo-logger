namespace Domain.Notes;

/// <summary>
/// La note (1 à 5 étoiles) qu'un utilisateur donne à un cocktail (F7). Une seule par couple
/// cocktail / utilisateur : noter à nouveau remplace la note précédente.
/// </summary>
public class Note
{
    public const int Minimum = 1;
    public const int Maximum = 5;

    public Guid CocktailId { get; }
    public Guid UtilisateurId { get; }
    public int Valeur { get; }

    /// <summary>Date de la dernière attribution : garde l'évolution des goûts exploitable plus tard (docs/STRATEGIE.md).</summary>
    public DateTime NoteeLe { get; }

    public Note(Guid cocktailId, Guid utilisateurId, int valeur)
    {
        if (cocktailId == Guid.Empty)
            throw new ArgumentException("Le cocktail noté est invalide.", nameof(cocktailId));
        if (utilisateurId == Guid.Empty)
            throw new ArgumentException("L'auteur de la note est invalide.", nameof(utilisateurId));
        if (valeur is < Minimum or > Maximum)
            throw new ArgumentException($"La note doit être comprise entre {Minimum} et {Maximum}.", nameof(valeur));

        CocktailId = cocktailId;
        UtilisateurId = utilisateurId;
        Valeur = valeur;
        NoteeLe = DateTime.UtcNow;
    }
}

/// <summary>Les notes d'un cocktail vues par un utilisateur : la moyenne de tous, et la sienne.</summary>
/// <param name="Moyenne">Arrondie au dixième, <c>null</c> sans aucune note.</param>
/// <param name="MaNote"><c>null</c> si l'utilisateur n'a pas noté ce cocktail.</param>
public record ResumeNotes(double? Moyenne, int Nombre, int? MaNote)
{
    public static readonly ResumeNotes Aucune = new(null, 0, null);

    public static ResumeNotes Calculer(IEnumerable<Note> notes, Guid utilisateurId)
    {
        ArgumentNullException.ThrowIfNull(notes, nameof(notes));

        List<Note> liste = [.. notes];
        if (liste.Count == 0)
            return Aucune;

        // Arrondie ici plutôt qu'à l'affichage : le filtre « 4 étoiles et plus » et le « 4,0 »
        // affiché portent ainsi sur la même valeur.
        double moyenne = Math.Round(liste.Average(note => note.Valeur), 1, MidpointRounding.AwayFromZero);

        return new ResumeNotes(moyenne, liste.Count, liste.FirstOrDefault(note => note.UtilisateurId == utilisateurId)?.Valeur);
    }
}
