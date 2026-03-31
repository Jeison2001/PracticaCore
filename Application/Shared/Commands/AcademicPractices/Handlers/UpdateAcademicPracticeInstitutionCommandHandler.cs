using Application.Shared.DTOs.AcademicPractices;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.Common.Services.Jobs;

namespace Application.Shared.Commands.AcademicPractices.Handlers
{
    public class UpdateAcademicPracticeInstitutionCommandHandler : IRequestHandler<UpdateAcademicPracticeInstitutionCommand, bool>
    {
        private readonly IAcademicPracticeRepository _academicPracticeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJobEnqueuer _jobEnqueuer;

        public UpdateAcademicPracticeInstitutionCommandHandler(
            IAcademicPracticeRepository academicPracticeRepository,
            IUnitOfWork unitOfWork,
            IJobEnqueuer jobEnqueuer)
        {
            _academicPracticeRepository = academicPracticeRepository;
            _unitOfWork = unitOfWork;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task<bool> Handle(UpdateAcademicPracticeInstitutionCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var academicPractice = await _academicPracticeRepository.GetByIdAsync(dto.Id);
            if (academicPractice == null)
                return false;

            // Snapshot para notificaciones (copia superficial suficiente para comparar IdStateStage)
            var originalStateId = academicPractice.IdStateStage;
            var hasStateChange = dto.NewStateStageId.HasValue
                && dto.NewStateStageId.Value > 0
                && dto.NewStateStageId.Value != originalStateId;

            // Actualizar campos de institución
            UpdateInstitutionFields(academicPractice, dto);

            // Cambiar de estado si se especifica (ej: pasar a PA_PEND_APROBACION_DOCUMENTOS después de radicar docs)
            if (hasStateChange)
            {
                academicPractice.IdStateStage = dto.NewStateStageId.Value;
            }

            // Tracking
            UpdateTrackingFields(academicPractice, dto);

            await _academicPracticeRepository.UpdateAsync(academicPractice);
            await _unitOfWork.CommitAsync(cancellationToken);

            // Encolar job de notificación si hubo cambio de estado (patrón consistente)
            if (hasStateChange)
            {
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(x =>
                    x.HandleAcademicPracticeChangeAsync(academicPractice.Id, originalStateId));
            }

            return true;
        }

        private static void UpdateInstitutionFields(AcademicPractice academicPractice, UpdateInstitutionInfoDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Title))
                academicPractice.Title = dto.Title;
                
            if (!string.IsNullOrEmpty(dto.InstitutionName))
                academicPractice.InstitutionName = dto.InstitutionName;
                
            if (!string.IsNullOrEmpty(dto.InstitutionContact))
                academicPractice.InstitutionContact = dto.InstitutionContact;
                
            if (dto.PracticeStartDate.HasValue)
                academicPractice.PracticeStartDate = dto.PracticeStartDate.Value;

            if (dto.PracticeEndDate.HasValue)
                academicPractice.PracticeEndDate = dto.PracticeEndDate.Value;

            if (dto.PracticeHours.HasValue)
                academicPractice.PracticeHours = dto.PracticeHours;
                
            academicPractice.IsEmprendimiento = dto.IsEmprendimiento;
                
            if (!string.IsNullOrEmpty(dto.Observations))
                academicPractice.Observations = dto.Observations;
        }

        private static void UpdateTrackingFields(AcademicPractice academicPractice, UpdateInstitutionInfoDto dto)
        {
            academicPractice.UpdatedAt = DateTimeOffset.UtcNow;
            academicPractice.IdUserUpdatedAt = dto.IdUserUpdatedAt;
        }

    }
}

