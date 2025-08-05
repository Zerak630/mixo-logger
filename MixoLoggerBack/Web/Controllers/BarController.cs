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
        // Assuming GetBarQuery is defined in Application.MyBar.Queries
        // and returns a Bar object.
        return await mediator.Send(new GetMyBarQuery(), cancellationToken);
    }

    [HttpPost("MakeCocktails")]
    public async Task<bool> MakeCocktails(MakeCocktailCommand command, CancellationToken cancellationToken = default)
    {
        return await mediator.Send(command, cancellationToken);
    }
}