using Application.Shared.DTOs.ProjectPresentations;
using MediatR;

namespace Application.Shared.Queries.ProjectPresentations
{
    public record GetProjectPresentationsByUserQuery : IRequest<List<ProjectPresentationWithDetailsResponseDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetProjectPresentationsByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
