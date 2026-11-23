namespace Domain.Common.Dashboard
{
    /// <summary>Página de registros enriquecidos del dashboard con conteo total.</summary>
    public record DashboardRecordsPage(
        List<DashboardExportRow> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
