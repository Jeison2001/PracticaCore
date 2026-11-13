namespace Domain.Constants
{
    /// <summary>
    /// Códigos de Permiso tal como están registrados en la tabla "Permission".
    /// Organizados por módulo/modalidad para facilitar su localización.
    /// </summary>
    public static class PermissionCodes
    {
        // ---------- Reportes y Estadísticas ----------
        public static class Reportes
        {
            public const string N1ED  = "N1ED";
            public const string N2EDD = "N2EDD";
        }

        // ---------- Configuraciones y Seguridad ----------
        public static class Configuraciones
        {
            public const string N1CF     = "N1CF";
            public const string N2CFP    = "N2CFP";
            public const string N2CFS    = "N2CFS";
            public const string N3CFSU   = "N3CFSU";
            public const string N3CFSRL  = "N3CFSRL";
            public const string N3CFSC   = "N3CFSC";
            public const string N2CFAC   = "N2CFAC";
            public const string N2CFDO   = "N2CFDO";
            public const string N2CFIN   = "N2CFIN";
            public const string N3CFACM  = "N3CFACM";
            public const string N3CFACP  = "N3CFACP";
            public const string N3CFACPE = "N3CFACPE";
            public const string N3CFDOT  = "N3CFDOT";
            public const string N3CFINA  = "N3CFINA";
            public const string N3CFING  = "N3CFING";
            public const string N3CFINL  = "N3CFINL";
            public const string N3CFINS  = "N3CFINS";
        }

        // ---------- Modalidades de Grado ----------
        public static class ModalidadesGrado
        {
            public const string N1MG   = "N1MG";
            public const string N2MGR  = "N2MGR";
            public const string N2MGC  = "N2MGC";
            public const string N2MGP  = "N2MGP";
            public const string N3MGPG = "N3MGPG";
            public const string N3MGPR = "N3MGPR";
        }

        // ---------- Práctica Académica ----------
        public static class PracticaAcademica
        {
            // Fase 0: Inscripción
            public const string N1PA     = "N1PA";
            public const string N2PAF0   = "N2PAF0";
            public const string N3PAF0R  = "N3PAF0R";
            public const string N3PAF0C  = "N3PAF0C";
            public const string N3PAF0P  = "N3PAF0P";
            public const string N4PAF0PG = "N4PAF0PG";
            public const string N4PAF0PR = "N4PAF0PR";
            public const string N4PAF0PA = "N4PAF0PA";

            // Fase 1: Desarrollo
            public const string N2PAF1   = "N2PAF1";
            public const string N3PAF1R  = "N3PAF1R";
            public const string N3PAF1C  = "N3PAF1C";
            public const string N3PAF1P  = "N3PAF1P";
            public const string N4PAF1PG = "N4PAF1PG";
            public const string N4PAF1PR = "N4PAF1PR";

            // Fase 2: Evaluación
            public const string N2PAF2   = "N2PAF2";
            public const string N3PAF2R  = "N3PAF2R";
            public const string N3PAF2C  = "N3PAF2C";
            public const string N3PAF2P  = "N3PAF2P";
            public const string N4PAF2PG = "N4PAF2PG";
            public const string N4PAF2PR = "N4PAF2PR";
        }

        // ---------- Proyecto de Grado ----------
        public static class ProyectoGrado
        {
            // Fase Propuesta
            public const string N1PG    = "N1PG";
            public const string N2PGP   = "N2PGP";
            public const string N3PGRP  = "N3PGRP";
            public const string N3PGCP  = "N3PGCP";
            public const string N3PGPP  = "N3PGPP";
            public const string N4PGPPG = "N4PGPPG";
            public const string N4PGPPR = "N4PGPPR";
            public const string N4PGPPA = "N4PGPPA";
            public const string N4PGPPC = "N4PGPPC";

            // Fase Anteproyecto
            public const string N2PGA   = "N2PGA";
            public const string N3PGRA  = "N3PGRA";
            public const string N3PGCA  = "N3PGCA";
            public const string N3PGPA  = "N3PGPA";
            public const string N4PGPAG = "N4PGPAG";
            public const string N4PGPAR = "N4PGPAR";
            public const string N4PGPAA = "N4PGPAA";

            // Fase Proyecto
            public const string N2PGPR   = "N2PGPR";
            public const string N3PGRPR  = "N3PGRPR";
            public const string N3PGCPR  = "N3PGCPR";
            public const string N3PGPPR  = "N3PGPPR";
            public const string N4PGPPRG = "N4PGPPRG";
            public const string N4PGPPRR = "N4PGPPRR";

            // Fase Sustentación (el estudiante consulta; no registra)
            public const string N2PGSU  = "N2PGSU";
            public const string N3PGCSU = "N3PGCSU";
            public const string N3PGPSU = "N3PGPSU";
            public const string N4PGPSG = "N4PGPSG";
            public const string N4PGPSR = "N4PGPSR";
        }

        // ---------- Co-Terminal ----------
        public static class CoTerminal
        {
            public const string N1CT    = "N1CT";
            public const string N2CTC   = "N2CTC";
            public const string N2CTR   = "N2CTR";
            public const string N2CTSG  = "N2CTSG";
            public const string N3CTSGG = "N3CTSGG";
            public const string N3CTSGR = "N3CTSGR";
        }

        // ---------- Seminario ----------
        public static class Seminario
        {
            public const string N1SA    = "N1SA";
            public const string N2SAC   = "N2SAC";
            public const string N2SAR   = "N2SAR";
            public const string N2SASG  = "N2SASG";
            public const string N3SASGG = "N3SASGG";
            public const string N3SASGR = "N3SASGR";
        }

        // ---------- Publicación de Artículo ----------
        public static class PublicacionArticulo
        {
            public const string N1PC    = "N1PC";
            public const string N2PCC   = "N2PCC";
            public const string N2PCR   = "N2PCR";
            public const string N2PCSG  = "N2PCSG";
            public const string N3PCSGG = "N3PCSGG";
            public const string N3PCSGR = "N3PCSGR";
        }

        // ---------- Grado por Promedio ----------
        public static class GradoPromedio
        {
            public const string N1GP    = "N1GP";
            public const string N2GPES  = "N2GPES";
            public const string N2GPR   = "N2GPR";
            public const string N2GPSG  = "N2GPSG";
            public const string N3GPSGG = "N3GPSGG";
            public const string N3GPSGR = "N3GPSGR";
        }

        // ---------- Saber Pro ----------
        public static class SaberPro
        {
            public const string N1SP    = "N1SP";
            public const string N2SPC   = "N2SPC";
            public const string N2SPR   = "N2SPR";
            public const string N2SPSG  = "N2SPSG";
            public const string N3SPSGG = "N3SPSGG";
            public const string N3SPSGR = "N3SPSGR";
        }
    }
}
