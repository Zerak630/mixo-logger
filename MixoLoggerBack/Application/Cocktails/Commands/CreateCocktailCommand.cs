using Application.Cocktails.Dtos;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Cocktails.Commands;

public class CreateCocktailCommand : IRequest<CocktailDetailDto>
{
    public required RecetteSaisie Recette { get; init; }
}

public class CreateCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    IIngredientRepository ingredientRepository
) : IRequestHandler<CreateCocktailCommand, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(CreateCocktailCommand command, CancellationToken cancellationToken)
    {
        RecetteSaisie saisie = command.Recette
            ?? throw new ArgumentException("La recette est obligatoire.", nameof(command));

        await saisie.VerifierNomDisponibleAsync(cocktailRepository);

        Cocktail cocktail = await saisie.ConstruireAsync(ingredientRepository,
            composants => new Cocktail(saisie.Name, composants, saisie.Etapes(), saisie.Description));

        await cocktailRepository.AddAsync(cocktail);

        return new CocktailDetailDto(cocktail);
    }
}
