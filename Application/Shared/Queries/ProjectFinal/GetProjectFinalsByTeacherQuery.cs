using Application.Shared.DTOs.ProjectFinal;
using Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries.ProjectFinal
{
    public class GetProjectFinalsByTeacherQuery : IRequest<PaginatedResult<ProjectFinalWithDetailsResponseDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
        public GetProjectFinalsByTeacherQuery(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
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
