using Application.Shared.DTOs.Proposal;
using MediatR;

namespace Application.Shared.Queries.Proposal
{
    public record GetProposalsByTeacherQuery : IRequest<List<ProposalWithDetailsResponseDto>>
    {
        public int TeacherId { get; init; }
        public bool? StatusFilter { get; init; }

        public GetProposalsByTeacherQuery(int teacherId, bool? statusFilter = null)
        {
            TeacherId = teacherId;
            StatusFilter = statusFilter;
        }
    }
}