using Application.Shared.DTOs.PreliminaryProjects;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.PreliminaryProjects
{
    public record GetPreliminaryProjectsByTeacherQuery : IRequest<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
        public GetPreliminaryProjectsByTeacherQuery(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
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
