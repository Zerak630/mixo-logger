using Application.Cocktails.Dtos;
using Application.Cocktails.Queries;
using Domain.Cocktails;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CocktailsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Tous les cocktails, avec leur faisabilité dans le bar courant : les réalisables
    /// d'abord, puis ceux auxquels il manque le moins d'ingrédients.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<CocktailResumeDto>> GetCocktails(CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetCocktailsListQuery(), cancellationToken);
    }

    [HttpGet("{Id}")]
    public async Task<Cocktail> GetCocktail(GetCocktailByIdQuery request, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(request, cancellationToken);
    }
}