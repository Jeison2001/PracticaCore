using Application.Shared.DTOs.AcademicAverages;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages.Handlers
{
    public class GetAcademicAveragesByUserHandler : IRequestHandler<GetAcademicAveragesByUserQuery, List<AcademicAverageWithDetailsDto>>
    {
        private readonly IAcademicAverageRepository _repository;
        private readonly IMapper _mapper;

        public GetAcademicAveragesByUserHandler(IAcademicAverageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<AcademicAverageWithDetailsDto>> Handle(GetAcademicAveragesByUserQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetByUserAsync(request.UserId, request.Status, cancellationToken);
            return _mapper.Map<List<AcademicAverageWithDetailsDto>>(result);
        }
    }
}
