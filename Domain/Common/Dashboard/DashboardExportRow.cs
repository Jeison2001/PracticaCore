namespace Domain.Common.Dashboard
{
    /// <summary>Fila plana de inscripción para exportación CSV del reporte.</summary>
    public record DashboardExportRow(
        int InscriptionId,
        string ModalityName,
        string StateName,
        string AcademicPeriod,
        string Program,
        string Faculty,
        string Students,
        string CreatedAt,
        string ApprovalDate,
        string CurrentPhase = "",
        string PhaseState = "",
        string ModalityDetail = "");
}
