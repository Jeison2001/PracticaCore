using Application.Shared.DTOs.ProjectPresentations;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.ProjectPresentations
{
    public record GetProjectPresentationsByTeacherQuery : IRequest<PaginatedResult<ProjectPresentationWithDetailsResponseDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
        public long? CursorId { get; set; }
        public DateTimeOffset? CursorCreatedAt { get; set; }
        public bool SkipTotalCount { get; set; }

        public GetProjectPresentationsByTeacherQuery(int teacherId, int pageNumber, int pageSize, string? sortBy, bool isDescending, Dictionary<string, string>? filters)
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
