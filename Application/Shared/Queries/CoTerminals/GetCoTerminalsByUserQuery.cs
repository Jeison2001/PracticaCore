using Application.Shared.DTOs.CoTerminals;
using MediatR;

namespace Application.Shared.Queries.CoTerminals
{
    public record GetCoTerminalsByUserQuery : IRequest<List<CoTerminalWithDetailsDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetCoTerminalsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
