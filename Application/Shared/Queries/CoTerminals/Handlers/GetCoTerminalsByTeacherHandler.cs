using Application.Shared.DTOs;
using Application.Shared.DTOs.CoTerminals;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.CoTerminals.Handlers
{
    public class GetCoTerminalsByTeacherHandler : IRequestHandler<GetCoTerminalsByTeacherQuery, PaginatedResult<CoTerminalWithDetailsDto>>
    {
        private readonly ICoTerminalRepository _repository;
        private readonly IMapper _mapper;

        public GetCoTerminalsByTeacherHandler(ICoTerminalRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<CoTerminalWithDetailsDto>> Handle(GetCoTerminalsByTeacherQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByTeacherAsync(
                request.TeacherId,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>(),
                cancellationToken
            );

            return new PaginatedResult<CoTerminalWithDetailsDto>
            {
                Items = _mapper.Map<List<CoTerminalWithDetailsDto>>(result.Items),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
