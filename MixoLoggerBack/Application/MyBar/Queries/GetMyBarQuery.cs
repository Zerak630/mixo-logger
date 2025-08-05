using Application.MyBar.Dtos;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using MediatR;

namespace Application.MyBar.Queries;

public class GetMyBarQuery : IRequest<MyBarDto>
{
}

public class GetMyBarQueryHandler(IBarRepository barRepository) : IRequestHandler<GetMyBarQuery, MyBarDto>
{
    public async Task<MyBarDto> Handle(GetMyBarQuery request, CancellationToken cancellationToken)
    {
        return new MyBarDto(await barRepository.GetBar()
            ?? throw new InvalidOperationException("Bar not found."));
    }
}