using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicAverages;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages
{
    public record GetAllAcademicAveragesQuery : IRequest<PaginatedResult<AcademicAverageWithDetailsDto>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}
