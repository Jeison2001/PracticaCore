using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Services.Notifications.Handlers;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Handlers
{
    public class ScientificArticleChangeHandler : IEntityChangeHandler<ScientificArticle, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IScientificArticleEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ScientificArticleChangeHandler> _logger;

        public ScientificArticleChangeHandler(IEmailNotificationQueueService queueService, IScientificArticleEventDataBuilder eventDataBuilder, IUnitOfWork unitOfWork, ILogger<ScientificArticleChangeHandler> logger)
        { _queueService = queueService; _eventDataBuilder = eventDataBuilder; _unitOfWork = unitOfWork; _logger = logger; }

        public async Task HandleChangeAsync(ScientificArticle oldEntity, ScientificArticle newEntity, CancellationToken ct = default)
        {
            if (oldEntity.IdStateStage == newEntity.IdStateStage) return;
            var eventName = await GetEventNameAsync(newEntity.IdStateStage, oldEntity.IdStateStage);
            if (string.IsNullOrEmpty(eventName)) return;
            var data = await _eventDataBuilder.BuildEventDataAsync(newEntity.Id, eventName);
            if (data.Count > 0) _queueService.EnqueueEventNotification(eventName, data);
        }

        public Task HandleCreationAsync(ScientificArticle entity, CancellationToken ct = default) => Task.CompletedTask;

        private async Task<string> GetEventNameAsync(int newStateStageId, int oldStateStageId)
        {
            var repo = _unitOfWork.GetRepository<StateStage, int>();
            var newState = await repo.GetByIdAsync(newStateStageId);
            var newCode = (newState?.Code ?? string.Empty).Trim().ToUpperInvariant();

            if (newCode == nameof(StateStageCodeEnum.ART_PUB_PENDIENTE))
            {
                var oldState = oldStateStageId > 0 ? await repo.GetByIdAsync(oldStateStageId) : null;
                var oldCode = (oldState?.Code ?? string.Empty).Trim();
                if (oldCode.StartsWith("ART_INS_", StringComparison.OrdinalIgnoreCase))
                {
                    return "ART_FASE1_APPROVED";
                }

                return string.Empty;
            }

            if (newCode.Contains(nameof(StateStageCodeEnum.ART_INS_RADICADO), StringComparison.Ordinal)) return "ART_FASE1_SUBMITTED";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_INS_APROBADO), StringComparison.Ordinal)) return "ART_FASE1_APPROVED";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_INS_OBSERVACIONES), StringComparison.Ordinal)) return "ART_FASE1_OBSERVATIONS";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_INS_RECHAZADO), StringComparison.Ordinal)) return "ART_FASE1_REJECTED";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_PUB_RADICADO), StringComparison.Ordinal)) return "ART_FASE2_SUBMITTED";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_PUB_APROBADO), StringComparison.Ordinal)) return "ART_FASE2_APPROVED";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_PUB_OBSERVACIONES), StringComparison.Ordinal)) return "ART_FASE2_OBSERVATIONS";
            if (newCode.Contains(nameof(StateStageCodeEnum.ART_PUB_RECHAZADO), StringComparison.Ordinal)) return "ART_FASE2_REJECTED";
            return string.Empty;
        }
    }
}
