using Domain.Entities;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications
{
    /// <summary>
    /// Handler específico para cambios en TeachingAssignment.
    /// Single Responsibility: Solo maneja notificaciones de asignaciones docentes.
    /// </summary>
    public class TeachingAssignmentChangeHandler : IEntityChangeHandler<TeachingAssignment, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly ITeachingAssignmentEventDataBuilder _eventDataBuilder;
        private readonly ILogger<TeachingAssignmentChangeHandler> _logger;

        public TeachingAssignmentChangeHandler(
            IEmailNotificationQueueService queueService,
            ITeachingAssignmentEventDataBuilder eventDataBuilder,
            ILogger<TeachingAssignmentChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _logger = logger;
        }

        public async Task HandleChangeAsync(TeachingAssignment oldEntity, TeachingAssignment newEntity, CancellationToken cancellationToken = default)
        {
            // Para cambios, solo notificamos si cambió el docente o el tipo de asignación
            if (oldEntity.IdTeacher != newEntity.IdTeacher || 
                oldEntity.IdTypeTeachingAssignment != newEntity.IdTypeTeachingAssignment)
            {
                var eventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(newEntity.Id, "TEACHING_ASSIGNMENT_ASSIGNED");
                var jobId = _queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", eventData);
                
                _logger.LogInformation("Asignación docente actualizada - Evento TEACHING_ASSIGNMENT_ASSIGNED encolado para ID: {AssignmentId}, JobId: {JobId}", 
                    newEntity.Id, jobId);
            }
        }

        public async Task HandleCreationAsync(TeachingAssignment entity, CancellationToken cancellationToken = default)
        {
            // Para nuevas asignaciones, siempre notificamos
            var eventData = await _eventDataBuilder.BuildTeachingAssignmentEventDataAsync(entity.Id, "TEACHING_ASSIGNMENT_ASSIGNED");
            var jobId = _queueService.EnqueueEventNotification("TEACHING_ASSIGNMENT_ASSIGNED", eventData);
            
            _logger.LogInformation("Nueva asignación docente - Evento TEACHING_ASSIGNMENT_ASSIGNED encolado para ID: {AssignmentId}, JobId: {JobId}", 
                entity.Id, jobId);
        }
    }
}
