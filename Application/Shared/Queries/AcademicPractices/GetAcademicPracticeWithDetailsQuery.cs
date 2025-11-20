using Application.Shared.DTOs.AcademicPractices;
using MediatR;

namespace Application.Shared.Queries.AcademicPractices
{
    public class GetAcademicPracticeWithDetailsQuery : IRequest<AcademicPracticeWithDetailsResponseDto>
    {
        public int Id { get; set; }

        public GetAcademicPracticeWithDetailsQuery(int id)
        {
            Id = id;
        }
    }
}
