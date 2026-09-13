using Application.Ingredients.Dtos;
using Application.Ingredients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientsController(IMediator mediator) : ControllerBase
{
    /// <summary>Référentiel complet, trié par nom — alimente l'autocomplétion de la saisie du stock.</summary>
    [HttpGet]
    public async Task<IEnumerable<IngredientReferenceDto>> GetIngredients(CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetIngredientsQuery(), cancellationToken);
    }
}
