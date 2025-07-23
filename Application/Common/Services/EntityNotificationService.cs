using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Common.Services
{
    /// <summary>
    /// Servicio simplificado para procesar notificaciones automáticas de cambios en entidades
    /// ✅ Clean Architecture: Inyección directa de dependencias, Hangfire maneja el scope automáticamente
    /// </summary>
    public class EntityNotificationService : IEntityNotificationService
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EntityNotificationService> _logger;

        public EntityNotificationService(
            IEmailNotificationQueueService queueService,
            IUnitOfWork unitOfWork,
            ILogger<EntityNotificationService> logger)
        {
            _queueService = queueService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessInscriptionModalityChangesAsync(object oldEntity, object newEntity, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔔 EntityNotificationService: Iniciando procesamiento de cambios en InscriptionModality");
                
                var oldInscription = oldEntity as InscriptionModality;
                var newInscription = newEntity as InscriptionModality;

                if (oldInscription == null || newInscription == null)
                {
                    _logger.LogWarning("Entidades no son del tipo InscriptionModality - Old: {OldType}, New: {NewType}", 
                        oldEntity?.GetType().Name ?? "null", newEntity?.GetType().Name ?? "null");
                    return;
                }

                _logger.LogInformation("🔄 Comparando estados - Anterior: {OldState}, Nuevo: {NewState}, ID: {Id}", 
                    oldInscription.IdStateInscription, newInscription.IdStateInscription, newInscription.Id);

                // Detectar cambios de estado
                if (oldInscription.IdStateInscription != newInscription.IdStateInscription)
                {
                    _logger.LogInformation("✅ Cambio de estado detectado - procesando notificación...");
                    await ProcessStateChangeAsync(oldInscription, newInscription, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("ℹ️ No hay cambios de estado - no se procesarán notificaciones");
                }

                _logger.LogInformation("✅ Procesamiento de notificaciones completado para InscriptionModality ID: {Id}", newInscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando notificaciones para InscriptionModality");
                // No lanzar excepción para no afectar el flujo principal
            }
        }

        private async Task ProcessStateChangeAsync(InscriptionModality oldInscription, InscriptionModality newInscription, CancellationToken cancellationToken)
        {
            try
            {
                // Obtener información adicional para el evento
                var eventData = await BuildEventDataAsync(newInscription, cancellationToken);
                
                // Determinar el tipo de evento según el estado
                var eventName = await GetEventNameForStateAsync(newInscription.IdStateInscription, cancellationToken);
                
                if (!string.IsNullOrEmpty(eventName))
                {
                    // Encolar evento para procesamiento asíncrono
                    var jobId = _queueService.EnqueueEventNotification(eventName, eventData);
                    
                    _logger.LogInformation("Evento {EventName} encolado para InscriptionModality ID: {Id}, JobId: {JobId}", 
                        eventName, newInscription.Id, jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando cambio de estado para InscriptionModality ID: {Id}", newInscription.Id);
            }
        }

        private async Task<Dictionary<string, object>> BuildEventDataAsync(InscriptionModality inscription, CancellationToken cancellationToken)
        {
            // ✅ OPTIMIZADO: Solo los placeholders que realmente usan las plantillas
            var eventData = new Dictionary<string, object>
            {
                // Datos básicos requeridos por TODAS las plantillas
                ["InscriptionDate"] = inscription.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                ["ApprovalDate"] = inscription.ApprovalDate?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                ["ReviewDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            try
            {
                // ✅ Clean Architecture: Inyección directa sin múltiples scopes
                // 1. ModalityName - REQUERIDO por todas las plantillas
                var modalityRepo = _unitOfWork.GetRepository<Modality, int>();
                var modality = await modalityRepo.GetByIdAsync(inscription.IdModality);
                eventData["ModalityName"] = modality?.Name ?? "Modalidad no encontrada";

                // 2. AcademicPeriod - REQUERIDO por todas las plantillas
                var periodRepo = _unitOfWork.GetRepository<AcademicPeriod, int>();
                var period = await periodRepo.GetByIdAsync(inscription.IdAcademicPeriod);
                eventData["AcademicPeriod"] = period?.Code ?? "Período no encontrado";

                // 3. StudentNames - REQUERIDO por APPROVED y REJECTED
                await AddStudentDataAsync(inscription, eventData, cancellationToken);
                
                // 4. Comentarios específicos por evento - REQUERIDO por APPROVED y REJECTED
                AddEventSpecificComments(inscription, eventData);

                _logger.LogInformation("✅ Event data optimizado generado con {Count} placeholders", eventData.Count);

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo datos para evento. Usando valores por defecto.");
                
                // Valores por defecto mínimos
                eventData["ModalityName"] = "Modalidad";
                eventData["AcademicPeriod"] = "Período Académico";
                eventData["StudentNames"] = "Estudiante";
                eventData["ApprovalComments"] = "Sin comentarios adicionales.";
                eventData["RejectionComments"] = "Revisa los requisitos para esta modalidad.";
            }

            return eventData;
        }

        private async Task AddStudentDataAsync(InscriptionModality inscription, Dictionary<string, object> eventData, CancellationToken cancellationToken)
        {
            // ✅ Clean Architecture: Inyección directa sin múltiples scopes
            var userRepo = _unitOfWork.GetRepository<User, int>();
            var userInscriptionRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            
            // Buscar todos los usuarios asociados a esta inscripción que estén activos
            var activeUserInscriptions = await userInscriptionRepo.GetAllAsync(
                filter: ui => ui.IdInscriptionModality == inscription.Id && ui.StatusRegister == true,
                orderBy: q => q.OrderBy(ui => ui.IdUser)
            );

            _logger.LogInformation("🔍 Encontrados {Count} usuarios activos para InscriptionModality ID: {Id}", 
                activeUserInscriptions.Count(), inscription.Id);

            if (activeUserInscriptions.Any())
            {
                var studentNames = new List<string>();
                var studentEmails = new List<string>();

                foreach (var userInscription in activeUserInscriptions)
                {
                    var student = await userRepo.GetByIdAsync(userInscription.IdUser);
                    if (student != null)
                    {
                        studentNames.Add($"{student.FirstName} {student.LastName}");
                        studentEmails.Add(student.Email);
                    }
                }

                // ✅ OPTIMIZADO: Solo datos necesarios para APPROVED y REJECTED
                if (studentNames.Any())
                {
                    eventData["StudentNames"] = string.Join(", ", studentNames);
                    eventData["StudentEmails"] = string.Join(", ", studentEmails); // Para resolución de destinatarios
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No se encontraron usuarios activos para InscriptionModality ID: {Id}", inscription.Id);
                
                // Fallback: usar el usuario creador
                if (inscription.IdUserCreatedAt.HasValue)
                {
                    _logger.LogInformation("🔄 Usando usuario creador como fallback para InscriptionModality ID: {Id}", inscription.Id);
                    var student = await userRepo.GetByIdAsync(inscription.IdUserCreatedAt.Value);
                    if (student != null)
                    {
                        eventData["StudentNames"] = $"{student.FirstName} {student.LastName}";
                        eventData["StudentEmails"] = student.Email;
                    }
                }
            }
        }

        private static void AddEventSpecificComments(InscriptionModality inscription, Dictionary<string, object> eventData)
        {
            // ✅ OPTIMIZADO: Solo comentarios requeridos por APPROVED y REJECTED
            var hasObservations = !string.IsNullOrEmpty(inscription.Observations);
            
            // Para INSCRIPTION_APPROVED
            eventData["ApprovalComments"] = hasObservations 
                ? $"<p><strong>Comentarios del comité:</strong> {inscription.Observations}</p>" 
                : "<p><em>Sin comentarios adicionales.</em></p>";
            
            // Para INSCRIPTION_REJECTED
            eventData["RejectionComments"] = hasObservations 
                ? inscription.Observations ?? "Sin observaciones específicas." 
                : "Revisa los requisitos y documentación requerida para esta modalidad.";
        }

        private async Task<string> GetEventNameForStateAsync(int stateId, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Clean Architecture: Inyección directa sin múltiples scopes
                var stateRepo = _unitOfWork.GetRepository<StateInscription, int>();
                var state = await stateRepo.GetByIdAsync(stateId);
                
                if (state == null) return string.Empty;

                return state.Code?.ToUpper() switch
                {
                    "APROBADO" => "INSCRIPTION_APPROVED",
                    "RECHAZADO" => "INSCRIPTION_REJECTED",
                    "PENDIENTE" => "INSCRIPTION_CREATED", // Estado inicial
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
