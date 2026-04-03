using Application.Features.Shared.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Features.Proposals.EventHandlers
{
    /// <summary>
    /// Cuando se aprueba una inscripción de modalidad Proyecto de Grado, avanza la
    /// InscriptionModality a la fase de propuesta (PG_FASE_PROPUESTA) y asigna
    /// los permisos iniciales de propuesta al estudiante.
    /// </summary>
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
                _logger.LogWarning(
                    "StartProposalPhaseOnApprovalHandler: Stage {StageCode} not found or inactive.",
                    StageModalityCodes.PgFasePropuesta);
                return;
            }

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            inscription.IdStageModality = targetStageModality.Id;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase propuesta asignada por DomainEvent";

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

