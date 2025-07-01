using Application.Shared.DTOs.AcademicPractice;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Queries.AcademicPractice.Handlers
{
    public class GetSimpleAcademicPracticesQuery : IRequest<List<AcademicPracticeDto>>
    {
    }

    public class GetSimpleAcademicPracticesQueryHandler : IRequestHandler<GetSimpleAcademicPracticesQuery, List<AcademicPracticeDto>>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;

        public GetSimpleAcademicPracticesQueryHandler(IAcademicPracticeRepository academicPracticeRepository)
        {
            _academicPracticeRepository = academicPracticeRepository;
        }

        public async Task<List<AcademicPracticeDto>> Handle(GetSimpleAcademicPracticesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Test simple: obtener solo las entidades básicas sin includes complejos
                var practices = await _academicPracticeRepository.GetAllAsync();
                
                // Mapeo manual simple para evitar problemas de AutoMapper
                var result = practices.Select(p => new AcademicPracticeDto
                {
                    Id = p.Id,
                    IdStateStage = p.IdStateStage,
                    InstitutionName = p.InstitutionName,
                    InstitutionContact = p.InstitutionContact,
                    PracticeStartDate = p.PracticeStartDate,
                    PracticeEndDate = p.PracticeEndDate,
                    IsEmprendimiento = p.IsEmprendimiento,
                    Observations = p.Observations,
                    AvalApprovalDate = p.AvalApprovalDate,
                    PlanApprovalDate = p.PlanApprovalDate,
                    DevelopmentCompletionDate = p.DevelopmentCompletionDate,
                    FinalReportApprovalDate = p.FinalReportApprovalDate,
                    FinalApprovalDate = p.FinalApprovalDate,
                    PracticeHours = p.PracticeHours,
                    EvaluatorObservations = p.EvaluatorObservations,
                    CreatedAt = p.CreatedAt,
                    StatusRegister = p.StatusRegister,
                    IdUserCreatedAt = p.IdUserCreatedAt
                }).ToList();
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en GetSimpleAcademicPracticesQueryHandler: {ex.Message}", ex);
            }
        }
    }
}
