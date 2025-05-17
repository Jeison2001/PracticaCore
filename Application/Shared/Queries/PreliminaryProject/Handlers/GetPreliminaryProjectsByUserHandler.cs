using Application.Shared.DTOs.PreliminaryProject;
using Application.Shared.Queries.PreliminaryProject;
using MediatR;
using Domain.Interfaces;
using Application.Shared.DTOs.Proposal;

namespace Application.Shared.Queries.PreliminaryProject.Handlers
{
    public class GetPreliminaryProjectsByUserHandler : IRequestHandler<GetPreliminaryProjectsByUserQuery, List<PreliminaryProjectWithDetailsResponseDto>>
    {
        private readonly IPreliminaryProjectRepository _repository;
        public GetPreliminaryProjectsByUserHandler(IPreliminaryProjectRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<PreliminaryProjectWithDetailsResponseDto>> Handle(GetPreliminaryProjectsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdAsync(request.UserId, request.Status);
            // TODO: Mapear a PreliminaryProjectWithDetailsResponseDto
            return entities.Select(e => new PreliminaryProjectWithDetailsResponseDto
            {
                PreliminaryProject = new PreliminaryProjectDto { Id = e.Id }, // Map completo requerido
                Proposal = new ProposalDto() // Map completo requerido
            }).ToList();
        }
    }
}
