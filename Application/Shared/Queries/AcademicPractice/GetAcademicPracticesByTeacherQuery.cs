using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicPractice;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice
{
    public class GetAcademicPracticesByTeacherQuery : IRequest<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; } = false;
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        public GetAcademicPracticesByTeacherQuery(
            int teacherId,
            int pageNumber = 1,
            int pageSize = 10,
            string sortBy = "",
            bool isDescending = false,
            Dictionary<string, string>? filters = null)
        {
            TeacherId = teacherId;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortBy = sortBy;
            IsDescending = isDescending;
            Filters = filters ?? new Dictionary<string, string>();
        }
    }
}
