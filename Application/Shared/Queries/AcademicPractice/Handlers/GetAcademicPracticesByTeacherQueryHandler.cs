using Application.Shared.DTOs.AcademicPractice;
using Application.Shared.Queries.AcademicPractice;
using AutoMapper;
using Domain.Common;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice.Handlers
{
    public class GetAcademicPracticesByTeacherQueryHandler : IRequestHandler<GetAcademicPracticesByTeacherQuery, PaginatedResult<AcademicPracticeWithDetailsResponseDto>>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IMapper _mapper;

        public GetAcademicPracticesByTeacherQueryHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IMapper mapper)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<AcademicPracticeWithDetailsResponseDto>> Handle(GetAcademicPracticesByTeacherQuery request, CancellationToken cancellationToken)
        {
            var result = await _academicPracticeRepository.GetAcademicPracticesByTeacherWithDetailsPaginatedAsync(
                request.TeacherId,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.IsDescending,
                request.Filters,
                cancellationToken);

            var mappedItems = _mapper.Map<List<AcademicPracticeWithDetailsResponseDto>>(result.Items);

            return new PaginatedResult<AcademicPracticeWithDetailsResponseDto>
            {
                Items = mappedItems,
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
    }
}
