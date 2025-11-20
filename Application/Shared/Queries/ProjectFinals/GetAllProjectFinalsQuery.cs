using Application.Shared.DTOs.ProjectFinals;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.ProjectFinals
{
    public class GetAllProjectFinalsQuery : IRequest<PaginatedResult<ProjectFinalWithDetailsResponseDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
