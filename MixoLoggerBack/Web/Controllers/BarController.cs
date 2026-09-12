using Application.MyBar.Commands;
using Application.MyBar.Dtos;
using Application.MyBar.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<MyBarDto> GetBar(CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetMyBarQuery(), cancellationToken);
    }

    /// <summary>Prépare une commande. Tout ou rien : rien n'est décompté si une ligne est infaisable.</summary>
    [HttpPost("MakeCocktails")]
    public async Task<MyBarDto> MakeCocktails(MakeCocktailCommand command, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>Déclare la possession d'un ingrédient (volume facultatif).</summary>
    [HttpPost("ingredients")]
    public async Task<MyBarDto> AddIngredient(
        [FromBody] AddIngredientToBarCommand command,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>Corrige le niveau d'une ligne de stock.</summary>
    [HttpPatch("ingredients/{name}")]
    public async Task<MyBarDto> SetNiveau(
        string name,
        [FromBody] SetNiveauRequest body,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(
            new SetIngredientNiveauCommand { Name = name, Niveau = body.Niveau },
            cancellationToken);
    }

    [HttpDelete("ingredients/{name}")]
    public async Task<MyBarDto> RemoveIngredient(string name, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new RemoveIngredientFromBarCommand { Name = name }, cancellationToken);
    }
}

public record SetNiveauRequest(string Niveau);
