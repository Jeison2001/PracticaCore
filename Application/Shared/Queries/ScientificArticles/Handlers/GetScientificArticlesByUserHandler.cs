using Application.Shared.DTOs.ScientificArticles;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles.Handlers
{
    public class GetScientificArticlesByUserHandler : IRequestHandler<GetScientificArticlesByUserQuery, List<ScientificArticleWithDetailsDto>>
    {
        private readonly IScientificArticleRepository _repository;
        private readonly IMapper _mapper;

        public GetScientificArticlesByUserHandler(IScientificArticleRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ScientificArticleWithDetailsDto>> Handle(GetScientificArticlesByUserQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserAsync(request.UserId, request.Status, cancellationToken);
            return _mapper.Map<List<ScientificArticleWithDetailsDto>>(result);
        }
    }
}
