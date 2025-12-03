using Application.Shared.DTOs.Seminars;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.Seminars.Handlers
{
    public class GetSeminarWithDetailsQueryHandler : IRequestHandler<GetSeminarWithDetailsQuery, SeminarWithDetailsDto>
    {
        private readonly ISeminarRepository _repository;
        private readonly IMapper _mapper;

        public GetSeminarWithDetailsQueryHandler(ISeminarRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SeminarWithDetailsDto> Handle(GetSeminarWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetWithDetailsAsync(request.Id);
            if (entity == null) throw new KeyNotFoundException($"Seminar with ID {request.Id} not found.");
            return _mapper.Map<SeminarWithDetailsDto>(entity);
        }
    }
}
