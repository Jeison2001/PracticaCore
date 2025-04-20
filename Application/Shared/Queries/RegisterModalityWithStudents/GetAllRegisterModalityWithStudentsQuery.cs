using Application.Shared.DTOs.RegisterModalityWithStudents;
using Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries.RegisterModalityWithStudents
{
    public record GetAllRegisterModalityWithStudentsQuery : IRequest<PaginatedResult<RegisterModalityWithStudentsResponseDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
    }
}