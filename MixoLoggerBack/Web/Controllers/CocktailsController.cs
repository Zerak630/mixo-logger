using Application.Cocktails.Queries;
using Domain.Cocktails;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CocktailsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<Cocktail>> GetCocktails(CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetCocktailsListQuery(), cancellationToken);
    }

    [HttpGet("{Id}")]
    public async Task<Cocktail> GetCocktail(GetCocktailByIdQuery request, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(request, cancellationToken);
    }
}