using MediatR;
using Application.Shared.DTOs.TeachingAssignments;

namespace Application.Shared.Queries.TeachingAssignments
{
    public class GetTeachingAssignmentsByProposalIdQuery : IRequest<List<TeachingAssignmentTeacherDto>>
    {
        public int ProposalId { get; set; }
        public bool? StatusRegister { get; set; }
        
        public GetTeachingAssignmentsByProposalIdQuery(int proposalId, bool? statusRegister = null)
        {
            ProposalId = proposalId;
            StatusRegister = statusRegister;
        }
    }
}