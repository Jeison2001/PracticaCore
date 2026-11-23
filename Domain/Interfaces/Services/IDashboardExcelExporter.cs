using Domain.Common.Dashboard;
using Domain.Interfaces.Common;

namespace Domain.Interfaces.Services
{
    /// <summary>
    /// Genera el libro Excel (.xlsx) multipestaña institucional del dashboard de reportes.
    /// </summary>
    public interface IDashboardExcelExporter : IScopedService
    {
        byte[] GenerateExcel(
            DashboardFilter filter,
            DashboardSummary summary,
            ModalityBreakdown? breakdown,
            DashboardOperationalAlerts alerts,
            List<DashboardExportRow> rows,
            DashboardFilterOptions options);
    }
}
