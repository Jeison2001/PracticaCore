using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Research.EventHandlers
{
    /// <summary>
    /// Cuando el estado de un Anteproyecto cambia a AP_APROBADO, avanza la InscriptionModality
    /// a la fase de Proyecto Final y crea el registro ProjectFinal.
    /// </summary>
    public class StartProjectPhaseOnPreliminaryApprovalHandler : INotificationHandler<PreliminaryProjectStateChangedEvent>
    {
        private static readonly string[] InitialPermissions =
        [
            PermissionCodes.ProyectoGrado.N2PGPR,
            PermissionCodes.ProyectoGrado.N3PGRPR,
            PermissionCodes.ProyectoGrado.N3PGCPR,
        ];

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StartProjectPhaseOnPreliminaryApprovalHandler> _logger;

        public StartProjectPhaseOnPreliminaryApprovalHandler(IUnitOfWork unitOfWork, ILogger<StartProjectPhaseOnPreliminaryApprovalHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(PreliminaryProjectStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            if (modality?.Code != ModalityCodes.ProyectoGrado) return;

            var newState = await stateStageRepo.GetByIdAsync(notification.NewStateStageId);
            if (newState?.Code != StateStageCodes.ApAprobado) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();

            // Fase destino: Proyecto/Informe Final
            var targetStageModality = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PgFaseProyectoInforme && x.StatusRegister, cancellationToken);

            if (targetStageModality == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la fase {code} activa.", nameof(StartProjectPhaseOnPreliminaryApprovalHandler), StageModalityCodes.PgFaseProyectoInforme);
                return;
            }

            // Estado inicial del proyecto final
            var initialStateStage = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.Code == StateStageCodes.PfinfPendienteInforme && x.IdStageModality == targetStageModality.Id && x.StatusRegister, cancellationToken);

            if (initialStateStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró el estado {code} activo.", nameof(StartProjectPhaseOnPreliminaryApprovalHandler), StateStageCodes.PfinfPendienteInforme);
                return;
            }

            // Crear ProjectFinal si no existe (idempotencia)
            if (!await projectFinalRepo.AnyAsync(x => x.Id == notification.InscriptionModalityId, cancellationToken))
            {
                await projectFinalRepo.AddAsync(new ProjectFinal
                {
                    Id = notification.InscriptionModalityId,
                    IdStateStage = initialStateStage.Id,
                    IdUserCreatedAt = notification.TriggeredByUserId,
                    OperationRegister = "Creado automáticamente al aprobar anteproyecto",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            // Avanzar la fase en InscriptionModality
            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase Proyecto asignada por DomainEvent";

            await inscriptionModalityRepo.UpdatePartialAsync(inscription, [
                x => x.IdStageModality,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt,
                x => x.OperationRegister
            ]);

            await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                _unitOfWork,
                notification.InscriptionModalityId,
                InitialPermissions,
                notification.TriggeredByUserId,
                cancellationToken);
        }
    }
}

