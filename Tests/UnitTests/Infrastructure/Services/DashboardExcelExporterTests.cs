using ClosedXML.Excel;
using Domain.Common.Dashboard;
using FluentAssertions;
using Infrastructure.Services.Reports;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services
{
    public class DashboardExcelExporterTests
    {
        [Fact]
        public void GenerateExcel_Creates_Valid_FourSheet_Workbook_And_Neutralizes_Formulas()
        {
            var exporter = new DashboardExcelExporter();

            var filter = new DashboardFilter
            {
                DateFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
                DateTo = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.FromHours(-5)),
                ModalityCodes = new List<string> { "PROYECTO_GRADO" },
                ActiveTeachersOnly = true,
                StaleDaysThreshold = 30
            };

            var summary = new DashboardSummary
            {
                TotalInscriptions = 10,
                Approved = 8,
                Pending = 1,
                Rejected = 1,
                NotApplicable = 0,
                ApprovalRate = 80.0,
                Completed = 4,
                InProgress = 2,
                InReview = 2,
                WithObservations = 1,
                RejectedOverall = 1,
                CompletionRate = 40.0,
                SuccessRateOverClosed = 80.0,
                AvgCycleDays = 145.5,
                StaleInscriptionsCount = 2,
                UpcomingDefensesCount = 1,
                ByLifecycleStatus = new List<CategoryCount>
                {
                    new("COMPLETED", "Completado", 4),
                    new("IN_PROGRESS", "En curso", 2),
                    new("IN_REVIEW", "En revisión", 2),
                    new("WITH_OBSERVATIONS", "Con observaciones", 1),
                    new("REJECTED", "Rechazado", 1)
                },
                ByModality = new List<CategoryCount> { new("PROYECTO_GRADO", "Proyecto de Grado", 10) },
                ByAcademicPeriod = new List<CategoryCount> { new("2026-1", "2026-1", 10) },
                TeacherLoadDetailed = new List<TeacherLoadDetail>
                {
                    new(100, "=MALICIOUS_TEACHER()", 5, new Dictionary<string, int>
                    {
                        ["Director"] = 3,
                        ["Jurado Evaluador"] = 2
                    })
                }
            };

            var breakdown = new ModalityBreakdown
            {
                ModalityCode = "PROYECTO_GRADO",
                ModalityName = "Proyecto de Grado",
                Total = 10,
                PhaseFunnel = new List<PhasePoint>
                {
                    new(1, "PG_FASE_PROPUESTA", "Propuesta", 2),
                    new(2, "PG_FASE_ANTEPROYECTO", "Anteproyecto", 3),
                    new(4, "PG_FASE_SUSTENTACION", "Sustentación", 5)
                },
                ByStateStage = new List<CategoryCount>
                {
                    new("SUST_APROBADA", "Sustentación Aprobada", 4)
                },
                NumericLabel = "Horas de prueba",
                NumericStats = new NumericSummaryStats(5, 100m, 200m, 190m, 300m, "pts"),
                TopEntitiesLabel = "Top sublíneas de investigación",
                TopEntities = new List<CategoryCount> { new("IA", "Inteligencia Artificial", 6) },
                DefenseSummary = new DefenseMetricsSummary(5, 3, 1, 0, 1, new List<CategoryCount>
                {
                    new("PRESENCIAL", "Presencial", 5)
                })
            };

            var alerts = new DashboardOperationalAlerts
            {
                StaleThresholdDays = 30,
                TotalStaleCount = 1,
                TotalEvaluationSlaBreachCount = 1,
                TotalTermRiskCount = 1,
                StaleItems = new List<StaleInscriptionAlert>
                {
                    new(77, "PROYECTO_GRADO", "Proyecto de Grado", "+Student Formula", "Anteproyecto",
                        "En evaluación", "IN_REVIEW", "En revisión", 42, "2026-02-01 09:00", true, "Jurados / Evaluadores")
                },
                TermRiskItems = new List<TermRiskAlert>
                {
                    new(88, "PROYECTO_GRADO", "Proyecto de Grado", "@Risk Student", "2024-1",
                        4, 3, "EXCEEDED", "Proyecto Final", "Pendiente informe")
                },
                UpcomingDefenses = new List<UpcomingDefenseAlert>
                {
                    new(99, "-Title Attack", "Ana Pérez", "2026-12-01 10:00", "Presencial", "Auditorio A", 1, "Dr. Jurado")
                },
                ReworkByModality = new List<ReworkModalityMetric>
                {
                    new("PROYECTO_GRADO", "Proyecto de Grado", 10, 6, 0.6, 4)
                }
            };

            var rows = new List<DashboardExportRow>
            {
                new(
                    InscriptionId: 10,
                    ModalityName: "Proyecto de Grado",
                    StateName: "Aprobado",
                    AcademicPeriod: "2026-1",
                    Program: "Ingeniería de Sistemas",
                    Faculty: "Ingenierías y Tecnológicas",
                    Students: "=HYPERLINK(\"http://evil\",\"Click\")",
                    CreatedAt: "2026-01-15 08:30",
                    ApprovalDate: "2026-01-20",
                    CurrentPhase: "Sustentación",
                    PhaseState: "Sustentación Aprobada",
                    ModalityDetail: "+CMD|' /C calc'!A0",
                    ModalityCode: "PROYECTO_GRADO",
                    LifecycleCode: "COMPLETED",
                    LifecycleStatus: "Completado",
                    StudentIdentifications: "1065000111",
                    StudentEmails: "ana@unicesar.edu.co",
                    Teachers: "Carlos Ruiz (Director)",
                    Title: "@SUM(A1:A9)",
                    ResearchLine: "IA",
                    ResearchSubLine: "NLP",
                    LastActivityAt: "2026-06-01 16:00",
                    DaysElapsed: 137)
            };

            var options = new DashboardFilterOptions
            {
                Modalities = new List<FilterOption> { new("PROYECTO_GRADO", "Proyecto de Grado") }
            };

            // Act
            var bytes = exporter.GenerateExcel(filter, summary, breakdown, alerts, rows, options);

            // Assert
            bytes.Should().NotBeNullOrEmpty();

            using var wb = new XLWorkbook(new MemoryStream(bytes));
            wb.Worksheets.Select(w => w.Name).Should().ContainInOrder(
                "Resumen Ejecutivo",
                "Registros Detallados",
                "Carga Docente",
                "Alertas y Sustentaciones");

            // Hoja 1: Resumen Ejecutivo
            var ws1 = wb.Worksheet("Resumen Ejecutivo");
            ws1.Cell(1, 1).GetString().Should().Be("SIGEPRO — Reporte Ejecutivo de Modalidades de Grado");
            ws1.CellsUsed().Select(c => c.GetString()).Should().Contain(s => s.Contains("Proyecto de Grado"));
            ws1.CellsUsed().Select(c => c.GetString()).Should().Contain(s => s.Contains("Sustentaciones Programadas"));

            // Hoja 2: Registros Detallados + neutralización de fórmulas
            var ws2 = wb.Worksheet("Registros Detallados");
            ws2.Cell(1, 1).GetString().Should().Be("Id");
            ws2.Cell(2, 1).GetValue<int>().Should().Be(10);
            ws2.Cell(2, 9).GetString().Should().Be("'=HYPERLINK(\"http://evil\",\"Click\")");
            ws2.Cell(2, 9).HasFormula.Should().BeFalse();
            ws2.Cell(2, 13).GetString().Should().Be("'@SUM(A1:A9)");
            ws2.Cell(2, 18).GetString().Should().Be("'+CMD|' /C calc'!A0");
            ws2.Cell(2, 17).GetString().Should().Be("Sustentación Aprobada");

            // Hoja 3: Carga Docente + neutralización de fórmulas
            var ws3 = wb.Worksheet("Carga Docente");
            ws3.Cell(1, 1).GetString().Should().Contain("Solo Inscripciones Activas");
            ws3.Cell(3, 1).GetString().Should().Be("'=MALICIOUS_TEACHER()");
            ws3.Cell(3, 1).HasFormula.Should().BeFalse();
            ws3.CellsUsed().Select(c => c.GetString()).Should().Contain("TOTAL");

            // Hoja 4: Alertas y Sustentaciones + SLA + neutralización de fórmulas
            var ws4 = wb.Worksheet("Alertas y Sustentaciones");
            var ws4Values = ws4.CellsUsed().Select(c => c.GetString()).ToList();
            ws4Values.Should().Contain("'+Student Formula");
            ws4Values.Should().Contain("INCUMPLE (>25 días háb.)");
            ws4Values.Should().Contain("'@Risk Student");
            ws4Values.Should().Contain("EXCEDIDO");
            ws4Values.Should().Contain("'-Title Attack");
            ws4Values.Should().Contain("Dr. Jurado");
        }
    }
}
