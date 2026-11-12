using Application.Shared.DTOs.ProjectPresentations;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ProjectPresentations.Handlers
{
    public class GetAllProjectPresentationsHandler : IRequestHandler<GetAllProjectPresentationsQuery, PaginatedResult<ProjectPresentationWithDetailsResponseDto>>
    {
        private readonly IProjectPresentationRepository _repository;

        public GetAllProjectPresentationsHandler(IProjectPresentationRepository repository)
        {
            _repository = repository;
        }

        public async Task<PaginatedResult<ProjectPresentationWithDetailsResponseDto>> Handle(GetAllProjectPresentationsQuery request, CancellationToken cancellationToken)
        {
            var pagedResult = await _repository.GetAllWithProposalAndStudentsAsync(
                request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters,
                request.CursorId, request.CursorCreatedAt, request.SkipTotalCount);

            return new PaginatedResult<ProjectPresentationWithDetailsResponseDto>
            {
                Items = pagedResult.Items.Select(ProjectPresentationResponseMapper.MapToDto).ToList(),
                TotalRecords = pagedResult.TotalRecords,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                HasMoreRows = pagedResult.HasMoreRows
            };
        }
    }
}
