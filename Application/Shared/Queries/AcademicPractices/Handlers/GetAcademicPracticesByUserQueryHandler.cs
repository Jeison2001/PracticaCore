using Application.Shared.DTOs.AcademicPractices;
using AutoMapper;
using MediatR;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Queries.AcademicPractices.Handlers
{
    public class GetAcademicPracticesByUserQueryHandler : IRequestHandler<GetAcademicPracticesByUserQuery, List<AcademicPracticeWithDetailsResponseDto>>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IMapper _mapper;

        public GetAcademicPracticesByUserQueryHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IMapper mapper)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _mapper = mapper;
        }

        public async Task<List<AcademicPracticeWithDetailsResponseDto>> Handle(GetAcademicPracticesByUserQuery request, CancellationToken cancellationToken)
        {
            var academicPracticesWithDetails = await _academicPracticeRepository
                .GetAcademicPracticesByUserWithDetailsAsync(request.UserId, request.Status, cancellationToken);

            var ordered = academicPracticesWithDetails
                .OrderByDescending(x => x.AcademicPractice.CreatedAt)
                .ThenByDescending(x => x.AcademicPractice.Id)
                .ToList();

            return _mapper.Map<List<AcademicPracticeWithDetailsResponseDto>>(ordered);
        }
    }
}