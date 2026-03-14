using Application.Shared.DTOs;
using Application.Shared.DTOs.AcademicAverages;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages.Handlers
{
    public class GetAllAcademicAveragesHandler : IRequestHandler<GetAllAcademicAveragesQuery, PaginatedResult<AcademicAverageWithDetailsDto>>
    {
        private readonly IAcademicAverageRepository _repository;
        private readonly IMapper _mapper;

        public GetAllAcademicAveragesHandler(IAcademicAverageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<AcademicAverageWithDetailsDto>> Handle(GetAllAcademicAveragesQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetAllWithDetailsPaginatedAsync(
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters ?? new Dictionary<string, string>(),
                cancellationToken
            );

            return new PaginatedResult<AcademicAverageWithDetailsDto>
            {
                Items = _mapper.Map<List<AcademicAverageWithDetailsDto>>(result.Items),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
