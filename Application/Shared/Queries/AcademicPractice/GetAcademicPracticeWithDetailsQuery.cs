using Application.Shared.DTOs.AcademicPractice;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice
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
