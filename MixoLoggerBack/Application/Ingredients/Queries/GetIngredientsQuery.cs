using Application.Ingredients.Dtos;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Ingredients.Queries;

public class GetIngredientsQuery : IRequest<IEnumerable<IngredientReferenceDto>>
{
}

public class GetIngredientsQueryHandler(IIngredientRepository ingredientRepository)
    : IRequestHandler<GetIngredientsQuery, IEnumerable<IngredientReferenceDto>>
{
    public async Task<IEnumerable<IngredientReferenceDto>> Handle(GetIngredientsQuery request, CancellationToken cancellationToken)
    {
        return (await ingredientRepository.GetAllAsync())
            .Select(ingredient => new IngredientReferenceDto(ingredient))
            .ToList();
    }
}
