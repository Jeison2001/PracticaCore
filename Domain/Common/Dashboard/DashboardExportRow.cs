namespace Domain.Common.Dashboard
{
    /// <summary>Fila plana enriquecida de inscripción para listado paginado y exportación CSV/Excel.</summary>
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
        string ModalityDetail = "",
        string ModalityCode = "",
        string LifecycleCode = "",
        string LifecycleStatus = "",
        string StudentIdentifications = "",
        string StudentEmails = "",
        string Teachers = "",
        string Title = "",
        string ResearchLine = "",
        string ResearchSubLine = "",
        string LastActivityAt = "",
        int DaysElapsed = 0);
}
