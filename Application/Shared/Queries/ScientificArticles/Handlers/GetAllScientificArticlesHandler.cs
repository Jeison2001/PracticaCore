using Application.Shared.DTOs;
using Application.Shared.DTOs.ScientificArticles;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.ScientificArticles.Handlers
{
    public class GetAllScientificArticlesHandler : IRequestHandler<GetAllScientificArticlesQuery, PaginatedResult<ScientificArticleWithDetailsDto>>
    {
        private readonly IScientificArticleRepository _repository;
        private readonly IMapper _mapper;

        public GetAllScientificArticlesHandler(IScientificArticleRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<ScientificArticleWithDetailsDto>> Handle(GetAllScientificArticlesQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetAllWithDetailsPaginatedAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>(),
                cancellationToken
            );

            return new PaginatedResult<ScientificArticleWithDetailsDto>
            {
                Items = _mapper.Map<List<ScientificArticleWithDetailsDto>>(result.Items),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
