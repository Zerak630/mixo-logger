using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Interfaces;

namespace Domain.MyBar;

/// <summary>
/// Niveau approximatif d'une bouteille. C'est la granularité que suit réellement
/// un utilisateur grand public : personne ne saisit « il me reste 437 mL de rhum ».
/// Le suivi précis reste possible via <see cref="LigneStock.Volume"/>, mais il est
/// facultatif (cf. docs/STRATEGIE.md §3).
/// </summary>
[JsonConverter(typeof(NiveauStockConverter))]
public record NiveauStock : IValueObject
{
	public const string pleine = "Pleine";
	public const string entamee = "Entamee";
	public const string presqueFinie = "PresqueFinie";

	public static readonly NiveauStock Pleine = new(pleine);
	public static readonly NiveauStock Entamee = new(entamee);
	public static readonly NiveauStock PresqueFinie = new(presqueFinie);

	private string Name { get; }

	private NiveauStock(string name)
	{
		Name = name;
	}

	public override string ToString() => Name;

	public static NiveauStock FromString(string? name)
	{
		return name switch
		{
			pleine => Pleine,
			entamee => Entamee,
			presqueFinie => PresqueFinie,
			// Message affiché tel quel dans l'interface (400) : en français.
			_ => throw new ArgumentException($"Niveau inconnu : « {name} ». Niveaux acceptés : {pleine}, {entamee}, {presqueFinie}.", nameof(name))
		};
	}
}

public class NiveauStockConverter : JsonConverter<NiveauStock>
{
	public override NiveauStock? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return NiveauStock.FromString(reader.GetString());
	}

	public override void Write(Utf8JsonWriter writer, NiveauStock value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
