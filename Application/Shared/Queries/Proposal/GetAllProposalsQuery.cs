using Application.Shared.DTOs.Proposal;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.Proposal
{
    public record GetAllProposalsQuery : IRequest<PaginatedResult<ProposalWithDetailsResponseDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string>? Filters { get; init; }
    }
}