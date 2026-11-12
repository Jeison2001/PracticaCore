namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Tipos de Documento (DocumentType) en la BD.
    /// </summary>
    public static class DocumentTypeCodes
    {
        // Documento Entregable de Anteproyecto (Proyecto de Grado)
        public const string AnteproyectoEntregable = "ANTEPROY_ENTREGABLE_EST_PG_IS";

        // Documento Entregable de Proyecto/Informe Final (Proyecto de Grado)
        public const string ProyectoFinalEntregable = "PROYINF_ENTREGABLE_EST_PG_IS";

        // Documentos de Fase Sustentación (Proyecto de Grado)
        public const string SustentacionActa = "SUST_ACTA_CALIF_PG_IS";
        public const string SustentacionRubrica = "SUST_RUBRICA_EVAL_PG_IS";
    }
}
