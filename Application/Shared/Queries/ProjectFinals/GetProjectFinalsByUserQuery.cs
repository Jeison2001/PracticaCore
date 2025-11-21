using Application.Shared.DTOs.ProjectFinals;
using MediatR;

namespace Application.Shared.Queries.ProjectFinals
{
    public record GetProjectFinalsByUserQuery : IRequest<List<ProjectFinalWithDetailsResponseDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }
        public GetProjectFinalsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
