using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.DTOs.Proposal;
using Application.Shared.Queries.ProjectFinal;
using Domain.Common;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.ProjectFinal.Handlers
{
    public class GetAllProjectFinalsHandler : IRequestHandler<GetAllProjectFinalsQuery, PaginatedResult<ProjectFinalWithDetailsResponseDto>>
    {
        private readonly IProjectFinalRepository _repository;
        public GetAllProjectFinalsHandler(IProjectFinalRepository repository)
        {
            _repository = repository;
        }
        public async Task<PaginatedResult<ProjectFinalWithDetailsResponseDto>> Handle(GetAllProjectFinalsQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllWithDetailsAsync(request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters);
            // TODO: Mapear a ProjectFinalWithDetailsResponseDto y paginar
            return new PaginatedResult<ProjectFinalWithDetailsResponseDto>
            {
                Items = entities.Select(e => new ProjectFinalWithDetailsResponseDto
                {
                    ProjectFinal = new ProjectFinalDto { Id = e.Id }, // Map completo requerido
                    Proposal = new ProposalDto() // Map completo requerido
                }).ToList(),
                TotalRecords = entities.Count
            };
        }
    }
}
