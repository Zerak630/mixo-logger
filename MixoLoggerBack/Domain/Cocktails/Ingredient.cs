using Domain.Interfaces;

namespace Domain.Cocktails;

public class Ingredient : IEntity
{
	public Guid Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Name { get; private set; }

	public Ingredient(string name)
	{
		Id = Guid.NewGuid();
		CreatedAt = DateTime.UtcNow;
		Name = name ?? throw new ArgumentNullException(nameof(name), "Ingredient name cannot be null");
	}
}
