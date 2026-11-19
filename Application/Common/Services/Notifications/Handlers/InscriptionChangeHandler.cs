using Domain.Entities;
using Domain.Interfaces.Services.Notifications.Handlers;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Repositories;

namespace Application.Common.Services.Notifications.Handlers
{
    /// <summary>
    /// Handler específico para cambios en InscriptionModality.
    /// Single Responsibility: Solo maneja notificaciones de InscriptionModality.
    /// </summary>
    public class InscriptionChangeHandler : IEntityChangeHandler<InscriptionModality, int>
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IInscriptionEventDataBuilder _eventDataBuilder;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InscriptionChangeHandler> _logger;

        public InscriptionChangeHandler(
            IEmailNotificationQueueService queueService,
            IInscriptionEventDataBuilder eventDataBuilder,
            IUnitOfWork unitOfWork,
            ILogger<InscriptionChangeHandler> logger)
        {
            _queueService = queueService;
            _eventDataBuilder = eventDataBuilder;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task HandleChangeAsync(InscriptionModality oldEntity, InscriptionModality newEntity, CancellationToken cancellationToken = default)
        {
            // Solo procesar si cambió el estado
            if (oldEntity.IdStateInscription != newEntity.IdStateInscription)
            {
                var eventName = await GetInscriptionEventNameAsync(newEntity.IdStateInscription, cancellationToken);
                if (!string.IsNullOrEmpty(eventName))
                {
                    var eventData = await _eventDataBuilder.BuildInscriptionEventDataAsync(newEntity.Id, eventName);
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    
                    _logger.LogInformation("Evento {EventName} encolado para inscripción ID: {InscriptionId}, JobId: {JobId}", 
                        eventName, newEntity.Id, jobId);
                }
            }
        }

        public async Task HandleCreationAsync(InscriptionModality entity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Iniciando procesamiento de notificaciones para nueva inscripción ID: {InscriptionId}", entity.Id);

            try
            {
                // Obtener estudiantes vinculados a la nueva inscripción
                var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
                var userInscriptions = (await userInscriptionRepo.GetAllAsync(ui => ui.IdInscriptionModality == entity.Id && ui.StatusRegister)).ToList();
                var studentIds = userInscriptions.Select(ui => ui.IdUser).ToList();

                var eventData = await _eventDataBuilder.BuildBasicInscriptionDataAsync(
                    entity.Id,
                    entity.IdModality,
                    entity.IdAcademicPeriod,
                    studentIds);

                var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);

                _logger.LogInformation("Inscription creation notification enqueued - Inscription ID: {InscriptionId}, JobId: {JobId}", entity.Id, jobId);

                var pendingCoStudents = userInscriptions
                    .Where(ui => ui.StatusRegister && !ui.IsConfirmed)
                    .ToList();

                if (pendingCoStudents.Count > 0)
                {
                    var firstConfirmedId = userInscriptions.FirstOrDefault(ui => ui.IsConfirmed)?.IdUser;
                    var creatorUserId = entity.IdUserCreatedAt is > 0 ? entity.IdUserCreatedAt.Value : firstConfirmedId;

                    var userIdsToLoad = pendingCoStudents.Select(ui => ui.IdUser).ToList();
                    if (creatorUserId.HasValue)
                        userIdsToLoad.Add(creatorUserId.Value);
                    if (firstConfirmedId.HasValue)
                        userIdsToLoad.Add(firstConfirmedId.Value);

                    var distinctUserIds = userIdsToLoad.Distinct().ToList();
                    var usersById = (await _unitOfWork.GetRepository<User, int>()
                            .GetAllAsync(u => distinctUserIds.Contains(u.Id)))
                        .ToDictionary(u => u.Id);

                    User? creator = null;
                    if (creatorUserId.HasValue)
                        usersById.TryGetValue(creatorUserId.Value, out creator);
                    if (creator == null && firstConfirmedId.HasValue)
                        usersById.TryGetValue(firstConfirmedId.Value, out creator);

                    var modality = await _unitOfWork.GetRepository<Modality, int>().GetByIdAsync(entity.IdModality);
                    var academicPeriod = await _unitOfWork.GetRepository<AcademicPeriod, int>().GetByIdAsync(entity.IdAcademicPeriod);

                    var creatorName = creator != null ? $"{creator.FirstName} {creator.LastName}".Trim() : string.Empty;
                    var creatorEmail = creator?.Email ?? string.Empty;
                    var modalityName = modality?.Name ?? string.Empty;
                    var periodCode = academicPeriod?.Code ?? string.Empty;
                    var inscriptionDate = entity.CreatedAt.ToString("dd/MM/yyyy");

                    foreach (var pending in pendingCoStudents)
                    {
                        if (!usersById.TryGetValue(pending.IdUser, out var invitedUser) || string.IsNullOrWhiteSpace(invitedUser.Email))
                        {
                            _logger.LogWarning(
                                "No se encontró usuario/correo para co-estudiante invitado UserId={UserId} en InscriptionId={InscriptionId}",
                                pending.IdUser, entity.Id);
                            continue;
                        }

                        var invitedEventData = new Dictionary<string, object>
                        {
                            ["InscriptionId"] = entity.Id,
                            ["ModalityName"] = modalityName,
                            ["AcademicPeriod"] = periodCode,
                            ["AcademicPeriodCode"] = periodCode,
                            ["InscriptionDate"] = inscriptionDate,
                            ["CreatorName"] = creatorName,
                            ["CreatorEmail"] = creatorEmail,
                            ["InvitedStudentName"] = $"{invitedUser.FirstName} {invitedUser.LastName}".Trim(),
                            ["InvitedStudentEmail"] = invitedUser.Email
                        };

                        var inviteJobId = _queueService.EnqueueEventNotification("INSCRIPTION_COSTUDENT_INVITED", invitedEventData);
                        _logger.LogInformation(
                            "Notificación INSCRIPTION_COSTUDENT_INVITED encolada - Inscription ID: {InscriptionId}, UserId: {UserId}, JobId: {JobId}",
                            entity.Id, invitedUser.Id, inviteJobId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inscription creation notification for ID {Id}", entity.Id);
                throw;
            }
        }

        private async Task<string> GetInscriptionEventNameAsync(int stateId, CancellationToken cancellationToken)
        {
            try
            {
                var stateRepo = _unitOfWork.GetRepository<StateInscription, int>();
                var state = await stateRepo.GetByIdAsync(stateId);
                
                return state?.Code?.ToUpper() switch
                {
                    "APROBADO" => "INSCRIPTION_APPROVED",
                    "RECHAZADO" => "INSCRIPTION_REJECTED",
                    "PENDIENTE" => "INSCRIPTION_CREATED",
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo nombre de evento para estado ID: {StateId}", stateId);
                return string.Empty;
            }
        }
    }
}
