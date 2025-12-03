using Application.Shared.DTOs.CoTerminals;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.CoTerminals.Handlers
{
    public class GetCoTerminalWithDetailsQueryHandler : IRequestHandler<GetCoTerminalWithDetailsQuery, CoTerminalWithDetailsDto>
    {
        private readonly ICoTerminalRepository _repository;
        private readonly IMapper _mapper;

        public GetCoTerminalWithDetailsQueryHandler(ICoTerminalRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<CoTerminalWithDetailsDto> Handle(GetCoTerminalWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetWithDetailsAsync(request.Id);
            if (entity == null) throw new KeyNotFoundException($"CoTerminal with ID {request.Id} not found.");
            return _mapper.Map<CoTerminalWithDetailsDto>(entity);
        }
    }
}
