using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Handlers;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Handlers
{
    public class SaberProChangeHandler : IEntityChangeHandler<SaberPro, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly ISaberProEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaberProChangeHandler> _logger;

        public SaberProChangeHandler(IEmailNotificationQueueService queueService, ISaberProEventDataBuilder eventDataBuilder, IUnitOfWork unitOfWork, ILogger<SaberProChangeHandler> logger)
        { _queueService = queueService; _eventDataBuilder = eventDataBuilder; _unitOfWork = unitOfWork; _logger = logger; }

        public async Task HandleChangeAsync(SaberPro oldEntity, SaberPro newEntity, CancellationToken ct = default)
        {
            if (oldEntity.IdStateStage == newEntity.IdStateStage) return;
            var eventName = await GetEventNameAsync(newEntity.IdStateStage);
            if (string.IsNullOrEmpty(eventName)) return;
            var data = await _eventDataBuilder.BuildEventDataAsync(newEntity.Id, eventName);
            if (data.Count > 0) _queueService.EnqueueEventNotification(eventName, data);
        }

        public Task HandleCreationAsync(SaberPro entity, CancellationToken ct = default) => Task.CompletedTask;

        private async Task<string> GetEventNameAsync(int stateStageId)
        {
            var repo = _unitOfWork.GetRepository<StateStage, int>();
            var state = await repo.GetByIdAsync(stateStageId);
            var code = state?.Code ?? "";
            if (code.Contains("RADICADO")) return "SP_SUBMITTED";
            if (code.Contains("APROBADO")) return "SP_APPROVED";
            if (code.Contains("OBSERVACIONES")) return "SP_OBSERVATIONS";
            if (code.Contains("RECHAZADO")) return "SP_REJECTED";
            return string.Empty;
        }
    }
}
