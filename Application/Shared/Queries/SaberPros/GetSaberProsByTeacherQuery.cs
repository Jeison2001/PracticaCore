using Application.Shared.DTOs;
using Application.Shared.DTOs.SaberPros;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.SaberPros
{
    public record GetSaberProsByTeacherQuery : IRequest<PaginatedResult<SaberProWithDetailsDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }

        public GetSaberProsByTeacherQuery(int teacherId, int pageNumber, int pageSize, string sortBy, bool isDescending, Dictionary<string, string>? filters)
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
