using Application.Shared.DTOs;
using Application.Shared.DTOs.CoTerminals;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.CoTerminals
{
    public record GetAllCoTerminalsQuery : IRequest<PaginatedResult<CoTerminalWithDetailsDto>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
