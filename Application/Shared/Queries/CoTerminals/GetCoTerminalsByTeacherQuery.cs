using Application.Shared.DTOs;
using Application.Shared.DTOs.CoTerminals;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.CoTerminals
{
    public record GetCoTerminalsByTeacherQuery : IRequest<PaginatedResult<CoTerminalWithDetailsDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }

        public GetCoTerminalsByTeacherQuery(int teacherId, int pageNumber, int pageSize, string sortBy, bool isDescending, Dictionary<string, string>? filters)
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
