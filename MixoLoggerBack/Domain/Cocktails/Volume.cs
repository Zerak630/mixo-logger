using Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Cocktails;


public record Volume : IValueObject
{
	public double Value { get; }

    public UniteVolume Unit { get; }

	public Volume(double value, UniteVolume unit)
	{
		if (value < 0)
			throw new ArgumentOutOfRangeException(nameof(value), "Volume must be non-negative.");

		Value = value;
		Unit = unit ?? throw new ArgumentNullException(nameof(unit), "Unit cannot be null.");
	}

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

[JsonConverter(typeof(UniteVolumeConverter))]
public class UniteVolume : IValueObject
{
	public static UniteVolume Mililitre { get; } = new UniteVolume("mL");
	public static UniteVolume Centilitre { get; } = new UniteVolume("cL");
	public static UniteVolume Decilitre { get; } = new UniteVolume("dL");
	public static UniteVolume Litre { get; } = new UniteVolume("L");

	private string Name { get; }

	private UniteVolume(string name)
	{
		Name = name;
	}

	public override string ToString() => Name;
	public static UniteVolume FromString(string? name)
	{
		return name switch
		{
			"mL" => Mililitre,
			"cL" => Centilitre,
			"dL" => Decilitre,
			"L" => Litre,
			_ => throw new ArgumentException($"Unknown unit: {name}", nameof(name))
		};
	}
}

public class UniteVolumeConverter : JsonConverter<UniteVolume>
{
    public override UniteVolume? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return UniteVolume.FromString(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, UniteVolume value, JsonSerializerOptions options)
    {
		writer.WriteStringValue(value.ToString());
    }
}