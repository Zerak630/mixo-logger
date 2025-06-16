using Domain.Interfaces;

namespace Domain.Cocktails;

public record Volume(double Value, UniteVolume Unit) : IValueObject
{
	// The primary constructor already handles (double, UniteVolume), so no need for an explicit one.

	public static Volume operator +(Volume a, Volume b)
	{
		if (a.Unit != b.Unit)
			throw new InvalidOperationException("Cannot add volumes with different units");

		return new Volume(a.Value + b.Value, a.Unit);
	}

	public static Volume operator -(Volume a, Volume b)
	{
		if (a.Unit != b.Unit)
			throw new InvalidOperationException("Cannot subtract volumes with different units");

		return new Volume(a.Value - b.Value, a.Unit);
	}


}

public class UniteVolume : IValueObject
{
	public static UniteVolume Mililitre { get; } = new UniteVolume("Mililitre");
	public static UniteVolume Centilitre { get; } = new UniteVolume("Centilitre");
	public static UniteVolume Decilitre { get; } = new UniteVolume("Decilitre");
	public static UniteVolume Litre { get; } = new UniteVolume("Litre");

	private string Name { get; }

	private UniteVolume(string name)
	{
		Name = name;
	}

	public override string ToString() => Name;
	public static UniteVolume FromString(string name)
	{
		return name switch
		{
			"Mililitre" => Mililitre,
			"Centilitre" => Centilitre,
			"Decilitre" => Decilitre,
			"Litre" => Litre,
			_ => throw new ArgumentException($"Unknown unit: {name}", nameof(name))
		};
	}
}