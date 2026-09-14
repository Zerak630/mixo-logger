using Application.Cocktails.Dtos;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.Cocktails.Queries;

public class GetCocktailByIdQuery : IRequest<CocktailDetailDto>
{
    [FromRoute]
    public Guid Id { get; init; }
}

public class GetCocktailByIdQueryHandler(ICocktailRepository cocktailRepository) : IRequestHandler<GetCocktailByIdQuery, CocktailDetailDto>
{
    public async Task<CocktailDetailDto> Handle(GetCocktailByIdQuery request, CancellationToken cancellationToken)
    {
        return new CocktailDetailDto(await cocktailRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {request.Id} not found."));
    }
}
