using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.InscriptionWithStudents
{
    public record GetAllInscriptionWithStudentsQuery : IRequest<PaginatedResult<InscriptionWithStudentsResponseDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
    }
}