using Application.Common.Helpers;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Handlers;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services.Notifications.Handlers
{
    /// <summary>
    /// Handler para eventos de ciclo de vida del usuario (creación / cambios).
    /// Encola notificación institucional cuando un nuevo usuario es registrado en la plataforma.
    /// </summary>
    public class UserChangeHandler : IEntityChangeHandler<User, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserChangeHandler> _logger;
        private readonly IConfiguration? _configuration;

        public UserChangeHandler(
            IEmailNotificationQueueService queueService,
            IUnitOfWork unitOfWork,
            ILogger<UserChangeHandler> logger,
            IConfiguration? configuration = null)
        {
            _queueService = queueService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        public Task HandleChangeAsync(User oldEntity, User newEntity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task HandleCreationAsync(User entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Iniciando procesamiento de notificaciones para nuevo usuario ID: {UserId}", entity.Id);

            try
            {
                var academicProgramRepo = _unitOfWork.GetRepository<AcademicProgram, int>();
                var program = entity.IdAcademicProgram > 0
                    ? await academicProgramRepo.GetByIdAsync(entity.IdAcademicProgram)
                    : null;

                var createdAt = entity.CreatedAt != default ? entity.CreatedAt : DateTimeOffset.UtcNow;
                var appUrl = NotificationFormattingHelper.ResolveAppBaseUrl(_configuration);
                var loginUrl = NotificationFormattingHelper.ResolveLoginUrl(_configuration);

                var eventData = new Dictionary<string, object>
                {
                    { "UserId", entity.Id },
                    { "UserEmail", entity.Email },
                    { "FullName", $"{entity.FirstName} {entity.LastName}".Trim() },
                    { "FirstName", entity.FirstName },
                    { "LastName", entity.LastName },
                    { "Identification", entity.Identification ?? "" },
                    { "AcademicProgram", program?.Name ?? "No asignado" },
                    { "PhoneNumber", entity.PhoneNumber ?? "" },
                    { "CreatedDate", NotificationFormattingHelper.ToColombiaDateTime(createdAt) },
                    { "AppUrl", appUrl },
                    { "LoginUrl", loginUrl },
                    { "TopAccentColor", NotificationFormattingHelper.GetTopAccentColor("approved") }
                };

                var jobId = _queueService.EnqueueEventNotification("USER_CREATED", eventData);

                _logger.LogInformation("Notificación USER_CREATED encolada para usuario ID: {UserId}, JobId: {JobId}", entity.Id, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando notificación de creación para usuario ID {Id}", entity.Id);
                throw;
            }
        }
    }
}
