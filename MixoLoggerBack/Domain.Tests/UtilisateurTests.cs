using Domain.Utilisateurs;
using Xunit;

namespace Domain.Tests;

public class UtilisateurTests
{
    private const string Empreinte = "empreinte-factice";

    [Theory]
    [InlineData("alice")]
    [InlineData("Alice")]
    [InlineData("  ALICE  ")]
    [InlineData("Àlice")]
    public void Id_DependDeLIdentifiantNormalise_Seulement(string variante)
    {
        // Les recettes et le bar référencent cet Id : il doit survivre à un redémarrage et à
        // une différence de saisie dans la configuration.
        Assert.Equal(new Utilisateur("alice", "Alice", Empreinte).Id, new Utilisateur(variante, "Autre nom", "autre-empreinte").Id);
    }

    [Fact]
    public void IdentifiantsDifferents_IdsDifferents()
    {
        Assert.NotEqual(new Utilisateur("alice", "", Empreinte).Id, new Utilisateur("alicia", "", Empreinte).Id);
    }

    [Fact]
    public void Id_EstUnGuidVersion5()
    {
        Guid id = new Utilisateur("alice", "", Empreinte).Id;

        Assert.Equal(5, id.Version);
        Assert.Equal(0b10, id.Variant >> 2);
    }

    [Fact]
    public void Id_EstStableDansLeTemps()
    {
        // Valeur figée, relevée sur l'implémentation livrée en F6. Changer l'espace de noms ou
        // l'algorithme ferait perdre à chacun son bar et la paternité de ses recettes en base.
        Assert.Equal(Guid.Parse("76ce2bb7-fbf2-5813-ad52-802999143544"), new Utilisateur("alice", "", Empreinte).Id);
    }

    [Fact]
    public void NomAffiche_ParDefautLIdentifiantNettoye()
    {
        Assert.Equal("Bob", new Utilisateur("  Bob ", "   ", Empreinte).NomAffiche);
        Assert.Equal("Bob L.", new Utilisateur("bob", " Bob L. ", Empreinte).NomAffiche);
    }

    [Fact]
    public void IdentifiantNormalise_IgnoreCasseAccentsEtPonctuation()
    {
        Assert.Equal("jean pierre", new Utilisateur(" Jean-Pierre ", "", Empreinte).IdentifiantNormalise);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("...")]
    public void IdentifiantVide_Leve(string identifiant)
    {
        Assert.Throws<ArgumentException>(() => new Utilisateur(identifiant, "", Empreinte));
    }

    [Fact]
    public void SansEmpreinte_Leve()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Utilisateur("alice", "", ""));
        Assert.ThrowsAny<ArgumentException>(() => new Utilisateur("alice", "", null!));
        Assert.Throws<ArgumentNullException>(() => new Utilisateur(null!, "", Empreinte));
    }
}
