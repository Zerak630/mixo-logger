using Application.Cocktails.Dtos;
using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Cocktails.Queries;

public class GetCocktailByIdQuery : IRequest<CocktailDetailDto>
{
    [FromRoute]
    public Guid Id { get; init; }
}

public class GetCocktailByIdQueryHandler(
    ICocktailRepository cocktailRepository,
    IUtilisateurRepository utilisateurRepository,
    INoteRepository noteRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<GetCocktailByIdQuery, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(GetCocktailByIdQuery request, CancellationToken cancellationToken)
    {
        Cocktail cocktail = await cocktailRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {request.Id} not found.");

        return await CocktailDetailDto.PourAsync(cocktail, utilisateurRepository, noteRepository, utilisateurCourant);
    }
}
