using Domain.Interfaces;
using Domain.Units;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Cocktails;

public record Volume : IValueObject
{
	public double Value { get; }
	public UniteVolume Unit { get; }

	public static readonly Volume Zero = new(0, UniteVolume.Mililitre);
	public Volume(double value, UniteVolume unit)
	{
		if (value < 0)
			throw new ArgumentOutOfRangeException(nameof(value), "Volume must be non-negative.");

		Value = value;
		Unit = unit ?? throw new ArgumentNullException(nameof(unit), "Unit cannot be null.");
	}

	/// <summary>La même quantité exprimée en millilitres — la base de comparaison.</summary>
	[JsonIgnore]
	public double EnMillilitres => VolumeConverter.Convert(Value, Unit, UniteVolume.Mililitre);

	// B3 — l'égalité générée par le record comparait Value ET Unit sans conversion,
	// alors que < et > convertissaient : 100 mL et 10 cL étaient jugés différents.
	// On compare donc en millilitres. L'unité de saisie reste portée par l'instance
	// (« 70 cL » continue de s'afficher « 70 cL ») : elle est un détail de présentation,
	// pas une composante de l'identité du volume.
	public virtual bool Equals(Volume? other) => other is not null && Arrondi == other.Arrondi;

	public override int GetHashCode() => Arrondi.GetHashCode();

	// On compare des valeurs arrondies plutôt qu'avec une tolérance : une égalité « à
	// epsilon près » ne serait pas transitive et ne pourrait pas fournir de GetHashCode
	// cohérent — deux volumes égaux doivent avoir le même hash pour servir de clé.
	private double Arrondi => Math.Round(EnMillilitres, Precision);
	private const int Precision = 6;

	public static Volume operator +(Volume a, Volume b)
	{
		return a.Unit == b.Unit
			? new Volume(a.Value + b.Value, a.Unit)
			: new Volume(
				VolumeConverter.Convert(a, UniteVolume.Mililitre).Value + VolumeConverter.Convert(b, UniteVolume.Mililitre).Value,
				UniteVolume.Mililitre
			);
	}

	public static Volume operator -(Volume a, Volume b)
	{
		return a.Unit == b.Unit
			? new Volume(a.Value - b.Value, a.Unit)
			: new Volume(
				VolumeConverter.Convert(a, UniteVolume.Mililitre).Value - VolumeConverter.Convert(b, UniteVolume.Mililitre).Value,
				UniteVolume.Mililitre
			);
	}

	/// <summary>Multiplie un volume par un nombre de portions (commande de N cocktails).</summary>
	public static Volume operator *(Volume volume, int facteur)
	{
		if (facteur < 0)
			throw new ArgumentOutOfRangeException(nameof(facteur), "Le facteur doit être positif ou nul.");

		return new Volume(volume.Value * facteur, volume.Unit);
	}

	public static bool operator <(Volume a, Volume b)
	{
		return a.Unit == b.Unit
			? a.Value < b.Value
			: VolumeConverter.Convert(a, UniteVolume.Mililitre).Value < VolumeConverter.Convert(b, UniteVolume.Mililitre).Value;
	}

	public static bool operator >(Volume a, Volume b)
	{
		return a.Unit == b.Unit
			? a.Value > b.Value
			: VolumeConverter.Convert(a, UniteVolume.Mililitre).Value > VolumeConverter.Convert(b, UniteVolume.Mililitre).Value;
	}
}

[JsonConverter(typeof(UniteVolumeConverter))]
public record UniteVolume : IValueObject
{
	public const string mL = "mL";
	public const string cL = "cL";
	public const string dL = "dL";
	public const string L = "L";

	public static readonly UniteVolume Mililitre = new(mL);
	public static readonly UniteVolume Centilitre = new(cL);
	public static readonly UniteVolume Decilitre = new(dL);
	public static readonly UniteVolume Litre = new(L);

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
			mL => Mililitre,
			cL => Centilitre,
			dL => Decilitre,
			L => Litre,
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