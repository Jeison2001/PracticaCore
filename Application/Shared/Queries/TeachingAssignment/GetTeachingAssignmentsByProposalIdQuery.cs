using MediatR;
using Application.Shared.DTOs.TeachingAssignment;
using System.Collections.Generic;

namespace Application.Shared.Queries.TeachingAssignment
{
    public class GetTeachingAssignmentsByProposalIdQuery : IRequest<List<TeachingAssignmentTeacherDto>>
    {
        public int ProposalId { get; set; }
        public GetTeachingAssignmentsByProposalIdQuery(int proposalId)
        {
            ProposalId = proposalId;
        }
    }
}