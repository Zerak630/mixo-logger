using Domain.Cocktails;
using Xunit;

namespace Domain.Tests;

public class EtapeRecetteTests
{
	[Fact]
	public void Constructeur_OrdreInferieurA1_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new EtapeRecette("Mélanger", 0));
	}

	[Fact]
	public void Constructeur_DescriptionNulle_Leve()
	{
		Assert.Throws<ArgumentNullException>(() => new EtapeRecette(null!, 1));
	}

	[Fact]
	public void FromOrderedList_NumeroteLesEtapesAPartirDe1()
	{
		var etapes = EtapeRecette.FromOrderedList(["Piler la menthe", "Ajouter le rhum", "Compléter"]).ToList();

		Assert.Equal(new[] { 1, 2, 3 }, etapes.Select(e => e.Ordre));
		Assert.Equal("Piler la menthe", etapes[0].Description);
		Assert.Equal("Compléter", etapes[2].Description);
	}

	[Fact]
	public void FromOrderedList_ListeVide_RenvoieUneSequenceVide()
	{
		Assert.Empty(EtapeRecette.FromOrderedList(Array.Empty<string>()));
	}
}
