using Application.Shared.DTOs.AcademicPractice;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice
{
    public class GetAcademicPracticesByUserQuery : IRequest<List<AcademicPracticeWithDetailsResponseDto>>
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
