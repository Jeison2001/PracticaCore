using Application.Shared.Commands.AcademicPractice;
using Application.Shared.DTOs.AcademicPractice;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.AcademicPractice.Handlers
{
    public class UpdateAcademicPracticeInstitutionCommandHandler : IRequestHandler<UpdateAcademicPracticeInstitutionCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAcademicPracticeInstitutionCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(UpdateAcademicPracticeInstitutionCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var academicPractice = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            
            if (academicPractice == null)
                return false;

            // Update institution information
            if (!string.IsNullOrEmpty(dto.InstitutionName))
                academicPractice.InstitutionName = dto.InstitutionName;
                
            if (!string.IsNullOrEmpty(dto.InstitutionContact))
                academicPractice.InstitutionContact = dto.InstitutionContact;
                
            if (dto.PracticeStartDate.HasValue)
                academicPractice.PracticeStartDate = DateTime.SpecifyKind(dto.PracticeStartDate.Value, DateTimeKind.Utc);

            if (dto.PracticeEndDate.HasValue)
                academicPractice.PracticeEndDate = DateTime.SpecifyKind(dto.PracticeEndDate.Value, DateTimeKind.Utc);

            if (dto.PracticeHours.HasValue)
                academicPractice.PracticeHours = dto.PracticeHours;
                
            academicPractice.IsEmprendimiento = dto.IsEmprendimiento;
                
            if (!string.IsNullOrEmpty(dto.Observations))
                academicPractice.Observations = dto.Observations;

            // Asignar siempre UpdatedAt como UTC (no usar el del DTO)
            academicPractice.UpdatedAt = DateTime.UtcNow;
            academicPractice.IdUserUpdatedAt = dto.IdUserUpdatedAt;
            
            // Solo forzar CreatedAt si viene del DTO y no es default
            if (dto.CreatedAt != default)
                academicPractice.CreatedAt = DateTime.SpecifyKind(dto.CreatedAt, DateTimeKind.Utc);
                
            // ASEGURAR que todas las fechas existentes en la entidad sean UTC
            if (academicPractice.CreatedAt.Kind != DateTimeKind.Utc)
                academicPractice.CreatedAt = DateTime.SpecifyKind(academicPractice.CreatedAt, DateTimeKind.Utc);
                
            // Asegurar fechas específicas de AcademicPractice sean UTC
            if (academicPractice.PracticeStartDate.HasValue && academicPractice.PracticeStartDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.PracticeStartDate = DateTime.SpecifyKind(academicPractice.PracticeStartDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.PracticeEndDate.HasValue && academicPractice.PracticeEndDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.PracticeEndDate = DateTime.SpecifyKind(academicPractice.PracticeEndDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.AvalApprovalDate.HasValue && academicPractice.AvalApprovalDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.AvalApprovalDate = DateTime.SpecifyKind(academicPractice.AvalApprovalDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.PlanApprovalDate.HasValue && academicPractice.PlanApprovalDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.PlanApprovalDate = DateTime.SpecifyKind(academicPractice.PlanApprovalDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.DevelopmentCompletionDate.HasValue && academicPractice.DevelopmentCompletionDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.DevelopmentCompletionDate = DateTime.SpecifyKind(academicPractice.DevelopmentCompletionDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.FinalReportApprovalDate.HasValue && academicPractice.FinalReportApprovalDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.FinalReportApprovalDate = DateTime.SpecifyKind(academicPractice.FinalReportApprovalDate.Value, DateTimeKind.Utc);
                
            if (academicPractice.FinalApprovalDate.HasValue && academicPractice.FinalApprovalDate.Value.Kind != DateTimeKind.Utc)
                academicPractice.FinalApprovalDate = DateTime.SpecifyKind(academicPractice.FinalApprovalDate.Value, DateTimeKind.Utc);

            await _academicPracticeRepository.UpdateAsync(academicPractice);
            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
    }
}
