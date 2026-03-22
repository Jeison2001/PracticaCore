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
        PFINF_PENDIENTE_INFORME,

        // Academic Practice (Práctica Académica) - Modelo Híbrido Final Corregido
        // Fase Inscripción (5 estados)
        PA_INSCRIPCION_PEND_DOC,
        PA_INSCRIPCION_EN_REVISION,
        PA_INSCRIPCION_OBSERVACIONES,
        PA_INSCRIPCION_APROBADA,
        PA_INSCRIPCION_RECHAZADA,
        
        // Fase Desarrollo (5 estados - CORREGIDO)
        PA_DESARROLLO_PEND_DOC,
        PA_DESARROLLO_EN_REVISION,  // RENOMBRADO desde PA_EN_DESARROLLO
        PA_DESARROLLO_OBSERVACIONES,  // NUEVO
        PA_DESARROLLO_APROBADA,
        PA_DESARROLLO_NO_APROBADA,    // NUEVO
        
        // Fase Informe Final (4 estados)
        PA_INFORME_FINAL_PEND_DOC,
        PA_INFORME_FINAL_EN_REVISION,
        PA_INFORME_FINAL_OBSERVACIONES,
        PA_APROBADO,
        PA_NO_APROBADO,

        // Sustentacion (SUST)
        SUST_PENDIENTE_PROGRAMACION,
        SUST_PROGRAMADA,
        SUST_APROBADA,
        SUST_NO_APROBADA_REINTENTO,
        SUST_REPROBADA_FINAL,

        // Co-terminal (CT)
        CT_PENDIENTE_DOCUMENTACION,
        CT_RADICADO,
        CT_CON_OBSERVACIONES,
        CT_APROBADO,
        CT_RECHAZADO,

        // Seminario (SEM)
        SEM_PENDIENTE_DOCUMENTACION,
        SEM_RADICADO,
        SEM_CON_OBSERVACIONES,
        SEM_APROBADO,
        SEM_RECHAZADO,

        // Grado por Promedio (GP)
        GP_PENDIENTE_DOCUMENTACION,
        GP_RADICADO,
        GP_CON_OBSERVACIONES,
        GP_APROBADO,
        GP_RECHAZADO,

        // Saber Pro (SP)
        SP_PENDIENTE_DOCUMENTACION,
        SP_RADICADO,
        SP_CON_OBSERVACIONES,
        SP_APROBADO,
        SP_RECHAZADO,

        // Articulo de Investigacion - Inscripcion (ART_INS)
        ART_INS_PENDIENTE,
        ART_INS_RADICADO,
        ART_INS_OBSERVACIONES,
        ART_INS_APROBADO,
        ART_INS_RECHAZADO,

        // Articulo de Investigacion - Publicacion (ART_PUB)
        ART_PUB_PENDIENTE,
        ART_PUB_RADICADO,
        ART_PUB_OBSERVACIONES,
        ART_PUB_APROBADO,
        ART_PUB_RECHAZADO
    }
}
