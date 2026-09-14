using Application.Cocktails.Commands;
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
    public async Task<CocktailDetailDto> GetCocktail(GetCocktailByIdQuery request, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(request, cancellationToken);
    }

    /// <summary>Unités de dose acceptées, dans l'ordre où les proposer à la saisie.</summary>
    [HttpGet("unites")]
    public IReadOnlyList<string> GetUnites() => Dose.Unites;

    /// <summary>Crée une recette. 409 si le nom est déjà pris, 400 si le contenu est invalide.</summary>
    [HttpPost]
    public async Task<ActionResult<CocktailDetailDto>> CreateCocktail(
        [FromBody] RecetteSaisie recette,
        CancellationToken cancellationToken = default)
    {
        CocktailDetailDto cree = await mediator.Send(new CreateCocktailCommand { Recette = recette }, cancellationToken);

        return CreatedAtAction(nameof(GetCocktail), new { Id = cree.Id }, cree);
    }

    /// <summary>Remplace le contenu d'une recette existante.</summary>
    [HttpPut("{id:guid}")]
    public async Task<CocktailDetailDto> UpdateCocktail(
        Guid id,
        [FromBody] RecetteSaisie recette,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new UpdateCocktailCommand { Id = id, Recette = recette }, cancellationToken);
    }
}
