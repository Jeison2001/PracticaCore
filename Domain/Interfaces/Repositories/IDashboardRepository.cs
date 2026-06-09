using Domain.Common.Dashboard;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Repositories
{
    /// <summary>
    /// Consultas de agregación para el dashboard de reportes. Solo lectura.
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

        /// <summary>Filas planas (una por inscripción) para exportación a CSV, aplicando los mismos filtros.</summary>
        Task<List<DashboardExportRow>> GetExportRowsAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
    }
}
