using Application.Shared.DTOs.CoTerminals;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.CoTerminals.Handlers
{
    public class GetCoTerminalsByUserHandler : IRequestHandler<GetCoTerminalsByUserQuery, List<CoTerminalWithDetailsDto>>
    {
        private readonly ICoTerminalRepository _repository;
        private readonly IMapper _mapper;

        public GetCoTerminalsByUserHandler(ICoTerminalRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<CoTerminalWithDetailsDto>> Handle(GetCoTerminalsByUserQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserAsync(request.UserId, request.Status, cancellationToken);
            var ordered = result
                .OrderByDescending(x => x.CoTerminal.CreatedAt)
                .ThenByDescending(x => x.CoTerminal.Id)
                .ToList();
            return _mapper.Map<List<CoTerminalWithDetailsDto>>(ordered);
        }
    }
}
