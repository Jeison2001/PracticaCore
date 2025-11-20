using Application.Shared.DTOs.Proposals;
using Domain.Common;
using MediatR;

namespace Application.Shared.Queries.Proposals
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