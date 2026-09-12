using Application.MyBar.Dtos;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.MyBar.Commands;

/// <summary>Corrige le niveau approximatif d'une ligne de stock existante.</summary>
public class SetIngredientNiveauCommand : IRequest<MyBarDto>
{
    public required string Name { get; init; }
    public required string Niveau { get; init; }
}

public class SetIngredientNiveauCommandHandler(
    IBarRepository barRepository,
    IIngredientRepository ingredientRepository
) : IRequestHandler<SetIngredientNiveauCommand, MyBarDto>
{
    public async Task<MyBarDto> Handle(SetIngredientNiveauCommand request, CancellationToken cancellationToken)
    {
        Bar bar = await barRepository.GetBar()
            ?? throw new InvalidOperationException("Bar not found.");

        Ingredient ingredient = await ingredientRepository.GetByNameAsync(request.Name)
            ?? throw new KeyNotFoundException($"Ingrédient inconnu : « {request.Name} ».");

        bar.SetNiveau(ingredient, NiveauStock.FromString(request.Niveau));
        await barRepository.SaveAsync(bar);

        return new MyBarDto(bar);
    }
}
