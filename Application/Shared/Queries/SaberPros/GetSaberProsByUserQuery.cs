using Application.Shared.DTOs.SaberPros;
using MediatR;

namespace Application.Shared.Queries.SaberPros
{
    public record GetSaberProsByUserQuery : IRequest<List<SaberProWithDetailsDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetSaberProsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
