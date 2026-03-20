using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.AcademicPractices.EventHandlers
{
    /// <summary>
    /// Reemplaza: trg_assign_practice_phase_permissions + trg_academic_practice_phase_transitions
    /// Gestiona dos responsabilidades al cambiar el estado de AcademicPractice:
    ///   1. Transiciones automáticas de fase en InscriptionModality.
    ///   2. Asignación de permisos progresivos al estudiante.
    /// </summary>
    public class AdvanceAcademicPracticePhaseHandler : INotificationHandler<AcademicPracticeStateChangedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdvanceAcademicPracticePhaseHandler> _logger;

        public AdvanceAcademicPracticePhaseHandler(IUnitOfWork unitOfWork, ILogger<AdvanceAcademicPracticePhaseHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(AcademicPracticeStateChangedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.OldStateStageId == notification.NewStateStageId) return;

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();

            var newState = await stateStageRepo.GetByIdAsync(notification.NewStateStageId);
            if (newState == null) return;

            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            if (modality?.Code != ModalityCodes.PracticaAcademica) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

            switch (newState.Code)
            {
                // Transición: Inscripción Aprobada → Fase Desarrollo + permisos F1
                case StateStageCodes.PaInscripcionAprobada:
                {
                    var devStage = await stageModalityRepo.GetFirstOrDefaultAsync(
                        x => x.Code == StageModalityCodes.PaFaseDesarrollo && x.StatusRegister, cancellationToken);

                    if (devStage == null)
                    {
                        _logger.LogWarning("{Handler}: No se encontró la fase {code}.", nameof(AdvanceAcademicPracticePhaseHandler), StageModalityCodes.PaFaseDesarrollo);
                        break;
                    }

                    inscription.IdStageModality = devStage.Id;
                    inscription.UpdatedAt = DateTime.UtcNow;
                    inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
                    inscription.OperationRegister += " | Fase Desarrollo PA por DomainEvent";
                    await inscriptionModalityRepo.UpdateAsync(inscription);

                    await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                        _unitOfWork, notification.InscriptionModalityId,
                        [PermissionCodes.PracticaAcademica.N2PAF1, PermissionCodes.PracticaAcademica.N3PAF1R, PermissionCodes.PracticaAcademica.N3PAF1C],
                        notification.TriggeredByUserId, cancellationToken);
                    break;
                }

                // Transición: Desarrollo Aprobado → Fase Evaluación + permisos F2
                case StateStageCodes.PaDesarrolloAprobada:
                {
                    var evalStage = await stageModalityRepo.GetFirstOrDefaultAsync(
                        x => x.Code == StageModalityCodes.PaFaseEvaluacion && x.StatusRegister, cancellationToken);

                    if (evalStage == null)
                    {
                        _logger.LogWarning("{Handler}: No se encontró la fase {code}.", nameof(AdvanceAcademicPracticePhaseHandler), StageModalityCodes.PaFaseEvaluacion);
                        break;
                    }

                    inscription.IdStageModality = evalStage.Id;
                    inscription.UpdatedAt = DateTime.UtcNow;
                    inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
                    inscription.OperationRegister += " | Fase Evaluación PA por DomainEvent";
                    await inscriptionModalityRepo.UpdateAsync(inscription);

                    await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                        _unitOfWork, notification.InscriptionModalityId,
                        [PermissionCodes.PracticaAcademica.N2PAF2, PermissionCodes.PracticaAcademica.N3PAF2R, PermissionCodes.PracticaAcademica.N3PAF2C],
                        notification.TriggeredByUserId, cancellationToken);
                    break;
                }
            }
        }
    }
}
