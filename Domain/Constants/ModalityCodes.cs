namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Modalidad de Grado tal como están registrados en la tabla "Modality".
    /// Estos valores son inmutables y forman parte del contrato de dominio del sistema.
    /// </summary>
    public static class ModalityCodes
    {
        public const string ProyectoGrado        = "PROYECTO_GRADO";
        public const string PracticaAcademica    = "PRACTICA_ACADEMICA";
        public const string CoTerminal           = "COTERMINAL";
        public const string SeminarioAct         = "SEMINARIO_ACT";
        public const string PublicacionArticulo  = "PUBLICACION_ARTICULO";
        public const string GradoPromedio        = "GRADO_PROMEDIO";
        public const string SaberPro             = "SABER_PRO";
    }
}
