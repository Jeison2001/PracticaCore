using Application.Shared.DTOs.StudyPlans;
using MediatR;

namespace Application.Shared.Queries.StudyPlans.GetStudyPlansByAcademicProgram
{
    public record GetStudyPlansByAcademicProgramQuery(int AcademicProgramId) : IRequest<List<StudyPlanDto>>;
}
