using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;
namespace Application.Cocktails.Queries;

public class GetCocktailsListQuery : IRequest<IEnumerable<Cocktail>>
{
}

public class GetCocktailsListQueryHandler(ICocktailRepository cocktailRepository) : IRequestHandler<GetCocktailsListQuery, IEnumerable<Cocktail>>
{
    public async Task<IEnumerable<Cocktail>> Handle(GetCocktailsListQuery request, CancellationToken cancellationToken)
    {
        return await cocktailRepository.GetAllAsync();
    }
}