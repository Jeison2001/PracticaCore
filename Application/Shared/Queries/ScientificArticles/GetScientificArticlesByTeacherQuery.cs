using Application.Shared.DTOs;
using Application.Shared.DTOs.ScientificArticles;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles
{
    public record GetScientificArticlesByTeacherQuery : IRequest<PaginatedResult<ScientificArticleWithDetailsDto>>
    {
        public int TeacherId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
        public Dictionary<string, string>? Filters { get; set; }

        /// <summary>Paginación keyset: Id de la última fila de la página anterior.</summary>
        public long? CursorId { get; set; }

        /// <summary>Paginación keyset: CreatedAt de la última fila de la página anterior.</summary>
        public DateTimeOffset? CursorCreatedAt { get; set; }

        /// <summary>Omite el COUNT del total (páginas posteriores en modo keyset).</summary>
        public bool SkipTotalCount { get; set; }

        public GetScientificArticlesByTeacherQuery(
            int teacherId,
            int pageNumber,
            int pageSize,
            string sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            long? cursorId = null,
            DateTimeOffset? cursorCreatedAt = null,
            bool skipTotalCount = false)
        {
            TeacherId = teacherId;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortBy = sortBy;
            IsDescending = isDescending;
            Filters = filters;
            CursorId = cursorId;
            CursorCreatedAt = cursorCreatedAt;
            SkipTotalCount = skipTotalCount;
        }
    }
}
