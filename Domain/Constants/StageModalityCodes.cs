namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Fase de Modalidad (StageModality) tal como están registrados en la BD.
    /// </summary>
    public static class StageModalityCodes
    {
        // Práctica Académica
        public const string PaFaseInscripcion  = "PA_FASE_INSCRIPCION";
        public const string PaFaseDesarrollo   = "PA_FASE_DESARROLLO";
        public const string PaFaseEvaluacion   = "PA_FASE_EVALUACION";

        // Proyecto de Grado
        public const string PgFasePropuesta       = "PG_FASE_PROPUESTA";
        public const string PgFaseAnteproyecto    = "PG_FASE_ANTEPROYECTO";
        public const string PgFaseProyectoInforme = "PG_FASE_PROYECTO_INFORME";
        public const string PgFaseProyecto        = "PG_FASE_PROYECTO";
    }
}
