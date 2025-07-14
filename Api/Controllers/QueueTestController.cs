using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces.Notifications;
using Api.Responses;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueTestController : ControllerBase
    {
        private readonly IEmailNotificationQueueService _queueService;
        private readonly ILogger<QueueTestController> _logger;

        public QueueTestController(
            IEmailNotificationQueueService queueService,
            ILogger<QueueTestController> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        /// <summary>
        /// Encola un email directo para envío en background
        /// </summary>
        [HttpPost("enqueue-direct-email")]
        public IActionResult EnqueueDirectEmail([FromBody] EnqueueDirectEmailRequest request)
        {
            try
            {
                var jobId = _queueService.EnqueueDirectEmail(
                    request.To,
                    request.Subject,
                    request.Body,
                    request.IsHtml,
                    request.CC,
                    request.BCC
                );

                _logger.LogInformation("Email directo encolado con JobId: {JobId}", jobId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        JobId = jobId,
                        Message = "Email encolado exitosamente para procesamiento en background",
                        To = request.To,
                        Subject = request.Subject
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al encolar email directo");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al encolar email: {ex.Message}" }
                });
            }
        }

        /// <summary>
        /// Encola un evento de notificación para procesamiento en background
        /// </summary>
        [HttpPost("enqueue-notification-event")]
        public IActionResult EnqueueNotificationEvent([FromBody] EnqueueEventRequest request)
        {
            try
            {
                var jobId = _queueService.EnqueueEventNotification(request.EventName, request.EventData);

                _logger.LogInformation("Evento {EventName} encolado con JobId: {JobId}", request.EventName, jobId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        JobId = jobId,
                        Message = "Evento encolado exitosamente para procesamiento en background",
                        EventName = request.EventName,
                        EventData = request.EventData
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al encolar evento de notificación");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error al encolar evento: {ex.Message}" }
                });
            }
        }

        /// <summary>
        /// Prueba rápida: encolar ejemplo de inscripción
        /// </summary>
        [HttpPost("test-inscription-queue")]
        public IActionResult TestInscriptionQueue()
        {
            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["InscriptionId"] = 9999,
                    ["ModalityName"] = "PRACTICA ACADEMICA (COLA)",
                    ["AcademicPeriod"] = "2025-2",
                    ["InscriptionDate"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    ["StudentsCount"] = 1,
                    ["IsMultipleStudents"] = false,
                    ["StudentName"] = "Usuario de Prueba Cola",
                    ["StudentEmail"] = "prueba.cola@universidad.edu",
                    ["StudentIdentification"] = "999999999",
                    ["StudentNames"] = "Usuario de Prueba Cola",
                    ["StudentEmails"] = "prueba.cola@universidad.edu",
                    ["StudentIdentifications"] = "999999999",
                    ["Observations"] = "Prueba del sistema de colas"
                };

                var jobId = _queueService.EnqueueEventNotification("INSCRIPTION_CREATED", eventData);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        JobId = jobId,
                        Message = "Evento de prueba de inscripción encolado exitosamente",
                        EventName = "INSCRIPTION_CREATED",
                        TestData = "Datos de prueba para validar el sistema de colas"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en prueba de cola de inscripción");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { $"Error en prueba: {ex.Message}" }
                });
            }
        }
    }

    public class EnqueueDirectEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public string[]? CC { get; set; }
        public string[]? BCC { get; set; }
    }

    public class EnqueueEventRequest
    {
        public string EventName { get; set; } = string.Empty;
        public Dictionary<string, object> EventData { get; set; } = new();
    }
}
