using Application.Shared.DTOs.SaberPros;
using MediatR;

namespace Application.Shared.Queries.SaberPros
{
    public record GetSaberProWithDetailsQuery(int Id) : IRequest<SaberProWithDetailsDto>;
}
