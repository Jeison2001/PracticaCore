using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestNotificationController : ControllerBase
    {
        [HttpPost("test-hangfire")]
        public IActionResult TestHangfire()
        {
            try
            {
                // Encolar job simple de prueba
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Test job ejecutado a las {DateTime.Now}"));
                
                return Ok(new { 
                    Message = "Job de prueba encolado", 
                    JobId = jobId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    Error = ex.Message, 
                    StackTrace = ex.StackTrace 
                });
            }
        }

        [HttpPost("test-notification")]
        public IActionResult TestNotification([FromServices] Domain.Interfaces.Notifications.IEmailNotificationQueueService queueService)
        {
            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["TestMessage"] = "Mensaje de prueba",
                    ["Timestamp"] = DateTime.Now.ToString()
                };

                var jobId = queueService.EnqueueEventNotification("TEST_EVENT", eventData);
                
                return Ok(new { 
                    Message = "Notificación de prueba encolada", 
                    JobId = jobId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    Error = ex.Message, 
                    StackTrace = ex.StackTrace 
                });
            }
        }
    }
}
