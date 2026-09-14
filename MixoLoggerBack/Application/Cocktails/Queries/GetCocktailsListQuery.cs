using Application.Cocktails.Dtos;
using Application.Utilisateurs;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.Cocktails.Queries;

public class GetCocktailsListQuery : IRequest<IEnumerable<CocktailResumeDto>>
{
}

public class GetCocktailsListQueryHandler(
    ICocktailRepository cocktailRepository,
    IBarRepository barRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<GetCocktailsListQuery, IEnumerable<CocktailResumeDto>>
{
    public async Task<IEnumerable<CocktailResumeDto>> Handle(GetCocktailsListQuery request, CancellationToken cancellationToken)
    {
        // Une seule lecture du bar pour toute la liste : chaque cocktail est évalué contre
        // le même état, même si le stock change pendant la requête. C'est le bar de
        // l'utilisateur connecté : la faisabilité dépend de ce que chacun possède.
        Bar bar = await barRepository.GetForOwnerAsync(utilisateurCourant.Id);

        return (await cocktailRepository.GetAllAsync())
            .Select(cocktail => new CocktailResumeDto(cocktail, bar.Manques(cocktail)))
            .OrderByDescending(resume => resume.Realisable)
            .ThenBy(resume => resume.Manques.Count)
            .ThenBy(resume => resume.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}
