namespace Domain.Enums
{
    /// <summary>
    /// Enum para mapear los estados clave de la práctica académica a sus IDs CONSECUTIVOS de base de datos.
    /// IDs generados con secuencia reiniciada: 18-32 (sin saltos).
    /// </summary>
    public enum AcademicPracticeStateStageEnum
    {
        InscriptionApproved = 21, // PA_INSCRIPCION_APROBADA (ID consecutivo)
        DevelopmentApproved = 26, // PA_DESARROLLO_APROBADA (ID consecutivo)
        FinalApproved = 31 // PA_APROBADO (ID consecutivo)
    }
}
