using Domain.Interfaces;

namespace Domain.Cocktails;

/// <summary>
/// Entrée du référentiel d'ingrédients.
/// </summary>
/// <remarks>
/// L'identité repose sur <see cref="NormalizedName"/>, et non sur <see cref="Id"/> :
/// un `Ingredient` construit par le référentiel et un autre construit par un seed ou
/// par une désérialisation doivent être le même ingrédient (cf. docs/MVP.md §7, B1).
///
/// Les <see cref="Aliases"/> ne participent PAS à l'égalité : les inclure la rendrait
/// non transitive (A alias de B, B nommé C…). Ils servent uniquement au référentiel
/// (<c>IIngredientRepository.GetByNameAsync</c>) pour résoudre une saisie libre vers
/// l'ingrédient canonique.
/// </remarks>
public class Ingredient : IEntity, IEquatable<Ingredient>
{
	private readonly HashSet<string> _aliases;

	public Guid Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Name { get; private set; }

	/// <summary>Clé d'identité, cf. <see cref="IngredientName.Normalize"/>.</summary>
	public string NormalizedName { get; }

	/// <summary>Autres façons de désigner cet ingrédient, sous forme normalisée.</summary>
	public IReadOnlyCollection<string> Aliases => _aliases;

	public Ingredient(string name, params string[] aliases)
	{
		Id = Guid.NewGuid();
		CreatedAt = DateTime.UtcNow;
		Name = name ?? throw new ArgumentNullException(nameof(name), "Ingredient name cannot be null");

		NormalizedName = IngredientName.Normalize(name);
		if (NormalizedName.Length == 0)
			throw new ArgumentException("Le nom d'un ingrédient ne peut pas être vide.", nameof(name));

		_aliases = [.. (aliases ?? []).Select(IngredientName.Normalize).Where(alias => alias.Length > 0)];
		_aliases.Remove(NormalizedName);
	}

	/// <summary>Vrai si <paramref name="name"/> désigne cet ingrédient, par son nom ou par un alias.</summary>
	public bool Matches(string name)
	{
		if (name is null)
			return false;

		string normalized = IngredientName.Normalize(name);
		return normalized == NormalizedName || _aliases.Contains(normalized);
	}

	public bool Equals(Ingredient? other) =>
		other is not null && NormalizedName == other.NormalizedName;

	public override bool Equals(object? obj) => Equals(obj as Ingredient);

	public override int GetHashCode() => NormalizedName.GetHashCode();

	public override string ToString() => Name;
}
