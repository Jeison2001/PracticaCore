using Application.Shared.DTOs.CoTerminals;
using MediatR;

namespace Application.Shared.Queries.CoTerminals
{
    public record GetCoTerminalWithDetailsQuery(int Id) : IRequest<CoTerminalWithDetailsDto>;
}
