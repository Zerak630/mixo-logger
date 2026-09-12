using System.Globalization;
using System.Text;

namespace Domain.Cocktails;

/// <summary>
/// Normalisation des noms d'ingrédients : c'est cette forme normalisée qui porte
/// l'identité d'un <see cref="Ingredient"/> (cf. docs/MVP.md §7, B1).
/// </summary>
public static class IngredientName
{
	/// <summary>
	/// « Crème de Coco » et « creme  de   coco » donnent la même clé « creme de coco ».
	/// Les accents sont retirés, la casse abaissée, et toute suite de caractères non
	/// alphanumériques est réduite à une espace simple.
	/// </summary>
	public static string Normalize(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		string decomposed = name.Trim().Normalize(NormalizationForm.FormD);
		StringBuilder builder = new(decomposed.Length);
		bool pendingSeparator = false;

		foreach (char c in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
				continue;

			if (char.IsLetterOrDigit(c))
			{
				if (pendingSeparator && builder.Length > 0)
					builder.Append(' ');

				pendingSeparator = false;
				builder.Append(char.ToLowerInvariant(c));
			}
			else
			{
				pendingSeparator = true;
			}
		}

		return builder.ToString().Normalize(NormalizationForm.FormC);
	}
}
