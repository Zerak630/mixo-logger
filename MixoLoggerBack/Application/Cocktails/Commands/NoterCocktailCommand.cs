using Application.Cocktails.Dtos;
using Application.Utilisateurs;
using Domain.Interfaces.Repositories;
using Domain.Notes;
using MediatR;

namespace Application.Cocktails.Commands;

/// <summary>Donne ou change sa note (1 à 5) pour un cocktail. Tout utilisateur peut noter toute recette.</summary>
public class NoterCocktailCommand : IRequest<NotesDto>
{
    public required Guid CocktailId { get; init; }
    public required int Valeur { get; init; }
}

/// <summary>Retire sa note d'un cocktail. Idempotent : sans note, rien ne change.</summary>
public class RetirerNoteCommand : IRequest<NotesDto>
{
    public required Guid CocktailId { get; init; }
}

public class NoterCocktailCommandHandler(
    ICocktailRepository cocktailRepository,
    INoteRepository noteRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<NoterCocktailCommand, NotesDto>, IRequestHandler<RetirerNoteCommand, NotesDto>
{
    public async Task<NotesDto> Handle(NoterCocktailCommand command, CancellationToken cancellationToken)
    {
        await VerifierExistenceAsync(command.CocktailId);

        await noteRepository.DefinirAsync(new Note(command.CocktailId, utilisateurCourant.Id, command.Valeur));

        return await ResumeAsync(command.CocktailId);
    }

    public async Task<NotesDto> Handle(RetirerNoteCommand command, CancellationToken cancellationToken)
    {
        await VerifierExistenceAsync(command.CocktailId);

        await noteRepository.RetirerAsync(command.CocktailId, utilisateurCourant.Id);

        return await ResumeAsync(command.CocktailId);
    }

    private async Task VerifierExistenceAsync(Guid cocktailId)
    {
        if (await cocktailRepository.GetByIdAsync(cocktailId) is null)
            throw new KeyNotFoundException($"Cocktail with ID {cocktailId} not found.");
    }

    private async Task<NotesDto> ResumeAsync(Guid cocktailId) =>
        new(ResumeNotes.Calculer(await noteRepository.GetByCocktailAsync(cocktailId), utilisateurCourant.Id));
}
