using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.MyBar.Commands;
public class MakeCocktailCommand : IRequest<bool>
{
    [FromBody]
    public required IEnumerable<CocktailBarOrder> Order { get; init; }
}

public class MakeCocktailCommandHandler(
    IBarRepository barRepository,
    ICocktailRepository cocktailRepository
) : IRequestHandler<MakeCocktailCommand, bool>
{
    public async Task<bool> Handle(MakeCocktailCommand request, CancellationToken cancellationToken)
    {
        Bar bar = await barRepository.GetBar()
            ?? throw new InvalidOperationException("Bar not found.");

        foreach (CocktailBarOrder order in request.Order)
        {
            Cocktail cocktail = await cocktailRepository.GetByIdAsync(order.CocktailId) 
                ?? throw new KeyNotFoundException($"Cocktail with ID {order.CocktailId} not found.");

            if (!bar.CanMake(cocktail))
                return false;

            bar.MakeCocktail(cocktail);     
        }

        // TODO: Update the bar in the repository if needed
        // await barRepository.UpdateAsync(bar);
        return true;
    }
}

