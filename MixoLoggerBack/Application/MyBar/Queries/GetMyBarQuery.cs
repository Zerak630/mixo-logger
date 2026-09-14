using Application.MyBar.Dtos;
using Application.Utilisateurs;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.MyBar.Queries;

/// <summary>Le bar de l'utilisateur connecté (vide s'il n'y a encore rien déclaré).</summary>
public class GetMyBarQuery : IRequest<MyBarDto>
{
}

public class GetMyBarQueryHandler(
    IBarRepository barRepository,
    IUtilisateurCourant utilisateurCourant
) : IRequestHandler<GetMyBarQuery, MyBarDto>
{
    public async Task<MyBarDto> Handle(GetMyBarQuery request, CancellationToken cancellationToken)
    {
        return new MyBarDto(await barRepository.GetForOwnerAsync(utilisateurCourant.Id));
    }
}
