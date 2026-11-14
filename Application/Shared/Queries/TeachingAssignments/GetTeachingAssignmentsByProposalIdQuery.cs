using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common.Users;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments
{
    public record GetTeachingAssignmentsByProposalIdQuery : IRequest<List<TeachingAssignmentTeacherDto>>
    {
        public int ProposalId { get; set; }
        public bool? StatusRegister { get; set; }
        public CurrentUserInfo? CurrentUser { get; set; }

        public GetTeachingAssignmentsByProposalIdQuery(
            int proposalId,
            bool? statusRegister = null,
            CurrentUserInfo? currentUser = null)
        {
            ProposalId = proposalId;
            StatusRegister = statusRegister;
            CurrentUser = currentUser;
        }
    }
}
