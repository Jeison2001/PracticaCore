using Application.Shared.DTOs.PreliminaryProjects;
using MediatR;

namespace Application.Shared.Queries.PreliminaryProjects
{
    public record GetPreliminaryProjectsByUserQuery : IRequest<List<PreliminaryProjectWithDetailsResponseDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }
        public GetPreliminaryProjectsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
