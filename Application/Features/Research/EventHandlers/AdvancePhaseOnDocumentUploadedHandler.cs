using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Jobs;
using Application.Common.Services.Jobs;

namespace Application.Features.Research.EventHandlers
{
    /// <summary>
    /// Reemplaza la lógica (o la complementa si estaba en deshuso) de:
    /// - trg_update_preliminary_project_on_document_upload
    /// - trg_update_project_final_on_document_upload
    /// Actualiza el estado de las fases cuando se carga el documento respectivo.
    /// </summary>
    public class AdvancePhaseOnDocumentUploadedHandler : INotificationHandler<DocumentUploadedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdvancePhaseOnDocumentUploadedHandler> _logger;
        private readonly IJobEnqueuer _jobEnqueuer;

        public AdvancePhaseOnDocumentUploadedHandler(IUnitOfWork unitOfWork, ILogger<AdvancePhaseOnDocumentUploadedHandler> logger, IJobEnqueuer jobEnqueuer)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _jobEnqueuer = jobEnqueuer;
        }

        public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
        {
            var documentTypeRepo = _unitOfWork.GetRepository<DocumentType, int>();
            var documentType = await documentTypeRepo.GetByIdAsync(notification.DocumentTypeId);

            if (documentType == null) return;

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();

            switch (documentType.Code)
            {
                case DocumentTypeCodes.AnteproyectoEntregable:
                    await ProcessPreliminaryProjectUploadAsync(notification, stateStageRepo, cancellationToken);
                    break;
                case DocumentTypeCodes.ProyectoFinalEntregable:
                    await ProcessProjectFinalUploadAsync(notification, stateStageRepo, cancellationToken);
                    break;
            }
        }

        private async Task ProcessPreliminaryProjectUploadAsync(DocumentUploadedEvent notification, IRepository<StateStage, int> stateStageRepo, CancellationToken cancellationToken)
        {
            var preliminaryProjectRepo = _unitOfWork.GetRepository<PreliminaryProject, int>();
            var preliminaryProject = await preliminaryProjectRepo.GetByIdAsync(notification.InscriptionModalityId);

            if (preliminaryProject == null) return;

            var currentState = await stateStageRepo.GetByIdAsync(preliminaryProject.IdStateStage);
            if (currentState?.Code != StateStageCodes.ApPendienteDocumento) return;

            var radicadoState = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.Code == StateStageCodes.ApRadicadoPendAsigEval && x.StatusRegister, cancellationToken);

            if (radicadoState != null)
            {
                preliminaryProject.IdStateStage = radicadoState.Id;
                preliminaryProject.UpdatedAt = DateTimeOffset.UtcNow;
                preliminaryProject.IdUserUpdatedAt = notification.TriggeredByUserId;
                preliminaryProject.OperationRegister += " | Radicado automáticamente al cargar documento";

                await preliminaryProjectRepo.UpdatePartialAsync(preliminaryProject, [
                    x => x.IdStateStage,
                    x => x.UpdatedAt,
                    x => x.IdUserUpdatedAt,
                    x => x.OperationRegister
                ]);

                _logger.LogInformation("Encolando notificación de cambio de estado para Anteproyecto ID {PreliminaryProjectId}", preliminaryProject.Id);
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                    x => x.HandlePreliminaryProjectChangeAsync(preliminaryProject.Id, currentState.Id));
            }
        }

        private async Task ProcessProjectFinalUploadAsync(DocumentUploadedEvent notification, IRepository<StateStage, int> stateStageRepo, CancellationToken cancellationToken)
        {
            var projectFinalRepo = _unitOfWork.GetRepository<ProjectFinal, int>();
            var projectFinal = await projectFinalRepo.GetByIdAsync(notification.InscriptionModalityId);

            if (projectFinal == null) return;

            var currentState = await stateStageRepo.GetByIdAsync(projectFinal.IdStateStage);
            if (currentState?.Code != StateStageCodes.PfinfPendienteInforme) return;

            var radicadoState = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.Code == StateStageCodes.PfinfRadicadoEnEvaluacion && x.StatusRegister, cancellationToken);

            if (radicadoState != null)
            {
                projectFinal.IdStateStage = radicadoState.Id;
                projectFinal.UpdatedAt = DateTimeOffset.UtcNow;
                projectFinal.IdUserUpdatedAt = notification.TriggeredByUserId;
                projectFinal.OperationRegister += " | Radicado automáticamente al cargar documento";

                await projectFinalRepo.UpdatePartialAsync(projectFinal, [
                    x => x.IdStateStage,
                    x => x.UpdatedAt,
                    x => x.IdUserUpdatedAt,
                    x => x.OperationRegister
                ]);

                _logger.LogInformation("Encolando notificación de cambio de estado para Proyecto Final ID {ProjectFinalId}", projectFinal.Id);
                _jobEnqueuer.Enqueue<INotificationBackgroundJob>(
                    x => x.HandleProjectFinalChangeAsync(projectFinal.Id, currentState.Id));
            }
        }
    }
}

