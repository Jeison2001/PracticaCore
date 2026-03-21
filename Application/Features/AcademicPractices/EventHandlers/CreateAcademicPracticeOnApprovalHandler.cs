using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.AcademicPractices.EventHandlers
{
    public class CreateAcademicPracticeOnApprovalHandler : INotificationHandler<InscriptionStateChangedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateAcademicPracticeOnApprovalHandler> _logger;

        public CreateAcademicPracticeOnApprovalHandler(IUnitOfWork unitOfWork, ILogger<CreateAcademicPracticeOnApprovalHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(InscriptionStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

            var modality = await modalityRepo.GetByIdAsync(notification.ModalityId);
            if (modality?.Code != ModalityCodes.PracticaAcademica) return;

            var stateInscription = await stateInscriptionRepo.GetByIdAsync(notification.NewStateInscriptionId);
            if (stateInscription?.Code != StateInscriptionCodes.Aprobado) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var academicPracticeRepo = _unitOfWork.GetRepository<AcademicPractice, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            var targetStageModality = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PaFaseInscripcion && x.StatusRegister, cancellationToken);

            if (targetStageModality == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la fase {code} activa.", nameof(CreateAcademicPracticeOnApprovalHandler), StageModalityCodes.PaFaseInscripcion);
                return;
            }

            var targetStateStage = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.Code == StateStageCodes.PaInscripcionPendDoc && x.IdStageModality == targetStageModality.Id && x.StatusRegister, cancellationToken);

            if (targetStateStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró el estado {code} activo.", nameof(CreateAcademicPracticeOnApprovalHandler), StateStageCodes.PaInscripcionPendDoc);
                return;
            }

            // Idempotencia: no crear si ya existe
            if (await academicPracticeRepo.AnyAsync(x => x.Id == notification.InscriptionModalityId, cancellationToken)) return;

            await academicPracticeRepo.AddAsync(new AcademicPractice
            {
                Id = notification.InscriptionModalityId,
                IdStateStage = targetStateStage.Id,
                Title = "",
                IsEmprendimiento = false,
                IdUserCreatedAt = notification.TriggeredByUserId,
                OperationRegister = "Creada automáticamente al aprobar inscripción",
                StatusRegister = true,
                CreatedAt = DateTime.UtcNow
            });

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTime.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase asignada por DomainEvent";

            await inscriptionModalityRepo.UpdateAsync(inscription);

            // Asignar permisos iniciales de Phase 0 (F0)
            await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                _unitOfWork,
                notification.InscriptionModalityId,
                [
                    PermissionCodes.PracticaAcademica.N1PA,
                    PermissionCodes.PracticaAcademica.N2PAF0,
                    PermissionCodes.PracticaAcademica.N3PAF0R,
                    PermissionCodes.PracticaAcademica.N3PAF0C
                ],
                notification.TriggeredByUserId,
                cancellationToken);
        }
    }
}
