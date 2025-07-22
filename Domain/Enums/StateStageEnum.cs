namespace Domain.Enums
{
    public enum StateStageEnum
    {
        PROP_RADICADA = 1, // Propuesta registrada, pendiente de revisión por comité
        PROP_PERTINENTE = 2, // Idea aprobada por comité para continuar a anteproyecto
        PROP_NO_PERTINENTE = 3 // Idea no aprobada por comité o requiere ajustes mayores
    }
}
