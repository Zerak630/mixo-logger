using Domain.Cocktails;
using Xunit;

namespace Domain.Tests;

/// <summary>Photo d'un cocktail par adresse (F9) : ce qui est accepté, et sous quelle forme.</summary>
public class PhotoCocktailTests
{
	private static Cocktail AvecPhoto(string? photoUrl) =>
		new("Mojito", [new CocktailIngredient(new Ingredient("Rhum"), 50, UniteVolume.Mililitre)], EtapeRecette.FromOrderedList(["Verser"]), photoUrl: photoUrl);

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void SansAdresse_PasDePhoto(string? photoUrl)
	{
		Assert.Null(AvecPhoto(photoUrl).PhotoUrl);
	}

	[Fact]
	public void AdresseHttps_ConserveeSansEspaces()
	{
		Assert.Equal("https://images.example/mojito.jpg?taille=800", AvecPhoto("  https://images.example/mojito.jpg?taille=800 ").PhotoUrl);
	}

	[Fact]
	public void AdresseHttps_MiseSousFormeCanonique()
	{
		// Schéma et hôte en minuscules, espace échappée : c'est cette forme qui ira dans l'attribut src.
		Assert.Equal("https://images.example/mon%20mojito.jpg", AvecPhoto("HTTPS://Images.Example/mon mojito.jpg").PhotoUrl);
	}

	[Theory]
	[InlineData("http://images.example/mojito.jpg")]
	[InlineData("ftp://images.example/mojito.jpg")]
	[InlineData("javascript:alert(1)")]
	[InlineData("data:image/png;base64,iVBORw0KGgo=")]
	[InlineData("file:///etc/passwd")]
	[InlineData("/images/mojito.jpg")]
	[InlineData("images.example/mojito.jpg")]
	[InlineData("pas une adresse")]
	public void AutreQuUneAdresseHttpsComplete_Leve(string photoUrl)
	{
		var erreur = Assert.Throws<ArgumentException>(() => AvecPhoto(photoUrl));

		Assert.StartsWith("La photo doit être une adresse complète commençant par https://.", erreur.Message);
	}

	[Fact]
	public void AdresseAvecIdentifiants_Leve()
	{
		var erreur = Assert.Throws<ArgumentException>(() => AvecPhoto("https://alice:secret@images.example/mojito.jpg"));

		Assert.StartsWith("L'adresse de la photo ne doit pas contenir d'identifiant", erreur.Message);
	}

	[Fact]
	public void AdresseTropLongue_Leve()
	{
		const string prefixe = "https://images.example/";
		string limite = prefixe + new string('a', Cocktail.LongueurMaxPhotoUrl - prefixe.Length);

		Assert.Equal(limite, AvecPhoto(limite).PhotoUrl);
		Assert.Throws<ArgumentException>(() => AvecPhoto(limite + "a"));
	}

	[Fact]
	public void AdresseQuiSAllongeEnFormeCanonique_TropLongue_Leve()
	{
		// Pile 2000 caractères saisis, mais chaque espace devient « %20 » une fois échappée.
		string debut = "https://images.example/" + string.Concat(Enumerable.Repeat("a b", 10));
		string saisie = debut + new string('a', Cocktail.LongueurMaxPhotoUrl - debut.Length);

		Assert.Equal(Cocktail.LongueurMaxPhotoUrl, saisie.Length);
		Assert.Throws<ArgumentException>(() => AvecPhoto(saisie));
	}

	[Fact]
	public void Modifier_RemplaceOuRetireLaPhoto()
	{
		Cocktail original = AvecPhoto("https://images.example/avant.jpg");
		CocktailIngredient rhum = new(new Ingredient("Rhum"), 50, UniteVolume.Mililitre);

		Cocktail remplacee = original.Modifier("Mojito", [rhum], EtapeRecette.FromOrderedList(["Verser"]), photoUrl: "https://images.example/apres.jpg");
		Cocktail retiree = original.Modifier("Mojito", [rhum], EtapeRecette.FromOrderedList(["Verser"]));

		Assert.Equal("https://images.example/apres.jpg", remplacee.PhotoUrl);
		Assert.Null(retiree.PhotoUrl);
	}

	[Fact]
	public void Reconstituer_ValideLaPhotoCommeALaCreation()
	{
		Assert.Throws<ArgumentException>(() => Cocktail.Reconstituer(
			Guid.NewGuid(), "Mojito", [new CocktailIngredient(new Ingredient("Rhum"), 50, UniteVolume.Mililitre)],
			EtapeRecette.FromOrderedList(["Verser"]), null, null, "javascript:alert(1)"));
	}
}
