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
        Description = description
            ?? throw new ArgumentNullException(nameof(description), "La description de l'étape ne peut pas être nulle");
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