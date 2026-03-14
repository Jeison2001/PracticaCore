using Application.Shared.DTOs.AcademicAverages;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.AcademicAverages.Handlers
{
    public class GetAcademicAverageWithDetailsQueryHandler : IRequestHandler<GetAcademicAverageWithDetailsQuery, AcademicAverageWithDetailsDto>
    {
        private readonly IAcademicAverageRepository _repository;
        private readonly IMapper _mapper;

        public GetAcademicAverageWithDetailsQueryHandler(IAcademicAverageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<AcademicAverageWithDetailsDto> Handle(GetAcademicAverageWithDetailsQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetWithDetailsAsync(request.Id);
            if (entity == null) throw new KeyNotFoundException($"AcademicAverage with ID {request.Id} not found.");
            return _mapper.Map<AcademicAverageWithDetailsDto>(entity);
        }
    }
}
