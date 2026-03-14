using Application.Shared.DTOs.SaberPros;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.SaberPros.Handlers
{
    public class GetSaberProsByUserHandler : IRequestHandler<GetSaberProsByUserQuery, List<SaberProWithDetailsDto>>
    {
        private readonly ISaberProRepository _repository;
        private readonly IMapper _mapper;

        public GetSaberProsByUserHandler(ISaberProRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<SaberProWithDetailsDto>> Handle(GetSaberProsByUserQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserAsync(request.UserId, request.Status, cancellationToken);
            return _mapper.Map<List<SaberProWithDetailsDto>>(result);
        }
    }
}
