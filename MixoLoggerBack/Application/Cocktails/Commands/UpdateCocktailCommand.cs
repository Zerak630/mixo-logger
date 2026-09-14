using Application.Cocktails.Dtos;
using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Cocktails.Commands;

/// <summary>Remplace le contenu d'une recette. Réservé à son auteur : 403 pour les autres.</summary>
public class UpdateCocktailCommand : IRequest<CocktailDetailDto>
{
    public required Guid Id { get; init; }
    public required RecetteSaisie Recette { get; init; }
}

public class UpdateCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    IIngredientRepository ingredientRepository,
    IUtilisateurRepository utilisateurRepository,
    INoteRepository noteRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<UpdateCocktailCommand, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(UpdateCocktailCommand command, CancellationToken cancellationToken)
    {
        RecetteSaisie saisie = command.Recette
            ?? throw new ArgumentException("La recette est obligatoire.", nameof(command));

        Cocktail actuel = await cocktailRepository.GetByIdAsync(command.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {command.Id} not found.");

        // Avant toute validation du contenu : un non-auteur n'apprend rien sur les noms pris,
        // et sa saisie ne crée aucun ingrédient dans le référentiel.
        actuel.VerifierModifiablePar(utilisateurCourant.Id);

        await saisie.VerifierNomDisponibleAsync(cocktailRepository, idModifie: actuel.Id);

        Cocktail modifie = await saisie.ConstruireAsync(ingredientRepository,
            composants => actuel.Modifier(saisie.Name, composants, saisie.Etapes(), saisie.Description));

        await cocktailRepository.UpdateAsync(modifie);

        return await CocktailDetailDto.PourAsync(modifie, utilisateurRepository, noteRepository, utilisateurCourant);
    }
}
