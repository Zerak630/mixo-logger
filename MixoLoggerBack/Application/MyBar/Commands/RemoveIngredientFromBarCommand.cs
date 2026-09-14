using Application.MyBar.Dtos;
using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.MyBar.Commands;

/// <summary>Retire un ingrédient du bar (« je n'en ai plus »).</summary>
public class RemoveIngredientFromBarCommand : IRequest<MyBarDto>
{
    public required string Name { get; init; }
}

public class RemoveIngredientFromBarCommandHandler(
    IBarRepository barRepository,
    IIngredientRepository ingredientRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<RemoveIngredientFromBarCommand, MyBarDto>
{
    public async Task<MyBarDto> Handle(RemoveIngredientFromBarCommand request, CancellationToken cancellationToken)
    {
        Bar bar = await barRepository.GetForOwnerAsync(utilisateurCourant.Id);

        Ingredient ingredient = await ingredientRepository.GetByNameAsync(request.Name)
            ?? throw new KeyNotFoundException($"Ingrédient inconnu : « {request.Name} ».");

        if (!bar.RemoveIngredient(ingredient))
            throw new KeyNotFoundException($"L'ingrédient « {ingredient.Name} » n'est pas dans le bar.");

        await barRepository.SaveAsync(bar);

        return new MyBarDto(bar);
    }
}
