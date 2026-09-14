using System.Globalization;
using Domain.Interfaces;

namespace Domain.Cocktails;

/// <summary>
/// Quantité d'un ingrédient dans une recette : un volume (« 5 cL de rhum ») ou un
/// décompte (« 6 feuilles de menthe », « 2 traits d'angostura »).
/// </summary>
/// <remarks>
/// Seul un volume se compare au stock. Un décompte se vérifie par la seule présence de
/// l'ingrédient dans le bar : personne ne compte ses feuilles de menthe, et le bar ne
/// suit de toute façon aucune quantité non volumique (cf. docs/MVP.md §3).
/// </remarks>
public record Dose : IValueObject
{
	public const string Piece = "piece";
	public const string Feuille = "feuille";
	public const string Trait = "trait";
	public const string Pincee = "pincee";

	/// <summary>Unités de décompte acceptées, en plus des unités de volume (mL, cL, dL, L).</summary>
	public static readonly IReadOnlyList<string> UnitesDecompte = [Piece, Feuille, Trait, Pincee];

	/// <summary>Toutes les unités acceptées, dans l'ordre où les proposer à la saisie.</summary>
	public static readonly IReadOnlyList<string> Unites =
		[UniteVolume.mL, UniteVolume.cL, UniteVolume.dL, UniteVolume.L, .. UnitesDecompte];

	public double Valeur { get; }
	public string Unite { get; }

	/// <summary>Le volume correspondant, ou <c>null</c> pour un décompte.</summary>
	public Volume? Volume { get; }

	public bool EstUnVolume => Volume is not null;

	public Dose(double valeur, string unite)
	{
		if (double.IsNaN(valeur) || double.IsInfinity(valeur) || valeur <= 0)
			throw new ArgumentOutOfRangeException(nameof(valeur), "La quantité doit être supérieure à zéro.");

		ArgumentNullException.ThrowIfNull(unite, nameof(unite));

		if (!Unites.Contains(unite))
			throw new ArgumentException(
				$"Unité inconnue : « {unite} ». Unités acceptées : {string.Join(", ", Unites)}.", nameof(unite));

		Valeur = valeur;
		Unite = unite;
		Volume = UnitesDecompte.Contains(unite) ? null : new Volume(valeur, UniteVolume.FromString(unite));
	}

	/// <summary>La dose pour <paramref name="verres"/> verres.</summary>
	public static Dose operator *(Dose dose, int verres)
	{
		if (verres < 1)
			throw new ArgumentOutOfRangeException(nameof(verres), "Le nombre de verres doit être supérieur ou égal à 1.");

		return new Dose(dose.Valeur * verres, dose.Unite);
	}

	public override string ToString() => $"{Valeur.ToString(CultureInfo.InvariantCulture)} {Unite}";
}
