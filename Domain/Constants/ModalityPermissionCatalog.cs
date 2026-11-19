using System.Collections.ObjectModel;

namespace Domain.Constants
{
    public static class ModalityPermissionCatalog
    {
        public static readonly string[] InscriptionModalityManagement =
        [
            PermissionCodes.ModalidadesGrado.N3MGPR,
            PermissionCodes.ModalidadesGrado.N3MGPG,
        ];

        public static readonly string[] PgPropuestaRead =
        [
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPA,
        ];

        public static readonly string[] PgPropuestaManagement =
        [
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPA,
        ];

        public static readonly string[] PgAnteproyectoManagement =
        [
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
        ];

        public static readonly string[] PgProyectoManagement =
        [
            PermissionCodes.ProyectoGrado.N4PGPPRG,
        ];

        public static readonly string[] PgSustentacionManagement =
        [
            PermissionCodes.ProyectoGrado.N4PGPSG,
        ];

        public static readonly string[] ProyectoGradoAllRead =
        [
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
            PermissionCodes.ProyectoGrado.N4PGPPRG,
            PermissionCodes.ProyectoGrado.N4PGPSG,
        ];

        public static readonly string[] ProyectoGradoAllStageManagement =
        [
            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
            PermissionCodes.ProyectoGrado.N4PGPPRG,
            PermissionCodes.ProyectoGrado.N4PGPSG,
        ];

        public static readonly string[] PaInscripcionManagement =
        [
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
        ];

        public static readonly string[] PaDesarrolloManagement =
        [
            PermissionCodes.PracticaAcademica.N4PAF1PG,
        ];

        public static readonly string[] PaEvaluacionManagement =
        [
            PermissionCodes.PracticaAcademica.N4PAF2PG,
        ];

        public static readonly string[] PaInscripcionEvaluator =
        [
            PermissionCodes.PracticaAcademica.N4PAF0PR,
        ];

        public static readonly string[] PaDesarrolloEvaluator =
        [
            PermissionCodes.PracticaAcademica.N4PAF1PR,
        ];

        public static readonly string[] PaEvaluacionEvaluator =
        [
            PermissionCodes.PracticaAcademica.N4PAF2PR,
        ];

        public static readonly string[] PaDesarrolloStudent =
        [
            PermissionCodes.PracticaAcademica.N3PAF1R,
        ];

        public static readonly string[] PaEvaluacionStudent =
        [
            PermissionCodes.PracticaAcademica.N3PAF2R,
        ];

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PracticaAcademicaPhaseManagementByStage =
            new ReadOnlyDictionary<string, IReadOnlySet<string>>(
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    [StageModalityCodes.PaFaseInscripcion] = new HashSet<string>(PaInscripcionManagement, StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseDesarrollo] = new HashSet<string>(PaDesarrolloManagement, StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseEvaluacion] = new HashSet<string>(PaEvaluacionManagement, StringComparer.OrdinalIgnoreCase),
                });

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PracticaAcademicaPhaseEvaluatorByStage =
            new ReadOnlyDictionary<string, IReadOnlySet<string>>(
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    [StageModalityCodes.PaFaseInscripcion] = new HashSet<string>(PaInscripcionEvaluator, StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseDesarrollo] = new HashSet<string>(PaDesarrolloEvaluator, StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseEvaluacion] = new HashSet<string>(PaEvaluacionEvaluator, StringComparer.OrdinalIgnoreCase),
                });

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PracticaAcademicaPhaseStudentByStage =
            new ReadOnlyDictionary<string, IReadOnlySet<string>>(
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    [StageModalityCodes.PaFaseInscripcion] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseDesarrollo] = new HashSet<string>(PaDesarrolloStudent, StringComparer.OrdinalIgnoreCase),
                    [StageModalityCodes.PaFaseEvaluacion] = new HashSet<string>(PaEvaluacionStudent, StringComparer.OrdinalIgnoreCase),
                });

        public static readonly string[] PracticaAcademicaInstitutionManagers =
        [
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
        ];

        public static readonly string[] PracticaAcademicaManagement =
        [
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
        ];

        public static readonly string[] PracticaAcademicaAllAssignmentRead =
        [
            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF0PR,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,
        ];

        public static readonly string[] CoTerminalManagementAndReview =
        [
            PermissionCodes.CoTerminal.N3CTSGG,
            PermissionCodes.CoTerminal.N3CTSGR,
        ];

        public static readonly string[] SeminarioManagementAndReview =
        [
            PermissionCodes.Seminario.N3SASGG,
            PermissionCodes.Seminario.N3SASGR,
        ];

        public static readonly string[] PublicacionArticuloManagementAndReview =
        [
            PermissionCodes.PublicacionArticulo.N3PCSGG,
            PermissionCodes.PublicacionArticulo.N3PCSGR,
        ];

        public static readonly string[] GradoPromedioManagementAndReview =
        [
            PermissionCodes.GradoPromedio.N3GPSGG,
            PermissionCodes.GradoPromedio.N3GPSGR,
        ];

        public static readonly string[] SaberProManagementAndReview =
        [
            PermissionCodes.SaberPro.N3SPSGG,
            PermissionCodes.SaberPro.N3SPSGR,
        ];

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> TeachingAssignmentReadByModality =
            new ReadOnlyDictionary<string, IReadOnlySet<string>>(
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    [ModalityCodes.ProyectoGrado] = new HashSet<string>(ProyectoGradoAllRead, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.PracticaAcademica] = new HashSet<string>(PracticaAcademicaAllAssignmentRead, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.CoTerminal] = new HashSet<string>(CoTerminalManagementAndReview, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.SeminarioAct] = new HashSet<string>(SeminarioManagementAndReview, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.PublicacionArticulo] = new HashSet<string>(PublicacionArticuloManagementAndReview, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.GradoPromedio] = new HashSet<string>(GradoPromedioManagementAndReview, StringComparer.OrdinalIgnoreCase),
                    [ModalityCodes.SaberPro] = new HashSet<string>(SaberProManagementAndReview, StringComparer.OrdinalIgnoreCase),
                });

        public static readonly IReadOnlySet<string> AnyModalityTeachingAssignmentRead =
            new HashSet<string>(
                TeachingAssignmentReadByModality.Values.SelectMany(v => v),
                StringComparer.OrdinalIgnoreCase);

        public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ObservationHistoryManagementByEntityType =
            new ReadOnlyDictionary<string, IReadOnlySet<string>>(
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["InscriptionModality"] = new HashSet<string>(InscriptionModalityManagement, StringComparer.OrdinalIgnoreCase),
                    ["Proposal"] = new HashSet<string>(PgPropuestaRead, StringComparer.OrdinalIgnoreCase),
                    ["PreliminaryProject"] = new HashSet<string>(PgAnteproyectoManagement, StringComparer.OrdinalIgnoreCase),
                    ["ProjectFinal"] = new HashSet<string>(PgProyectoManagement, StringComparer.OrdinalIgnoreCase),
                    ["ProjectPresentation"] = new HashSet<string>(PgSustentacionManagement, StringComparer.OrdinalIgnoreCase),
                    ["AcademicPractice"] = new HashSet<string>(PracticaAcademicaManagement, StringComparer.OrdinalIgnoreCase),
                    ["CoTerminal"] = new HashSet<string>([PermissionCodes.CoTerminal.N3CTSGG], StringComparer.OrdinalIgnoreCase),
                    ["Seminar"] = new HashSet<string>([PermissionCodes.Seminario.N3SASGG], StringComparer.OrdinalIgnoreCase),
                    ["ScientificArticle"] = new HashSet<string>([PermissionCodes.PublicacionArticulo.N3PCSGG], StringComparer.OrdinalIgnoreCase),
                    ["AcademicAverage"] = new HashSet<string>([PermissionCodes.GradoPromedio.N3GPSGG], StringComparer.OrdinalIgnoreCase),
                    ["SaberPro"] = new HashSet<string>([PermissionCodes.SaberPro.N3SPSGG], StringComparer.OrdinalIgnoreCase),
                });
    }
}
