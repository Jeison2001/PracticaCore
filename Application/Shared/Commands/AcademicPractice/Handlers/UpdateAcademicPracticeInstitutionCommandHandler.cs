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
            UpdateInstitutionFields(academicPractice, dto);
            
            // Update tracking fields
            UpdateTrackingFields(academicPractice, dto);
            
            // Ensure all dates in entity are UTC
            NormalizeAllDatesToUtc(academicPractice);

            await _academicPracticeRepository.UpdateAsync(academicPractice);
            await _unitOfWork.CommitAsync(cancellationToken);

            return true;
        }

        private static void UpdateInstitutionFields(Domain.Entities.AcademicPractice academicPractice, UpdateInstitutionInfoDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Title))
                academicPractice.Title = dto.Title;
                
            if (!string.IsNullOrEmpty(dto.InstitutionName))
                academicPractice.InstitutionName = dto.InstitutionName;
                
            if (!string.IsNullOrEmpty(dto.InstitutionContact))
                academicPractice.InstitutionContact = dto.InstitutionContact;
                
            if (dto.PracticeStartDate.HasValue)
                academicPractice.PracticeStartDate = EnsureUtc(dto.PracticeStartDate.Value);

            if (dto.PracticeEndDate.HasValue)
                academicPractice.PracticeEndDate = EnsureUtc(dto.PracticeEndDate.Value);

            if (dto.PracticeHours.HasValue)
                academicPractice.PracticeHours = dto.PracticeHours;
                
            academicPractice.IsEmprendimiento = dto.IsEmprendimiento;
                
            if (!string.IsNullOrEmpty(dto.Observations))
                academicPractice.Observations = dto.Observations;
        }

        private static void UpdateTrackingFields(Domain.Entities.AcademicPractice academicPractice, UpdateInstitutionInfoDto dto)
        {
            academicPractice.UpdatedAt = DateTime.UtcNow;
            academicPractice.IdUserUpdatedAt = dto.IdUserUpdatedAt;
            
            if (dto.CreatedAt != default)
                academicPractice.CreatedAt = EnsureUtc(dto.CreatedAt);
        }

        private static void NormalizeAllDatesToUtc(Domain.Entities.AcademicPractice academicPractice)
        {
            // Normalize base tracking dates
            academicPractice.CreatedAt = EnsureUtc(academicPractice.CreatedAt);
            
            // Normalize optional dates specific to AcademicPractice
            academicPractice.PracticeStartDate = EnsureUtcIfHasValue(academicPractice.PracticeStartDate);
            academicPractice.PracticeEndDate = EnsureUtcIfHasValue(academicPractice.PracticeEndDate);
            academicPractice.AvalApprovalDate = EnsureUtcIfHasValue(academicPractice.AvalApprovalDate);
            academicPractice.PlanApprovalDate = EnsureUtcIfHasValue(academicPractice.PlanApprovalDate);
            academicPractice.DevelopmentCompletionDate = EnsureUtcIfHasValue(academicPractice.DevelopmentCompletionDate);
            academicPractice.FinalReportApprovalDate = EnsureUtcIfHasValue(academicPractice.FinalReportApprovalDate);
            academicPractice.FinalApprovalDate = EnsureUtcIfHasValue(academicPractice.FinalApprovalDate);
        }

        private static DateTime EnsureUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Utc 
                ? dateTime 
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        private static DateTime? EnsureUtcIfHasValue(DateTime? dateTime)
        {
            return dateTime.HasValue ? EnsureUtc(dateTime.Value) : null;
        }
    }
}
