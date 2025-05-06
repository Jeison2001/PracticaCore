using Application.Shared.DTOs;
using Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries
{
    public record GetDocumentsByInscriptionModalityQuery : IRequest<PaginatedResult<DocumentDto>>
    {
        public int IdInscriptionModality { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }

        public GetDocumentsByInscriptionModalityQuery(
            int idInscriptionModality,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortBy = null,
            bool isDescending = false,
            Dictionary<string, string>? filters = null)
        {
            IdInscriptionModality = idInscriptionModality;
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 10 : pageSize;
            SortBy = sortBy;
            IsDescending = isDescending;
            Filters = filters;
        }
    }
}
