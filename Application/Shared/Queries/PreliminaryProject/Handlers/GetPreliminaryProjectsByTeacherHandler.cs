using Application.Shared.DTOs.PreliminaryProject;
using Application.Shared.Queries.PreliminaryProject;
using Domain.Common;
using MediatR;
using Domain.Interfaces;
using Application.Shared.DTOs.Proposal;
using System.Linq;

namespace Application.Shared.Queries.PreliminaryProject.Handlers
{
    public class GetPreliminaryProjectsByTeacherHandler : IRequestHandler<GetPreliminaryProjectsByTeacherQuery, PaginatedResult<PreliminaryProjectWithDetailsResponseDto>>
    {
        private readonly IPreliminaryProjectRepository _repository;
        public GetPreliminaryProjectsByTeacherHandler(IPreliminaryProjectRepository repository)
        {
            _repository = repository;
        }
        public async Task<PaginatedResult<PreliminaryProjectWithDetailsResponseDto>> Handle(GetPreliminaryProjectsByTeacherQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByTeacherIdAsync(request.TeacherId, request.PageNumber, request.PageSize, request.SortBy, request.IsDescending, request.Filters);
            // TODO: Mapear a PreliminaryProjectWithDetailsResponseDto y paginar
            return new PaginatedResult<PreliminaryProjectWithDetailsResponseDto>
            {
                Items = entities.Select(e => new PreliminaryProjectWithDetailsResponseDto
                {
                    PreliminaryProject = new PreliminaryProjectDto { Id = e.Id }, // Map completo requerido
                    Proposal = new ProposalDto() // Map completo requerido
                }).ToList(),
                TotalRecords = entities.Count
            };
        }
    }
}
