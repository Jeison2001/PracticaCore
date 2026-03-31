using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.MinorModalities.EventHandlers
{
    /// <summary>
    /// Reemplaza: trg_article_phase_transition
    /// Cuando el estado de un ScientificArticle es el último de Fase 1 (IsFinalStateForStage=true)
    /// y no es el final general (IsFinalStateForModalityOverall=false), avanza automáticamente a la Fase 2.
    /// Usa lógica dinámica igual que el trigger original.
    /// </summary>
    public class AdvanceScientificArticlePhaseHandler : INotificationHandler<ScientificArticleStateChangedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdvanceScientificArticlePhaseHandler> _logger;

        public AdvanceScientificArticlePhaseHandler(IUnitOfWork unitOfWork, ILogger<AdvanceScientificArticlePhaseHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ScientificArticleStateChangedEvent notification, CancellationToken cancellationToken)
        {
            // Solo si el estado realmente cambió
            if (notification.OldStateStageId == notification.NewStateStageId) return;

            var stateStageRepo = _unitOfWork.GetRepository<StateStage, int>();

            // El trigger original verifica: IsFinalStateForStage=true AND IsFinalStateForModalityOverall=false
            // Es decir: fin de una fase intermedia → avanzar a la siguiente fase.
            var newState = await stateStageRepo.GetByIdAsync(notification.NewStateStageId);
            if (newState == null || !newState.IsFinalStateForStage || newState.IsFinalStateForModalityOverall) return;

            var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
            var stageModalityRepo = _unitOfWork.GetRepository<StageModality, int>();
            var scientificArticleRepo = _unitOfWork.GetRepository<ScientificArticle, int>();
            var inscriptionModalityRepo = _unitOfWork.GetRepository<InscriptionModality, int>();

            var inscription = await inscriptionModalityRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (inscription == null) return;

            var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
            if (modality?.Code != ModalityCodes.PublicacionArticulo) return;

            // Buscar dinámicamente la Fase 2 (StageOrder = 2) de esta modalidad
            var nextStage = await stageModalityRepo.GetFirstOrDefaultAsync(
                x => x.IdModality == modality.Id && x.StageOrder == 2 && x.StatusRegister, cancellationToken);

            if (nextStage == null)
            {
                _logger.LogWarning("{Handler}: No se encontró la Fase 2 activa para {code}.", nameof(AdvanceScientificArticlePhaseHandler), modality.Code);
                return;
            }

            // Estado inicial de la Fase 2
            var initialState = await stateStageRepo.GetFirstOrDefaultAsync(
                x => x.IdStageModality == nextStage.Id && x.IsInitialState && x.StatusRegister, cancellationToken);

            if (initialState == null)
            {
                _logger.LogWarning("{Handler}: No se encontró estado inicial para Fase 2 de {code}.", nameof(AdvanceScientificArticlePhaseHandler), modality.Code);
                return;
            }

            // Actualizar ScientificArticle con el estado inicial de la Fase 2
            var article = await scientificArticleRepo.GetByIdAsync(notification.InscriptionModalityId);
            if (article == null) return;

            article.IdStateStage = initialState.Id;
            article.UpdatedAt = DateTimeOffset.UtcNow;
            article.IdUserUpdatedAt = notification.TriggeredByUserId;
            article.OperationRegister += " | Auto-Inicio Fase 2";
            await scientificArticleRepo.UpdatePartialAsync(article, [
                x => x.IdStateStage,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt,
                x => x.OperationRegister
            ]);

            // Avanzar la fase en InscriptionModality
            inscription.IdStageModality = nextStage.Id;
            inscription.UpdatedAt = DateTimeOffset.UtcNow;
            inscription.IdUserUpdatedAt = notification.TriggeredByUserId;
            inscription.OperationRegister += " | Fase 2 Artículo activada por DomainEvent";
            await inscriptionModalityRepo.UpdatePartialAsync(inscription, [
                x => x.IdStageModality,
                x => x.UpdatedAt,
                x => x.IdUserUpdatedAt,
                x => x.OperationRegister
            ]);
        }
    }
}

