using Application.Shared.DTOs.Seminars;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.Seminars.Handlers
{
    public class GetSeminarsByUserHandler : IRequestHandler<GetSeminarsByUserQuery, List<SeminarWithDetailsDto>>
    {
        private readonly ISeminarRepository _repository;
        private readonly IMapper _mapper;

        public GetSeminarsByUserHandler(ISeminarRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<SeminarWithDetailsDto>> Handle(GetSeminarsByUserQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserAsync(request.UserId, request.Status, cancellationToken);
            return _mapper.Map<List<SeminarWithDetailsDto>>(result);
        }
    }
}
