using Application.Shared.DTOs.StudyPlans;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.StudyPlans.GetStudyPlansByAcademicProgram
{
    public class GetStudyPlansByAcademicProgramHandler : IRequestHandler<GetStudyPlansByAcademicProgramQuery, List<StudyPlanDto>>
    {
        private readonly IRepository<Domain.Entities.StudyPlan, int> _repository;
        private readonly IMapper _mapper;

        public GetStudyPlansByAcademicProgramHandler(IRepository<Domain.Entities.StudyPlan, int> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<StudyPlanDto>> Handle(GetStudyPlansByAcademicProgramQuery request, CancellationToken cancellationToken)
        {
            var studyPlans = await _repository.GetAllAsync(
                filter: sp => sp.IdAcademicProgram == request.AcademicProgramId && sp.StatusRegister
            );

            return _mapper.Map<List<StudyPlanDto>>(studyPlans);
        }
    }
}
