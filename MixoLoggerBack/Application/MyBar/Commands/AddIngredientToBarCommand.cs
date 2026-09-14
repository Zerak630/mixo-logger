using Application.MyBar.Dtos;
using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.MyBar.Commands;

/// <summary>
/// Déclare la possession d'un ingrédient. <see cref="Quantity"/> est facultatif :
/// sans lui, la ligne est en possession simple, ce qui est le cas nominal.
/// </summary>
public class AddIngredientToBarCommand : IRequest<MyBarDto>
{
    public required string Name { get; init; }
    public string? Niveau { get; init; }
    public VolumeInput? Quantity { get; init; }
}

/// <summary>Volume saisi par l'utilisateur, avant validation de l'unité.</summary>
public record VolumeInput(double Value, string Unit);

public class AddIngredientToBarCommandHandler(
    IBarRepository barRepository,
    IIngredientRepository ingredientRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<AddIngredientToBarCommand, MyBarDto>
{
    public async Task<MyBarDto> Handle(AddIngredientToBarCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Le nom de l'ingrédient est obligatoire.", nameof(request));

        Bar bar = await barRepository.GetForOwnerAsync(utilisateurCourant.Id);

        Ingredient ingredient = await ingredientRepository.GetOrCreateAsync(request.Name);

        if (request.Quantity is not null)
        {
            bar.AddIngredient(ingredient, new Volume(request.Quantity.Value, UniteVolume.FromString(request.Quantity.Unit)));
        }
        else
        {
            bar.AddIngredient(ingredient, NiveauStock.FromString(request.Niveau ?? NiveauStock.pleine));
        }

        await barRepository.SaveAsync(bar);

        return new MyBarDto(bar);
    }
}
