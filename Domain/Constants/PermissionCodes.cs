namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Permiso tal como están registrados en la tabla "Permission".
    /// Organizados por módulo/modalidad para facilitar su localización.
    /// </summary>
    public static class PermissionCodes
    {
        // ---------- Práctica Académica ----------
        public static class PracticaAcademica
        {
            // Fase 0: Inscripción
            public const string N1PA    = "N1PA";
            public const string N2PAF0  = "N2PAF0";
            public const string N3PAF0R = "N3PAF0R";
            public const string N3PAF0C = "N3PAF0C";

            // Fase 1: Desarrollo
            public const string N2PAF1  = "N2PAF1";
            public const string N3PAF1R = "N3PAF1R";
            public const string N3PAF1C = "N3PAF1C";

            // Fase 2: Evaluación
            public const string N2PAF2  = "N2PAF2";
            public const string N3PAF2R = "N3PAF2R";
            public const string N3PAF2C = "N3PAF2C";
        }

        // ---------- Proyecto de Grado ----------
        public static class ProyectoGrado
        {
            // Fase Propuesta
            public const string N1PG   = "N1PG";
            public const string N2PGP  = "N2PGP";
            public const string N3PGRP = "N3PGRP";
            public const string N3PGCP = "N3PGCP";

            // Fase Anteproyecto
            public const string N2PGA  = "N2PGA";
            public const string N3PGRA = "N3PGRA";
            public const string N3PGCA = "N3PGCA";

            // Fase Proyecto
            public const string N2PGPR  = "N2PGPR";
            public const string N3PGRPR = "N3PGRPR";
            public const string N3PGCPR = "N3PGCPR";
        }

        // ---------- Co-Terminal ----------
        public static class CoTerminal
        {
            public const string N1CT  = "N1CT";
            public const string N2CTC = "N2CTC";
        }

        // ---------- Seminario ----------
        public static class Seminario
        {
            public const string N1SA  = "N1SA";
            public const string N2SAC = "N2SAC";
        }

        // ---------- Publicación de Artículo ----------
        public static class PublicacionArticulo
        {
            public const string N1PC  = "N1PC";
            public const string N2PCC = "N2PCC";
        }

        // ---------- Grado por Promedio ----------
        public static class GradoPromedio
        {
            public const string N1GP    = "N1GP";
            public const string N2GPES  = "N2GPES";
            public const string N2GPR   = "N2GPR";
        }

        // ---------- Saber Pro ----------
        public static class SaberPro
        {
            public const string N1SP  = "N1SP";
            public const string N2SPC = "N2SPC";
        }
    }
}
