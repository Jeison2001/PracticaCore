using Domain.Entities;
using Domain.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces.Notifications;

namespace Api.Controllers
{
    /// <summary>
    /// Controlador para probar el sistema de notificaciones automáticas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailNotificationEventService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _directNotificationService;

        public EmailTestController(
            IEmailNotificationEventService notificationService,
            IUnitOfWork unitOfWork,
            INotificationService directNotificationService)
        {
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _directNotificationService = directNotificationService;
        }

        /// <summary>
        /// Verifica si existe configuración para un evento
        /// </summary>
        [HttpGet("check-config/{eventName}")]
        public async Task<IActionResult> CheckEventConfiguration(string eventName)
        {
            var hasConfig = await _notificationService.HasActiveConfigurationAsync(eventName);
            
            if (hasConfig)
            {
                // Obtener detalles de la configuración
                var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
                var config = await configRepo.GetFirstOrDefaultAsync(
                    c => c.EventName == eventName && c.IsActive && c.StatusRegister, 
                    CancellationToken.None
                );

                var rulesRepo = _unitOfWork.GetRepository<EmailRecipientRule, int>();
                var rules = await rulesRepo.GetAllAsync(
                    filter: r => r.EmailNotificationConfigId == config!.Id && r.StatusRegister
                );

                return Ok(new
                {
                    hasConfiguration = true,
                    eventName = eventName,
                    subjectTemplate = config?.SubjectTemplate,
                    recipientRules = rules.Select(r => new
                    {
                        recipientType = r.RecipientType,
                        ruleType = r.RuleType,
                        ruleValue = r.RuleValue,
                        priority = r.Priority
                    }).ToList()
                });
            }

            return Ok(new { hasConfiguration = false, eventName = eventName });
        }

        /// <summary>
        /// Simula el evento de inscripción de modalidad
        /// </summary>
        [HttpPost("test-inscription")]
        public async Task<IActionResult> TestInscriptionEvent([FromBody] TestInscriptionRequest request)
        {
            try
            {
                // Simular datos del evento
                var eventData = new Dictionary<string, object>
                {
                    ["StudentId"] = 1,
                    ["StudentName"] = request.StudentName ?? "Juan Pérez",
                    ["StudentEmail"] = request.StudentEmail ?? "estudiante@test.com",
                    ["ModalityName"] = request.ModalityName ?? "Trabajo de Grado",
                    ["AcademicProgram"] = "Ingeniería de Sistemas",
                    ["FacultyId"] = 1,
                    ["AcademicProgramId"] = 1,
                    ["InscriptionDate"] = DateTime.Now.ToString("dd/MM/yyyy"),
                    ["InscriptionId"] = 123
                };

                // Disparar evento automático
                await _notificationService.ProcessEventAsync("INSCRIPTION_CREATED", eventData);

                return Ok(new
                {
                    success = true,
                    message = "Evento INSCRIPTION_CREATED procesado exitosamente",
                    eventData = eventData
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al procesar evento",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lista todos los eventos configurados
        /// </summary>
        [HttpGet("configured-events")]
        public async Task<IActionResult> GetConfiguredEvents()
        {
            var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
            var configs = await configRepo.GetAllAsync(
                filter: c => c.IsActive && c.StatusRegister
            );

            return Ok(new
            {
                totalEvents = configs.Count(),
                events = configs.Select(c => new
                {
                    eventName = c.EventName,
                    subjectTemplate = c.SubjectTemplate,
                    isActive = c.IsActive,
                    createdAt = c.CreatedAt
                }).ToList()
            });
        }

        /// <summary>
        /// Envía un email directo usando el provider configurado
        /// </summary>
        [HttpPost("send-direct-email")]
        public async Task<IActionResult> SendDirectEmail([FromBody] DirectEmailRequest request)
        {
            try
            {
                var emailNotification = new EmailNotification
                {
                    To = request.To,
                    Subject = request.Subject,
                    Body = request.Body,
                    IsHtml = request.IsHtml
                };

                var success = await _directNotificationService.SendEmailAsync(emailNotification);

                return Ok(new
                {
                    success = success,
                    message = success ? "Email enviado exitosamente" : "Error al enviar email",
                    provider = "Gmail", // Basado en la configuración actual
                    to = request.To,
                    subject = request.Subject
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al enviar email directo",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Procesa un evento de notificación automática
        /// </summary>
        [HttpPost("send-notification-event")]
        public async Task<IActionResult> SendNotificationEvent([FromBody] NotificationEventRequest request)
        {
            try
            {
                await _notificationService.ProcessEventAsync(request.EventName, request.EventData);

                return Ok(new
                {
                    success = true,
                    message = $"Evento {request.EventName} procesado exitosamente",
                    eventName = request.EventName,
                    eventData = request.EventData
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Error al procesar evento {request.EventName}",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Prueba la resolución de destinatarios para un evento
        /// </summary>
        [HttpPost("test-recipient-resolution")]
        public async Task<IActionResult> TestRecipientResolution([FromBody] RecipientTestRequest request)
        {
            try
            {
                // Obtener configuración del evento
                var configRepo = _unitOfWork.GetRepository<EmailNotificationConfig, int>();
                var config = await configRepo.GetFirstOrDefaultAsync(
                    c => c.EventName == request.EventName && c.IsActive && c.StatusRegister,
                    CancellationToken.None
                );

                if (config == null)
                {
                    return BadRequest(new { error = $"No hay configuración activa para el evento {request.EventName}" });
                }

                // Obtener reglas de destinatarios
                var rulesRepo = _unitOfWork.GetRepository<EmailRecipientRule, int>();
                var rules = await rulesRepo.GetAllAsync(
                    filter: r => r.EmailNotificationConfigId == config.Id && r.StatusRegister
                );

                if (!rules.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        eventName = request.EventName,
                        totalRecipients = 0,
                        recipients = new List<object>(),
                        message = "No hay reglas de destinatarios configuradas"
                    });
                }

                // Obtener servicio de resolución de destinatarios
                var recipientResolverType = typeof(IEmailRecipientResolverService);
                var recipientResolver = HttpContext.RequestServices.GetService(recipientResolverType) as IEmailRecipientResolverService;

                if (recipientResolver == null)
                {
                    return BadRequest(new { error = "Servicio de resolución de destinatarios no disponible" });
                }

                var recipientsResult = await recipientResolver.ResolveRecipientsAsync(rules.ToList(), request.EventData);

                return Ok(new
                {
                    success = true,
                    eventName = request.EventName,
                    totalRecipients = recipientsResult.To.Count + recipientsResult.Cc.Count + recipientsResult.Bcc.Count,
                    recipients = new
                    {
                        to = recipientsResult.To,
                        cc = recipientsResult.Cc,
                        bcc = recipientsResult.Bcc
                    },
                    rulesProcessed = rules.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al resolver destinatarios",
                    error = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// Request para probar inscripción
    /// </summary>
    public record TestInscriptionRequest(
        string? StudentName = null,
        string? StudentEmail = null,
        string? ModalityName = null
    );

    /// <summary>
    /// Request para envío directo de email
    /// </summary>
    public record DirectEmailRequest(
        string To,
        string Subject,
        string Body,
        bool IsHtml = true
    );

    /// <summary>
    /// Request para evento de notificación
    /// </summary>
    public record NotificationEventRequest(
        string EventName,
        Dictionary<string, object> EventData
    );

    /// <summary>
    /// Request para probar resolución de destinatarios
    /// </summary>
    public record RecipientTestRequest(
        string EventName,
        Dictionary<string, object> EventData
    );
}
