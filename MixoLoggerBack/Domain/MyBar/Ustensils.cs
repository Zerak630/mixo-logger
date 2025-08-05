using Domain.Interfaces;

namespace Domain.MyBar;

public class Ustensils : IEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Ustensils()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
