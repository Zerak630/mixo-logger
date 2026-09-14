using Application.Cocktails.Dtos;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Cocktails.Commands;

/// <summary>
/// Remplace le contenu d'une recette. Sans comptes utilisateurs, n'importe qui peut modifier
/// n'importe quelle recette : la règle « seul l'auteur modifie » arrive avec le lot C.
/// </summary>
public class UpdateCocktailCommand : IRequest<CocktailDetailDto>
{
    public required Guid Id { get; init; }
    public required RecetteSaisie Recette { get; init; }
}

public class UpdateCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    IIngredientRepository ingredientRepository
) : IRequestHandler<UpdateCocktailCommand, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(UpdateCocktailCommand command, CancellationToken cancellationToken)
    {
        RecetteSaisie saisie = command.Recette
            ?? throw new ArgumentException("La recette est obligatoire.", nameof(command));

        Cocktail actuel = await cocktailRepository.GetByIdAsync(command.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {command.Id} not found.");

        await saisie.VerifierNomDisponibleAsync(cocktailRepository, idModifie: actuel.Id);

        Cocktail modifie = await saisie.ConstruireAsync(ingredientRepository,
            composants => actuel.Modifier(saisie.Name, composants, saisie.Etapes(), saisie.Description));

        await cocktailRepository.UpdateAsync(modifie);

        return new CocktailDetailDto(modifie);
    }
}
