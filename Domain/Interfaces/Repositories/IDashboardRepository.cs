using Domain.Common.Dashboard;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    /// <summary>
    /// Consultas de agregación, alertas operativas y exportación para el dashboard de reportes. Solo lectura.
    /// </summary>
    public interface IDashboardRepository : IScopedService
    {
        Task<DashboardSummary> GetSummaryAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
        Task<DashboardFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Métricas de drill-down específicas de la modalidad seleccionada. Devuelve null si el filtro
        /// no trae exactamente una modalidad (el drill-down solo aplica con una modalidad).
        /// </summary>
        Task<ModalityBreakdown?> GetModalityBreakdownAsync(DashboardFilter filter, CancellationToken cancellationToken = default);

        /// <summary>Alertas operativas: estancamiento, SLA Acuerdo 015, riesgo de vencimiento, sustentaciones y retrabajo.</summary>
        Task<DashboardOperationalAlerts> GetOperationalAlertsAsync(DashboardFilter filter, CancellationToken cancellationToken = default);

        /// <summary>Listado paginado y buscable de inscripciones enriquecidas.</summary>
        Task<DashboardRecordsPage> GetPagedRecordsAsync(DashboardFilter filter, CancellationToken cancellationToken = default);

        /// <summary>Filas planas enriquecidas (una por inscripción, con tope seguro) para exportación CSV/Excel.</summary>
        Task<List<DashboardExportRow>> GetExportRowsAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
    }
}
