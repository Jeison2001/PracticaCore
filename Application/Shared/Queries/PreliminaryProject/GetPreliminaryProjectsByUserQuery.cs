using Application.Shared.DTOs.PreliminaryProject;
using MediatR;

namespace Application.Shared.Queries.PreliminaryProject
{
    public class GetPreliminaryProjectsByUserQuery : IRequest<List<PreliminaryProjectWithDetailsResponseDto>>
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
