using Api.Authorization;
using Api.Responses;
using Domain.Common.Extensions;
using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Interfaces.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Administración institucional del motor de alertas SLA, umbrales normativos, destinatarios en copia (CC)
    /// y bitácora de envíos automáticos.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SlaAlertSettingsController : ControllerBase
    {
        private readonly ISlaAlertSettingsService _slaAlertSettingsService;
        private readonly ISlaDeadlineAlertService _slaDeadlineAlertService;

        public SlaAlertSettingsController(
            ISlaAlertSettingsService slaAlertSettingsService,
            ISlaDeadlineAlertService slaDeadlineAlertService)
        {
            _slaAlertSettingsService = slaAlertSettingsService;
            _slaDeadlineAlertService = slaDeadlineAlertService;
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFNOA)]
        [HttpGet]
        public async Task<IActionResult> GetOverview(CancellationToken cancellationToken = default)
        {
            var overview = await _slaAlertSettingsService.GetOverviewAsync(cancellationToken);
            return Ok(new ApiResponse<SlaAlertSettingsOverviewDto>
            {
                Success = true,
                Data = overview
            });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFNOA)]
        [HttpPut]
        public async Task<IActionResult> UpdateSettings(
            [FromBody] UpdateSlaAlertSettingsRequest? request,
            CancellationToken cancellationToken = default)
        {
            int? currentUserId = User?.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
            if (currentUserId is <= 0)
            {
                currentUserId = null;
            }

            var (overview, errors) = await _slaAlertSettingsService.UpdateSettingsAsync(
                request,
                currentUserId,
                cancellationToken);

            if (errors.Count > 0 || overview == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Errors = errors,
                    Messages = errors
                });
            }

            return Ok(new ApiResponse<SlaAlertSettingsOverviewDto>
            {
                Success = true,
                Data = overview,
                Messages = ["Configuración de alertas SLA actualizada correctamente."]
            });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFNOA)]
        [HttpPost("run-now")]
        public async Task<IActionResult> RunNow(CancellationToken cancellationToken = default)
        {
            var summary = await _slaDeadlineAlertService.ProcessDueAlertsAsync(cancellationToken);
            return Ok(new ApiResponse<SlaAlertExecutionSummary>
            {
                Success = true,
                Data = summary
            });
        }

        [RequirePermission(PermissionCodes.Configuraciones.N3CFNOA)]
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? alertCode = null,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var logs = await _slaAlertSettingsService.GetLogsAsync(
                page,
                pageSize,
                alertCode,
                search,
                cancellationToken);

            return Ok(new ApiResponse<SlaAlertDispatchLogsPageDto>
            {
                Success = true,
                Data = logs
            });
        }
    }
}
