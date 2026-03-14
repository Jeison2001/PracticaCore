using Application.Shared.DTOs;
using Application.Shared.DTOs.SaberPros;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.SaberPros.Handlers
{
    public class GetAllSaberProsHandler : IRequestHandler<GetAllSaberProsQuery, PaginatedResult<SaberProWithDetailsDto>>
    {
        private readonly ISaberProRepository _repository;
        private readonly IMapper _mapper;

        public GetAllSaberProsHandler(ISaberProRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<SaberProWithDetailsDto>> Handle(GetAllSaberProsQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetAllWithDetailsPaginatedAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>(),
                cancellationToken
            );

            return new PaginatedResult<SaberProWithDetailsDto>
            {
                Items = _mapper.Map<List<SaberProWithDetailsDto>>(result.Items),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
