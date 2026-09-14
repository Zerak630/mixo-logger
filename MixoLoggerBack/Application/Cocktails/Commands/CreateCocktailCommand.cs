using Application.Cocktails.Dtos;
using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Cocktails.Commands;

/// <summary>Crée une recette dont l'utilisateur connecté devient l'auteur.</summary>
public class CreateCocktailCommand : IRequest<CocktailDetailDto>
{
    public required RecetteSaisie Recette { get; init; }
}

public class CreateCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    IIngredientRepository ingredientRepository,
    IUtilisateurRepository utilisateurRepository,
    INoteRepository noteRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<CreateCocktailCommand, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(CreateCocktailCommand command, CancellationToken cancellationToken)
    {
        RecetteSaisie saisie = command.Recette
            ?? throw new ArgumentException("La recette est obligatoire.", nameof(command));

        await saisie.VerifierNomDisponibleAsync(cocktailRepository);

        Cocktail cocktail = await saisie.ConstruireAsync(ingredientRepository,
            composants => new Cocktail(saisie.Name, composants, saisie.Etapes(), saisie.Description, utilisateurCourant.Id));

        await cocktailRepository.AddAsync(cocktail);

        return await CocktailDetailDto.PourAsync(cocktail, utilisateurRepository, noteRepository, utilisateurCourant);
    }
}
