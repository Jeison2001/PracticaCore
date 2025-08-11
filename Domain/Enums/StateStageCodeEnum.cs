namespace Domain.Enums
{
    /// <summary>
    /// Enum representing the string codes of StateStage.Code across relevant phases.
    /// Use Enum.TryParse on the StateStage.Code to map to this type.
    /// </summary>
    public enum StateStageCodeEnum
    {
        // Proposal phase
        PROP_RADICADA,
        PROP_PERTINENTE,
        PROP_NO_PERTINENTE,

        // Preliminary Project (Anteproyecto)
        AP_RADICADO_PEND_ASIG_EVAL,
        AP_APROBADO,
        AP_CON_OBSERVACIONES,
        AP_EN_EVALUACION,
        AP_PENDIENTE_DOCUMENTO,

        // Project Final (Informe Final)
        PFINF_RADICADO_EN_EVALUACION,
        PFINF_INFORME_APROBADO,
        PFINF_INFORME_CON_OBSERVACIONES,
        PFINF_PENDIENTE_INFORME
    }
}
