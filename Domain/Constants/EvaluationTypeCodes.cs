namespace Domain.Constants
{
    /// <summary>
    /// Códigos de EvaluationType usados para registrar el historial de observaciones
    /// por modalidad. Cada entidad que almacena observaciones mapea a uno de estos códigos.
    /// </summary>
    public static class EvaluationTypeCodes
    {
        public const string InscriptionModality = "OBSERVACION_INSCRIPCION";
        public const string Proposal = "REVISION_PROPUESTA_IDEA";
        public const string PreliminaryProject = "REVISION_ANTEPROYECTO";
        public const string ProjectFinal = "REVISION_INFORME_FINAL";
        public const string AcademicPractice = "SEGUIMIENTO_PRACTICA";
        public const string CoTerminal = "OBSERVACION_COTERMINAL";
        public const string Seminar = "OBSERVACION_SEMINARIO";
        public const string AcademicAverage = "OBSERVACION_GRADO_PROMEDIO";
        public const string SaberPro = "OBSERVACION_SABER_PRO";
        public const string ScientificArticle = "OBSERVACION_ARTICULO";
    }
}
