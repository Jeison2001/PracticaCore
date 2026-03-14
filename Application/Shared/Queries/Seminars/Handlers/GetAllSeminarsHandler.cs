using Application.Shared.DTOs;
using Application.Shared.DTOs.Seminars;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.Seminars.Handlers
{
    public class GetAllSeminarsHandler : IRequestHandler<GetAllSeminarsQuery, PaginatedResult<SeminarWithDetailsDto>>
    {
        private readonly ISeminarRepository _repository;
        private readonly IMapper _mapper;

        public GetAllSeminarsHandler(ISeminarRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<SeminarWithDetailsDto>> Handle(GetAllSeminarsQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetAllWithDetailsPaginatedAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>(),
                cancellationToken
            );

            return new PaginatedResult<SeminarWithDetailsDto>
            {
                Items = _mapper.Map<List<SeminarWithDetailsDto>>(result.Items),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
