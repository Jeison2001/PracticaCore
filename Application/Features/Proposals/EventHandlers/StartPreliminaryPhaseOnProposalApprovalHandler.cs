using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Proposals.EventHandlers
{
    /// <summary>
    /// Reemplaza: trg_update_proposal_to_preliminary_phase
    /// Cuando el estado de una Propuesta cambia a PERTINENTE, avanza la InscriptionModality
    /// a la fase de Anteproyecto y crea el registro PreliminaryProject.
    /// </summary>
    public class StartPreliminaryPhaseOnProposalApprovalHandler : INotificationHandler<ProposalStateChangedEvent>
    {
        private static readonly string[] InitialPermissions =
        [
            PermissionCodes.ProyectoGrado.N2PGA,
            PermissionCodes.ProyectoGrado.N3PGRA,
            PermissionCodes.ProyectoGrado.N3PGCA,
        ];

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StartPreliminaryPhaseOnProposalApprovalHandler> _logger;

        public StartPreliminaryPhaseOnProposalApprovalHandler(IUnitOfWork unitOfWork, ILogger<StartPreliminaryPhaseOnProposalApprovalHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ProposalStateChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("StartPreliminaryPhaseOnProposalApprovalHandler: Processing proposal state change. InscriptionModalityId={IMId}, NewStateStageId={NewStateId}, TriggeredBy={UserId}",
                notification.InscriptionModalityId, notification.NewStateStageId, notification.TriggeredByUserId);

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null)
            {
                _logger.LogWarning("StartPreliminaryPhaseOnProposalApprovalHandler: InscriptionModality Id={Id} not found", notification.InscriptionModalityId);
                return;
            }

            _logger.LogInformation("StartPreliminaryPhaseOnProposalApprovalHandler: Found inscription Id={Id}, Modality={ModalityCode}",
                inscription.Id, inscription.IdModality);

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            if (modality?.Code != ModalityCodes.ProyectoGrado) return;

            var newState = await stateStageRepo.GetByIdAsync(notification.NewStateStageId);
            if (newState?.Code != StateStageCodes.PropPertinente) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var preliminaryProjectRepo = _unitOfWork.GetRepository<PreliminaryProject, int>();

            // Fase destino: Anteproyecto
            var targetStageModality = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PgFaseAnteproyecto && x.StatusRegister, cancellationToken);

            if (targetStageModality == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la fase {code} activa.", nameof(StartPreliminaryPhaseOnProposalApprovalHandler), StageModalityCodes.PgFaseAnteproyecto);
                return;
            }

            // Estado inicial del anteproyecto
            var initialStateStage = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.Code == StateStageCodes.ApPendienteDocumento && x.IdStageModality == targetStageModality.Id && x.StatusRegister, cancellationToken);

            if (initialStateStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró el estado {code} activo.", nameof(StartPreliminaryPhaseOnProposalApprovalHandler), StateStageCodes.ApPendienteDocumento);
                return;
            }

            // Crear PreliminaryProject si no existe (idempotencia)
            if (!await preliminaryProjectRepo.AnyAsync(x => x.Id == notification.InscriptionModalityId, cancellationToken))
            {
                await preliminaryProjectRepo.AddAsync(new PreliminaryProject
                {
                    Id = notification.InscriptionModalityId,
                    IdStateStage = initialStateStage.Id,
                    IdUserCreatedAt = notification.TriggeredByUserId,
                    OperationRegister = "Creado automáticamente al aprobar propuesta",
                    StatusRegister = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Avanzar la fase en InscriptionModality
            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTime.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase Anteproyecto asignada por DomainEvent";

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
