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
    /// Cuando el estado de un ProjectFinal alcanza PFINF_INFORME_APROBADO:
    ///   1. Estampa la fecha de aprobación del informe (ReportApprovalDate).
    ///   2. Avanza la InscriptionModality a la fase de Sustentación (PG_FASE_SUSTENTACION),
    ///      cerrando el avance automático del flujo de Proyecto de Grado.
    /// No asigna permisos: el catálogo institucional no define códigos de permiso
    /// para la fase de Sustentación.
    /// </summary>
    public class AdvanceProjectFinalToSustentacionHandler : INotificationHandler<ProjectFinalStateChangedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdvanceProjectFinalToSustentacionHandler> _logger;

        public AdvanceProjectFinalToSustentacionHandler(IUnitOfWork unitOfWork, ILogger<AdvanceProjectFinalToSustentacionHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ProjectFinalStateChangedEvent notification, CancellationToken cancellationToken)
        {
            if (notification.OldStateStageId == notification.NewStateStageId) return;

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();
            var newState = await stateStageRepo.GetByIdAsync(notification.NewStateStageId);
            if (newState?.Code != StateStageCodes.PfinfInformeAprobado) return;

            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();
            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            if (modality?.Code != ModalityCodes.ProyectoGrado) return;

            var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();

            // =========================================================================
            // 1. ESTAMPADO DE FECHA AUTOMÁTICA (idempotente)
            // =========================================================================
            var projectFinal = await projectFinalRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (projectFinal != null && projectFinal.ReportApprovalDate == null)
            {
                projectFinal.ReportApprovalDate = DateTimeOffset.UtcNow;
                _logger.LogInformation("{Handler}: Fecha ReportApproval establecida para ProjectFinal ID {Id}", nameof(AdvanceProjectFinalToSustentacionHandler), projectFinal.Id);
                await projectFinalRepo.UpdatePartialAsync(projectFinal, [x => x.ReportApprovalDate]);
            }

            // =========================================================================
            // 2. TRANSICIÓN DE FASE: Proyecto/Informe Final → Sustentación
            // =========================================================================
            var sustStage = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.Code == StageModalityCodes.PgFaseSustentacion && x.StatusRegister, cancellationToken);

            if (sustStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la fase {code} activa.", nameof(AdvanceProjectFinalToSustentacionHandler), StageModalityCodes.PgFaseSustentacion);
                return;
            }

            if (inscription.IdStageModality == sustStage.Id)
            {
                _logger.LogInformation("{Handler}: La inscripción {Id} ya está en Fase Sustentación; no se repite el avance.", nameof(AdvanceProjectFinalToSustentacionHandler), inscription.Id);
                return;
            }

            inscription.IdStageModality = sustStage.Id;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase Sustentación asignada por DomainEvent";
            await inscriptionModalityRepo.UpdatePartialAsync(inscription, [
                x => x.IdStageModality,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt,
                x => x.OperationRegister
            ]);

            _logger.LogInformation("{Handler}: Inscripción {Id} avanzada a Fase Sustentación por aprobación del informe final.", nameof(AdvanceProjectFinalToSustentacionHandler), inscription.Id);
        }
    }
}
