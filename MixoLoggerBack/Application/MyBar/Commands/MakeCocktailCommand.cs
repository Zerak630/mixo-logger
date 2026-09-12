using Application.MyBar.Dtos;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Application.MyBar.Commands;

public class MakeCocktailCommand : IRequest<MyBarDto>
{
    [FromBody]
    public required IEnumerable<CocktailBarOrder> Order { get; init; }
}

public class MakeCocktailCommandHandler(
    IBarRepository barRepository,
    ICocktailRepository cocktailRepository
) : IRequestHandler<MakeCocktailCommand, MyBarDto>
{
    public async Task<MyBarDto> Handle(MakeCocktailCommand request, CancellationToken cancellationToken)
    {
        Bar bar = await barRepository.GetBar()
            ?? throw new InvalidOperationException("Bar not found.");

        // On construit d'abord la commande entière : `MakeCocktails` valide la totalité
        // des besoins — quantités comprises — avant de consommer quoi que ce soit, et
        // lève sans rien entamer si elle est infaisable (cf. docs/MVP.md §7, B11/B12).
        List<CommandeCocktail> commande = [];

        foreach (CocktailBarOrder order in request.Order)
        {
            Cocktail cocktail = await cocktailRepository.GetByIdAsync(order.CocktailId)
                ?? throw new KeyNotFoundException($"Cocktail with ID {order.CocktailId} not found.");

            commande.Add(new CommandeCocktail(cocktail, order.Quantity));
        }

        bar.MakeCocktails(commande);
        await barRepository.SaveAsync(bar);

        return new MyBarDto(bar);
    }
}
