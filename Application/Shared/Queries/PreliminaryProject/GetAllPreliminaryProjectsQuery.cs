using Application.Shared.DTOs.PreliminaryProject;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.PreliminaryProject
{
    public class GetAllPreliminaryProjectsQuery : IRequest<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
