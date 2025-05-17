using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.Queries.ProjectFinal;
using Domain.Common;
using MediatR;
using Domain.Interfaces;
using Application.Shared.DTOs.Proposal;

namespace Application.Shared.Queries.ProjectFinal.Handlers
{
    public class GetProjectFinalsByTeacherHandler : IRequestHandler<GetProjectFinalsByTeacherQuery, PaginatedResult<ProjectFinalWithDetailsResponseDto>>
    {
        private readonly IProjectFinalRepository _repository;
        public GetProjectFinalsByTeacherHandler(IProjectFinalRepository repository)
        {
            _repository = repository;
        }
        public async Task<PaginatedResult<ProjectFinalWithDetailsResponseDto>> Handle(GetProjectFinalsByTeacherQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByTeacherIdAsync(request.TeacherId, request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters);
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
