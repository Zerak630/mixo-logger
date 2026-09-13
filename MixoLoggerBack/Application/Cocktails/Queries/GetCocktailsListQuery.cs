using Application.Cocktails.Dtos;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.Cocktails.Queries;

public class GetCocktailsListQuery : IRequest<IEnumerable<CocktailResumeDto>>
{
}

public class GetCocktailsListQueryHandler(
    ICocktailRepository cocktailRepository,
    IBarRepository barRepository
) : IRequestHandler<GetCocktailsListQuery, IEnumerable<CocktailResumeDto>>
{
    public async Task<IEnumerable<CocktailResumeDto>> Handle(GetCocktailsListQuery request, CancellationToken cancellationToken)
    {
        // Une seule lecture du bar pour toute la liste : chaque cocktail est évalué contre
        // le même état, même si le stock change pendant la requête.
        Bar bar = await barRepository.GetBar()
            ?? throw new InvalidOperationException("Bar not found.");

        return (await cocktailRepository.GetAllAsync())
            .Select(cocktail => new CocktailResumeDto(cocktail, bar.Manques(cocktail)))
            .OrderByDescending(resume => resume.Realisable)
            .ThenBy(resume => resume.Manques.Count)
            .ThenBy(resume => resume.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}
