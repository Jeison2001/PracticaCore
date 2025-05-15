using MediatR;
using Application.Shared.DTOs.TeachingAssignment;

namespace Application.Shared.Queries.TeachingAssignment
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