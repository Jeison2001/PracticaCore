using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.Queries.AcademicPractice;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice.Handlers
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

            return _mapper.Map<List<AcademicPracticeWithDetailsResponseDto>>(academicPracticesWithDetails);
        }
    }
}
