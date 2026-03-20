using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Proposals.EventHandlers
{
    public class StartProposalPhaseOnApprovalHandler : INotificationHandler<InscriptionStateChangedEvent>
    {
        private static readonly string[] InitialPermissions =
        [
            PermissionCodes.ProyectoGrado.N1PG,
            PermissionCodes.ProyectoGrado.N2PGP,
            PermissionCodes.ProyectoGrado.N3PGRP,
            PermissionCodes.ProyectoGrado.N3PGCP,
        ];

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StartProposalPhaseOnApprovalHandler> _logger;

        public StartProposalPhaseOnApprovalHandler(IUnitOfWork unitOfWork, ILogger<StartProposalPhaseOnApprovalHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(InscriptionStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var stateInscriptionRepo = _unitOfWork.GetRepository<StateInscription, int>();

            var modality = await modalityRepo.GetByIdAsync(notification.ModalityId);
            if (modality?.Code != ModalityCodes.ProyectoGrado) return;

            var stateInscription = await stateInscriptionRepo.GetByIdAsync(notification.NewStateInscriptionId);
            if (stateInscription?.Code != StateInscriptionCodes.Aprobado) return;

            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            var targetStageModality = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PgFasePropuesta && x.StatusRegister, cancellationToken);

            if (targetStageModality == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la fase {code} activa.", nameof(StartProposalPhaseOnApprovalHandler), StageModalityCodes.PgFasePropuesta);
                return;
            }

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTime.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase propuesta asignada por DomainEvent";

            await inscriptionModalityRepo.UpdateAsync(inscription);

            await PermissionAssignmentService.AssignPermissionsToInscriptionUsersAsync(
                _unitOfWork,
                notification.InscriptionModalityId,
                InitialPermissions,
                notification.TriggeredByUserId,
                cancellationToken);
        }
    }
}
