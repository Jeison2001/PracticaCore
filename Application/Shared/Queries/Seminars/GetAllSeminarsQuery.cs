using Application.Shared.DTOs;
using Application.Shared.DTOs.Seminars;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.Seminars
{
    public record GetAllSeminarsQuery : IRequest<PaginatedResult<SeminarWithDetailsDto>>
    {
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
    }
}
