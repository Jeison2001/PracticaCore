using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Handlers;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Handlers
{
    public class SeminarChangeHandler : IEntityChangeHandler<Seminar, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly ISeminarEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SeminarChangeHandler> _logger;

        public SeminarChangeHandler(IEmailNotificationQueueService queueService, ISeminarEventDataBuilder eventDataBuilder, IUnitOfWork unitOfWork, ILogger<SeminarChangeHandler> logger)
        { _queueService = queueService; _eventDataBuilder = eventDataBuilder; _unitOfWork = unitOfWork; _logger = logger; }

        public async Task HandleChangeAsync(Seminar oldEntity, Seminar newEntity, CancellationToken ct = default)
        {
            if (oldEntity.IdStateStage == newEntity.IdStateStage) return;
            var eventName = await GetEventNameAsync(newEntity.IdStateStage);
            if (string.IsNullOrEmpty(eventName)) return;
            var data = await _eventDataBuilder.BuildEventDataAsync(newEntity.Id, eventName);
            if (data.Count > 0) _queueService.EnqueueEventNotification(eventName, data);
        }

        public Task HandleCreationAsync(Seminar entity, CancellationToken ct = default) => Task.CompletedTask;

        private async Task<string> GetEventNameAsync(int stateStageId)
        {
            var repo = _unitOfWork.GetRepository<StateStage, int>();
            var state = await repo.GetByIdAsync(stateStageId);
            var code = state?.Code ?? "";
            if (code.Contains("RADICADO")) return "SEM_SUBMITTED";
            if (code.Contains("APROBADO")) return "SEM_APPROVED";
            if (code.Contains("OBSERVACIONES")) return "SEM_OBSERVATIONS";
            if (code.Contains("RECHAZADO")) return "SEM_REJECTED";
            return string.Empty;
        }
    }
}
