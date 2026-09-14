using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Cocktails.Commands;

/// <summary>Supprime une recette. Réservé à son auteur : 403 pour les autres et pour les recettes d'origine.</summary>
public class DeleteCocktailCommand : IRequest
{
    public required Guid Id { get; init; }
}

public class DeleteCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    INoteRepository noteRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<DeleteCocktailCommand>
{
    public async Task Handle(DeleteCocktailCommand command, CancellationToken cancellationToken)
    {
        Cocktail cocktail = await cocktailRepository.GetByIdAsync(command.Id)
            ?? throw new KeyNotFoundException($"Cocktail with ID {command.Id} not found.");

        cocktail.VerifierModifiablePar(utilisateurCourant.Id);

        await cocktailRepository.DeleteAsync(cocktail);
        // Après la suppression seulement : un refus (409) ne doit pas effacer les notes.
        await noteRepository.RetirerPourCocktailAsync(cocktail.Id);
    }
}
