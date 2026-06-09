using Api.Responses;
using Application.Shared.Queries.Dashboard;
using Domain.Common.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Api.Controllers
{
    /// <summary>
    /// Reportes y estadísticas agregadas de inscripciones de modalidad de grado.
    /// Todas las métricas se calculan según los filtros recibidos.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator) => _mediator = mediator;

        /// <summary>Catálogo de opciones para poblar los filtros (modalidades, estados, periodos, programas, facultades).</summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var options = await _mediator.Send(new GetDashboardFilterOptionsQuery());
            return Ok(new ApiResponse<DashboardFilterOptions> { Success = true, Data = options });
        }

        /// <summary>KPIs y breakdowns agregados según los filtros aplicados.</summary>
        [HttpPost("summary")]
        public async Task<IActionResult> GetSummary([FromBody] DashboardFilter filter)
        {
            var summary = await _mediator.Send(new GetDashboardSummaryQuery(filter ?? new DashboardFilter()));
            return Ok(new ApiResponse<DashboardSummary> { Success = true, Data = summary });
        }

        /// <summary>
        /// Métricas de drill-down de la modalidad seleccionada (embudo de fases, estados de fase y
        /// distribuciones propias). Requiere exactamente una modalidad en el filtro; si no, devuelve null.
        /// </summary>
        [HttpPost("modality-breakdown")]
        public async Task<IActionResult> GetModalityBreakdown([FromBody] DashboardFilter filter)
        {
            var breakdown = await _mediator.Send(new GetModalityBreakdownQuery(filter ?? new DashboardFilter()));
            return Ok(new ApiResponse<ModalityBreakdown?> { Success = true, Data = breakdown });
        }

        /// <summary>Exporta a CSV las inscripciones que cumplen los filtros.</summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportCsv([FromBody] DashboardFilter filter)
        {
            var rows = await _mediator.Send(new GetDashboardExportQuery(filter ?? new DashboardFilter()));

            var sb = new StringBuilder();
            sb.AppendLine("Id,Modalidad,Estado,Periodo,Programa,Facultad,Estudiantes,FechaCreacion,FechaAprobacion,FaseActual,EstadoFase,DetalleModalidad");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    Csv(r.InscriptionId.ToString()),
                    Csv(r.ModalityName),
                    Csv(r.StateName),
                    Csv(r.AcademicPeriod),
                    Csv(r.Program),
                    Csv(r.Faculty),
                    Csv(r.Students),
                    Csv(r.CreatedAt),
                    Csv(r.ApprovalDate),
                    Csv(r.CurrentPhase),
                    Csv(r.PhaseState),
                    Csv(r.ModalityDetail)));
            }

            // BOM UTF-8 para que Excel respete los acentos.
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"reporte_inscripciones_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }

        /// <summary>Escapa un valor para CSV (comillas y separadores).</summary>
        private static string Csv(string? value)
        {
            value ??= string.Empty;
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
