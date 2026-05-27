using Domain.Entities;
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
            var eventName = await GetEventNameAsync(newEntity.IdStateStage);
            if (string.IsNullOrEmpty(eventName)) return;
            var data = await _eventDataBuilder.BuildEventDataAsync(newEntity.Id, eventName);
            if (data.Count > 0) _queueService.EnqueueEventNotification(eventName, data);
        }

        public Task HandleCreationAsync(ScientificArticle entity, CancellationToken ct = default) => Task.CompletedTask;

        private async Task<string> GetEventNameAsync(int stateStageId)
        {
            var repo = _unitOfWork.GetRepository<StateStage, int>();
            var state = await repo.GetByIdAsync(stateStageId);
            var code = state?.Code ?? "";
            if (code.Contains("ART_INS_RADICADO")) return "ART_FASE1_SUBMITTED";
            if (code.Contains("ART_INS_APROBADO")) return "ART_FASE1_APPROVED";
            if (code.Contains("ART_INS_OBSERVACIONES")) return "ART_FASE1_OBSERVATIONS";
            if (code.Contains("ART_INS_RECHAZADO")) return "ART_FASE1_REJECTED";
            if (code.Contains("ART_PUB_RADICADO")) return "ART_FASE2_SUBMITTED";
            if (code.Contains("ART_PUB_APROBADO")) return "ART_FASE2_APPROVED";
            if (code.Contains("ART_PUB_OBSERVACIONES")) return "ART_FASE2_OBSERVATIONS";
            if (code.Contains("ART_PUB_RECHAZADO")) return "ART_FASE2_REJECTED";
            return string.Empty;
        }
    }
}
