using Application.Shared.DTOs.ProjectFinal;
using MediatR;

namespace Application.Shared.Queries.ProjectFinal
{
    public class GetProjectFinalsByUserQuery : IRequest<List<ProjectFinalWithDetailsResponseDto>>
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
