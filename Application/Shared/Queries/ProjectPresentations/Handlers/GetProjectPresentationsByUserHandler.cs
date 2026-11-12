using Application.Shared.DTOs.ProjectPresentations;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ProjectPresentations.Handlers
{
    public class GetProjectPresentationsByUserHandler : IRequestHandler<GetProjectPresentationsByUserQuery, List<ProjectPresentationWithDetailsResponseDto>>
    {
        private readonly IProjectPresentationRepository _repository;

        public GetProjectPresentationsByUserHandler(IProjectPresentationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ProjectPresentationWithDetailsResponseDto>> Handle(GetProjectPresentationsByUserQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetByUserIdWithProposalAndStudentsAsync(request.UserId, request.Status);

            return entities
                .OrderByDescending(e => e.Presentation.CreatedAt)
                .ThenByDescending(e => e.Presentation.Id)
                .Select(ProjectPresentationResponseMapper.MapToDto)
                .ToList();
        }
    }
}
