using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicAverages;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages
{
    public record GetAcademicAveragesByTeacherQuery : IRequest<PaginatedResult<AcademicAverageWithDetailsDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }

        public GetAcademicAveragesByTeacherQuery(int teacherId, int pageNumber, int pageSize, string sortBy, bool isDescending, Dictionary<string, string>? filters)
        {
            TeacherId = teacherId;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortBy = sortBy;
            IsDescending = isDescending;
            Filters = filters;
        }
    }
}
