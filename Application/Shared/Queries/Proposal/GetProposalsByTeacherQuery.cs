using Application.Shared.DTOs.Proposal;
using Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace Application.Shared.Queries.Proposal
{
    public class GetProposalsByTeacherQuery : IRequest<PaginatedResult<ProposalWithDetailsResponseDto>>
    {
        public int TeacherId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "";
        public bool IsDescending { get; init; } = false;
        public Dictionary<string, string> Filters { get; init; } = new Dictionary<string, string>();

        public GetProposalsByTeacherQuery(int teacherId)
        {
            TeacherId = teacherId;
        }

        public GetProposalsByTeacherQuery(
            int teacherId, 
            int pageNumber, 
            int pageSize, 
            string sortBy, 
            bool isDescending, 
            Dictionary<string, string> filters)
        {            TeacherId = teacherId;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortBy = sortBy;
            IsDescending = isDescending;
            Filters = filters;
        }
    }
}