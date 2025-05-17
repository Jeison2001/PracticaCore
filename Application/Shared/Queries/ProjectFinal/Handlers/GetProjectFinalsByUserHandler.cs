using Domain.Interfaces;
using Application.Shared.DTOs.ProjectFinal;
using Application.Shared.DTOs.Proposal;
using Application.Shared.Queries.ProjectFinal;
using MediatR;

namespace Application.Shared.Queries.ProjectFinal.Handlers
{
    public class GetProjectFinalsByUserHandler : IRequestHandler<GetProjectFinalsByUserQuery, List<ProjectFinalWithDetailsResponseDto>>
    {
        private readonly IProjectFinalRepository _repository;
        public GetProjectFinalsByUserHandler(IProjectFinalRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<ProjectFinalWithDetailsResponseDto>> Handle(GetProjectFinalsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdAsync(request.UserId, request.Status);
            // TODO: Mapear a ProjectFinalWithDetailsResponseDto
            return entities.Select(e => new ProjectFinalWithDetailsResponseDto
            {
                ProjectFinal = new ProjectFinalDto { Id = e.Id }, // Map completo requerido
                Proposal = new ProposalDto() // Map completo requerido
            }).ToList();
        }
    }
}
