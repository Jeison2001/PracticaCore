using Application.Shared.DTOs.ScientificArticles;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles.Handlers
{
    public class GetScientificArticleWithDetailsQueryHandler : IRequestHandler<GetScientificArticleWithDetailsQuery, ScientificArticleWithDetailsDto>
    {
        private readonly IScientificArticleRepository _repository;
        private readonly IMapper _mapper;

        public GetScientificArticleWithDetailsQueryHandler(IScientificArticleRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ScientificArticleWithDetailsDto> Handle(GetScientificArticleWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetWithDetailsAsync(request.Id);
            if (entity == null) throw new KeyNotFoundException($"ScientificArticle with ID {request.Id} not found.");
            return _mapper.Map<ScientificArticleWithDetailsDto>(entity);
        }
    }
}
