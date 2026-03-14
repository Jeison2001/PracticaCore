using Application.Shared.DTOs.SaberPros;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.SaberPros.Handlers
{
    public class GetSaberProWithDetailsQueryHandler : IRequestHandler<GetSaberProWithDetailsQuery, SaberProWithDetailsDto>
    {
        private readonly ISaberProRepository _repository;
        private readonly IMapper _mapper;

        public GetSaberProWithDetailsQueryHandler(ISaberProRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SaberProWithDetailsDto> Handle(GetSaberProWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetWithDetailsAsync(request.Id);
            if (entity == null) throw new KeyNotFoundException($"SaberPro with ID {request.Id} not found.");
            return _mapper.Map<SaberProWithDetailsDto>(entity);
        }
    }
}
