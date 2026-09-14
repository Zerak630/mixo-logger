using Domain.Interfaces;

namespace Domain.Cocktails;

public class EtapeRecette : IEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; }
    public int Ordre { get; }

    public EtapeRecette(string description, int ordre)
    {
        Id = Guid.NewGuid();
        ArgumentNullException.ThrowIfNull(description, nameof(description));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Une étape de préparation ne peut pas être vide.", nameof(description));
        Description = description.Trim();
        if (ordre < 1)
            throw new ArgumentOutOfRangeException(nameof(ordre), "L'ordre de l'étape doit être supérieur ou égal à 1");
        Ordre = ordre;
        CreatedAt = DateTime.UtcNow;
    }

    public static IEnumerable<EtapeRecette> FromOrderedList(IEnumerable<string> etapes)
    {
        return etapes
            .Select((description, index) => new EtapeRecette(description, index + 1))
            .OrderBy(etape => etape.Ordre);
    }
}