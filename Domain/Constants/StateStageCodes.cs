namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Estado de Etapa (StateStage) tal como están registrados en la BD.
    /// </summary>
    public static class StateStageCodes
    {
        // Práctica Académica
        public const string PaInscripcionPendDoc       = "PA_INSCRIPCION_PEND_DOC";
        public const string PaInscripcionAprobada      = "PA_INSCRIPCION_APROBADA";
        public const string PaDesarrolloAprobada       = "PA_DESARROLLO_APROBADA";
        public const string PaInformeFinalEnRevision  = "PA_INFORME_FINAL_EN_REVISION";
        public const string PaAprobado                = "PA_APROBADO";

        // Propuesta (Proyecto de Grado — Fase Propuesta)
        public const string PgPropuestaInicial      = "PG_PROPUESTA_INICIAL";
        public const string PropPertinente         = "PROP_PERTINENTE";

        // Anteproyecto (Proyecto de Grado — Fase Anteproyecto)
        public const string ApAprobado             = "AP_APROBADO";
        public const string ApPendienteDocumento   = "AP_PENDIENTE_DOCUMENTO";
        public const string ApRadicadoPendAsigEval = "AP_RADICADO_PEND_ASIG_EVAL";

        // Proyecto Final
        public const string PfinfPendienteInforme      = "PFINF_PENDIENTE_INFORME";
        public const string PfinfRadicadoEnEvaluacion  = "PFINF_RADICADO_EN_EVALUACION";
    }
}
