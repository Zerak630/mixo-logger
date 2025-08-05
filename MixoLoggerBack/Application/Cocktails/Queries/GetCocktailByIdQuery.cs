using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Cocktails.Queries;

public class GetCocktailByIdQuery : IRequest<Cocktail>
{
    [FromRoute]
    public Guid Id { get; init; }
}

public class GetCocktailByIdQueryHandler(ICocktailRepository cocktailRepository) : IRequestHandler<GetCocktailByIdQuery, Cocktail>
{
    public async Task<Cocktail> Handle(GetCocktailByIdQuery request, CancellationToken cancellationToken)
    {
        return await cocktailRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {request.Id} not found.");
    }
}