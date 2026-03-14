using Application.Shared.DTOs;
using Application.Shared.DTOs.Seminars;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.Seminars.Handlers
{
    public class GetSeminarsByTeacherHandler : IRequestHandler<GetSeminarsByTeacherQuery, PaginatedResult<SeminarWithDetailsDto>>
    {
        private readonly ISeminarRepository _repository;
        private readonly IMapper _mapper;

        public GetSeminarsByTeacherHandler(ISeminarRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<SeminarWithDetailsDto>> Handle(GetSeminarsByTeacherQuery request, CancellationToken cancellationToken)
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
