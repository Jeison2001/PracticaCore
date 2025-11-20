using Application.Shared.DTOs.AcademicPractices;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.AcademicPractices
{
    public class GetAllAcademicPracticesQuery : IRequest<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; } = false;
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();
    }
}
