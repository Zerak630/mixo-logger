using Domain.Notes;

namespace Application.Cocktails.Dtos;

/// <summary>Notes d'un cocktail vues par l'utilisateur connecté (F7).</summary>
public class NotesDto
{
    /// <summary>Moyenne arrondie au dixième ; <c>null</c> sans aucune note.</summary>
    public double? Moyenne { get; init; }

    public int Nombre { get; init; }

    /// <summary>La note de l'utilisateur connecté, <c>null</c> s'il n'a pas noté.</summary>
    public int? MaNote { get; init; }

    public NotesDto(ResumeNotes resume)
    {
        ArgumentNullException.ThrowIfNull(resume, nameof(resume));

        Moyenne = resume.Moyenne;
        Nombre = resume.Nombre;
        MaNote = resume.MaNote;
    }
}
