using Application.Shared.DTOs.AcademicPractices;
using MediatR;

namespace Application.Shared.Queries.AcademicPractices
{
    public record GetAcademicPracticesByUserQuery : IRequest<List<AcademicPracticeWithDetailsResponseDto>>
    {
        public int UserId { get; set; }
        public bool? Status { get; set; }

        public GetAcademicPracticesByUserQuery(int userId, bool? status = null)
        {
            UserId = userId;
            Status = status;
        }
    }
}
