using System.Globalization;
using ClosedXML.Excel;
using Domain.Common.Dashboard;
using Domain.Interfaces.Services;

namespace Infrastructure.Services.Reports
{
    /// <summary>
    /// Genera el libro Excel (.xlsx) institucional multipestaña para el dashboard PEGI con ClosedXML.
    /// Aplica formato institucional (#006633), congelamiento de encabezados, filtros automáticos
    /// y neutralización de inyección de fórmulas en todas las celdas de texto.
    /// </summary>
    public class DashboardExcelExporter : IDashboardExcelExporter
    {
        private static readonly XLColor InstitutionalGreen = XLColor.FromHtml("#006633");
        private static readonly XLColor SoftGreenBg = XLColor.FromHtml("#E8F5E9");
        private static readonly XLColor ZebraFill = XLColor.FromHtml("#F8FAFC");
        private static readonly XLColor BorderColor = XLColor.FromHtml("#CBD5E1");
        private static readonly XLColor AlertRedBg = XLColor.FromHtml("#FEE2E2");
        private static readonly XLColor AlertRedText = XLColor.FromHtml("#991B1B");
        private static readonly XLColor AlertAmberBg = XLColor.FromHtml("#FEF3C7");
        private static readonly XLColor AlertAmberText = XLColor.FromHtml("#92400E");

        private const double MaxColumnWidth = 55.0;
        private const double MinColumnWidth = 10.0;

        public byte[] GenerateExcel(
            DashboardFilter filter,
            DashboardSummary summary,
            ModalityBreakdown? breakdown,
            DashboardOperationalAlerts alerts,
            List<DashboardExportRow> rows,
            DashboardFilterOptions options)
        {
            using var workbook = new XLWorkbook();

            BuildExecutiveSummarySheet(workbook, filter, summary, breakdown, options);
            BuildDetailedRecordsSheet(workbook, rows);
            BuildTeacherLoadSheet(workbook, filter, summary);
            BuildAlertsAndDefensesSheet(workbook, alerts);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void BuildExecutiveSummarySheet(
            XLWorkbook workbook,
            DashboardFilter filter,
            DashboardSummary summary,
            ModalityBreakdown? breakdown,
            DashboardFilterOptions options)
        {
            var ws = workbook.Worksheets.Add("Resumen Ejecutivo");
            var row = 1;

            // Banner institucional
            ws.Range(row, 1, row, 5).Merge();
            var banner = ws.Cell(row, 1);
            SetSafeText(banner, "SIGEPRO — Reporte Ejecutivo de Modalidades de Grado");
            banner.Style.Fill.BackgroundColor = InstitutionalGreen;
            banner.Style.Font.FontColor = XLColor.White;
            banner.Style.Font.Bold = true;
            banner.Style.Font.FontSize = 14;
            banner.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(row).Height = 30;
            row++;

            // Fecha generación Colombia
            ws.Range(row, 1, row, 5).Merge();
            SetSafeText(ws.Cell(row, 1), $"Generado: {DashboardTimeHelper.FormatDateTime(DateTimeOffset.UtcNow)} (Hora Colombia UTC-5)");
            ws.Cell(row, 1).Style.Font.Italic = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
            row++;

            // Resumen legible de filtros aplicados
            ws.Range(row, 1, row, 5).Merge();
            SetSafeText(ws.Cell(row, 1), $"Filtros aplicados: {DescribeFilters(filter, options)}");
            ws.Cell(row, 1).Style.Fill.BackgroundColor = SoftGreenBg;
            ws.Cell(row, 1).Style.Font.FontSize = 10;
            ws.Cell(row, 1).Style.Alignment.WrapText = true;
            ws.Row(row).Height = 24;
            row += 2;

            // Tabla de KPIs
            row = WriteSectionHeader(ws, row, "Indicadores Clave (KPIs)", 3);
            WriteTableHeaders(ws, row, "Indicador", "Valor", "Observación");
            row++;

            var kpiRows = new (string Name, object Value, string Note, string? NumberFormat)[]
            {
                ("Total Inscripciones", summary.TotalInscriptions, "Registros activos filtrados", "#,##0"),
                ("Completadas (Aprobación Final)", summary.Completed, "Estado final de modalidad", "#,##0"),
                ("Tasa de Completitud (%)", summary.CompletionRate / 100.0, "Completadas / Total", "0.0%"),
                ("Tasa de Éxito sobre Cerradas (%)", summary.SuccessRateOverClosed.HasValue ? summary.SuccessRateOverClosed.Value / 100.0 : (object)"N/A", "Completadas / (Completadas + Rechazadas)", summary.SuccessRateOverClosed.HasValue ? "0.0%" : null),
                ("En Curso", summary.InProgress, "Trámites activos en ejecución", "#,##0"),
                ("En Revisión / Evaluación", summary.InReview, "Pendientes de comité o jurados", "#,##0"),
                ("Con Observaciones / Correcciones", summary.WithObservations, "Requieren ajustes o reintento", "#,##0"),
                ("Rechazadas / Reprobadas", summary.RejectedOverall, "Rechazo de inscripción o etapa final", "#,##0"),
                ("Tiempo Promedio de Ciclo (días)", summary.AvgCycleDays.HasValue ? summary.AvgCycleDays.Value : (object)"N/A", "Inscripciones completadas", summary.AvgCycleDays.HasValue ? "0.0" : null),
                ($"Inscripciones Estancadas (> {filter.StaleDaysThreshold} días)", summary.StaleInscriptionsCount, "Activas sin movimiento reciente", "#,##0"),
                ("Sustentaciones Próximas", summary.UpcomingDefensesCount, "Programadas a futuro", "#,##0"),
                ("Aval Inscripción — Aprobadas", summary.Approved, "Estado inscripción APROBADO", "#,##0"),
                ("Aval Inscripción — Pendientes", summary.Pending, "Estado inscripción PENDIENTE", "#,##0"),
                ("Aval Inscripción — Automático (No requiere aval)", summary.NotApplicable, "Estado inscripción NO_APLICA", "#,##0"),
                ("Tasa de Aprobación de Inscripción (%)", summary.ApprovalRate / 100.0, "Aprobadas / (Total - No requiere aval)", "0.0%")
            };

            for (var i = 0; i < kpiRows.Length; i++)
            {
                var k = kpiRows[i];
                SetSafeText(ws.Cell(row, 1), k.Name);
                SetCellValue(ws.Cell(row, 2), k.Value, k.NumberFormat);
                SetSafeText(ws.Cell(row, 3), k.Note);
                StyleDataRow(ws, row, 1, 3, i % 2 == 1);
                row++;
            }

            row++;

            // Distribución por Estado de Ciclo de Vida
            row = WriteCategoryTable(ws, row, "Distribución por Ciclo de Vida", "Estado de Ciclo de Vida", summary.ByLifecycleStatus, summary.TotalInscriptions);
            row++;

            // Distribución por Modalidad
            row = WriteCategoryTable(ws, row, "Distribución por Modalidad", "Modalidad", summary.ByModality, summary.TotalInscriptions);
            row++;

            // Distribución por Periodo Académico
            row = WriteCategoryTable(ws, row, "Distribución por Periodo Académico", "Periodo", summary.ByAcademicPeriod, summary.TotalInscriptions);

            // Sección opcional de Drill-Down por Modalidad
            if (breakdown != null)
            {
                row++;
                row = WriteSectionHeader(ws, row, $"Detalle de Modalidad: {breakdown.ModalityName} (Total: {breakdown.Total})", 3);

                if (breakdown.PhaseFunnel.Count > 0)
                {
                    WriteTableHeaders(ws, row, "Orden", "Fase", "Cantidad");
                    row++;
                    for (var i = 0; i < breakdown.PhaseFunnel.Count; i++)
                    {
                        var ph = breakdown.PhaseFunnel[i];
                        ws.Cell(row, 1).SetValue(ph.StageOrder);
                        SetSafeText(ws.Cell(row, 2), ph.StageName);
                        ws.Cell(row, 3).SetValue(ph.Count);
                        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        StyleDataRow(ws, row, 1, 3, i % 2 == 1);
                        row++;
                    }
                    row++;
                }

                if (breakdown.ByStateStage.Count > 0)
                {
                    row = WriteCategoryTable(ws, row, "Estados de Fase Activa", "Estado de Fase", breakdown.ByStateStage, breakdown.Total);
                    row++;
                }

                if (breakdown.NumericStats != null)
                {
                    var ns = breakdown.NumericStats;
                    var unitSuffix = string.IsNullOrWhiteSpace(ns.Unit) ? "" : $" ({ns.Unit})";
                    row = WriteSectionHeader(ws, row, $"Estadísticas Numéricas — {breakdown.NumericLabel}{unitSuffix}", 5);
                    WriteTableHeaders(ws, row, "Conteo", "Mínimo", "Promedio", "Mediana", "Máximo");
                    row++;
                    ws.Cell(row, 1).SetValue(ns.Count);
                    ws.Cell(row, 2).SetValue((double)ns.Min);
                    ws.Cell(row, 3).SetValue((double)ns.Avg);
                    ws.Cell(row, 4).SetValue((double)ns.Median);
                    ws.Cell(row, 5).SetValue((double)ns.Max);
                    ws.Cell(row, 1).Style.NumberFormat.Format = "#,##0";
                    for (var c = 2; c <= 5; c++)
                        ws.Cell(row, c).Style.NumberFormat.Format = "0.00";
                    StyleDataRow(ws, row, 1, 5, false);
                    row += 2;
                }

                if (breakdown.TopEntities.Count > 0)
                {
                    row = WriteCategoryTable(
                        ws,
                        row,
                        breakdown.TopEntitiesLabel ?? "Top Entidades",
                        "Entidad / Categoría",
                        breakdown.TopEntities,
                        breakdown.Total);
                    row++;
                }

                if (breakdown.DefenseSummary != null)
                {
                    var ds = breakdown.DefenseSummary;
                    row = WriteSectionHeader(ws, row, "Resumen de Sustentaciones (Proyecto de Grado)", 2);
                    WriteTableHeaders(ws, row, "Métrica", "Cantidad");
                    row++;
                    var defenseMetrics = new (string Label, int Val)[]
                    {
                        ("Sustentaciones Programadas", ds.TotalScheduled),
                        ("Aprobadas en 1er Intento", ds.ApprovedFirstAttempt),
                        ("Aprobadas en 2do Intento", ds.ApprovedSecondAttempt),
                        ("No Aprobadas (Reintento Pendiente)", ds.RetryPending),
                        ("Reprobadas Definitivas", ds.RejectedFinal)
                    };
                    for (var i = 0; i < defenseMetrics.Length; i++)
                    {
                        SetSafeText(ws.Cell(row, 1), defenseMetrics[i].Label);
                        ws.Cell(row, 2).SetValue(defenseMetrics[i].Val);
                        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                        StyleDataRow(ws, row, 1, 2, i % 2 == 1);
                        row++;
                    }
                }
            }

            ws.SheetView.FreezeRows(1);
            SafeAdjustColumns(ws, 5);
        }

        private static void BuildDetailedRecordsSheet(XLWorkbook workbook, List<DashboardExportRow> rows)
        {
            var ws = workbook.Worksheets.Add("Registros Detallados");

            var headers = new[]
            {
                "Id", "Código Modalidad", "Modalidad", "Estado Inscripción", "Estado Ciclo Vida",
                "Periodo", "Programa", "Facultad", "Estudiantes", "Identificaciones",
                "Correos", "Docentes", "Título", "Línea Investigación", "Sublínea Investigación",
                "Fase Actual", "Estado Fase", "Detalle Modalidad", "Fecha Creación (CO)",
                "Fecha Aprobación (CO)", "Última Actividad (CO)", "Días Transcurridos"
            };

            WriteTableHeaders(ws, 1, headers);

            var r = 2;
            for (var i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                ws.Cell(r, 1).SetValue(item.InscriptionId);
                SetSafeText(ws.Cell(r, 2), item.ModalityCode);
                SetSafeText(ws.Cell(r, 3), item.ModalityName);
                SetSafeText(ws.Cell(r, 4), item.StateName);
                SetSafeText(ws.Cell(r, 5), item.LifecycleStatus);
                SetSafeText(ws.Cell(r, 6), item.AcademicPeriod);
                SetSafeText(ws.Cell(r, 7), item.Program);
                SetSafeText(ws.Cell(r, 8), item.Faculty);
                SetSafeText(ws.Cell(r, 9), item.Students);
                SetSafeText(ws.Cell(r, 10), item.StudentIdentifications);
                SetSafeText(ws.Cell(r, 11), item.StudentEmails);
                SetSafeText(ws.Cell(r, 12), item.Teachers);
                SetSafeText(ws.Cell(r, 13), item.Title);
                SetSafeText(ws.Cell(r, 14), item.ResearchLine);
                SetSafeText(ws.Cell(r, 15), item.ResearchSubLine);
                SetSafeText(ws.Cell(r, 16), item.CurrentPhase);
                SetSafeText(ws.Cell(r, 17), item.PhaseState);
                SetSafeText(ws.Cell(r, 18), item.ModalityDetail);
                SetSafeText(ws.Cell(r, 19), DashboardTimeHelper.FormatColombiaFromIsoOrRaw(item.CreatedAt, includeTime: true));
                SetSafeText(ws.Cell(r, 20), DashboardTimeHelper.FormatColombiaFromIsoOrRaw(item.ApprovalDate, includeTime: false));
                SetSafeText(ws.Cell(r, 21), DashboardTimeHelper.FormatColombiaFromIsoOrRaw(item.LastActivityAt, includeTime: true));
                ws.Cell(r, 22).SetValue(item.DaysElapsed);
                ws.Cell(r, 22).Style.NumberFormat.Format = "#,##0";

                StyleDataRow(ws, r, 1, headers.Length, i % 2 == 1);
                r++;
            }

            ws.SheetView.FreezeRows(1);
            var lastRow = Math.Max(1, r - 1);
            ws.Range(1, 1, lastRow, headers.Length).SetAutoFilter();
            SafeAdjustColumns(ws, headers.Length);
        }

        private static void BuildTeacherLoadSheet(XLWorkbook workbook, DashboardFilter filter, DashboardSummary summary)
        {
            var ws = workbook.Worksheets.Add("Carga Docente");

            var defaultRoles = new[] { "Director", "Co-director", "Asesor", "Jurado Evaluador" };
            var extraRoles = summary.TeacherLoadDetailed
                .SelectMany(t => t.ByRole.Keys)
                .Where(k => !defaultRoles.Contains(k, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k)
                .ToList();

            var roleColumns = summary.TeacherLoadDetailed.Count > 0
                ? defaultRoles
                    .Where(dr => summary.TeacherLoadDetailed.Any(t => t.ByRole.Keys.Any(k => string.Equals(k, dr, StringComparison.OrdinalIgnoreCase))))
                    .Concat(extraRoles)
                    .ToList()
                : defaultRoles.ToList();

            if (roleColumns.Count == 0)
                roleColumns = defaultRoles.ToList();

            var totalCols = 2 + roleColumns.Count;

            ws.Range(1, 1, 1, totalCols).Merge();
            var modeLabel = filter.ActiveTeachersOnly
                ? "Carga Docente — Solo Inscripciones Activas (excluye Completadas y Rechazadas)"
                : "Carga Docente — Histórico Completo (incluye todas las inscripciones)";
            SetSafeText(ws.Cell(1, 1), modeLabel);
            ws.Cell(1, 1).Style.Fill.BackgroundColor = SoftGreenBg;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontColor = InstitutionalGreen;
            ws.Row(1).Height = 24;

            var headers = new List<string> { "Docente" };
            headers.AddRange(roleColumns);
            headers.Add("Total");
            WriteTableHeaders(ws, 2, headers.ToArray());

            var r = 3;
            var roleTotals = new int[roleColumns.Count];
            var grandTotal = 0;

            for (var i = 0; i < summary.TeacherLoadDetailed.Count; i++)
            {
                var t = summary.TeacherLoadDetailed[i];
                SetSafeText(ws.Cell(r, 1), t.TeacherName);

                for (var c = 0; c < roleColumns.Count; c++)
                {
                    var roleKey = t.ByRole.Keys.FirstOrDefault(k => string.Equals(k, roleColumns[c], StringComparison.OrdinalIgnoreCase));
                    var count = roleKey != null ? t.ByRole[roleKey] : 0;
                    roleTotals[c] += count;
                    ws.Cell(r, 2 + c).SetValue(count);
                    ws.Cell(r, 2 + c).Style.NumberFormat.Format = "#,##0";
                }

                grandTotal += t.Total;
                ws.Cell(r, totalCols).SetValue(t.Total);
                ws.Cell(r, totalCols).Style.Font.Bold = true;
                ws.Cell(r, totalCols).Style.NumberFormat.Format = "#,##0";

                StyleDataRow(ws, r, 1, totalCols, i % 2 == 1);
                r++;
            }

            if (summary.TeacherLoadDetailed.Count > 0)
            {
                SetSafeText(ws.Cell(r, 1), "TOTAL");
                for (var c = 0; c < roleColumns.Count; c++)
                {
                    ws.Cell(r, 2 + c).SetValue(roleTotals[c]);
                    ws.Cell(r, 2 + c).Style.NumberFormat.Format = "#,##0";
                }
                ws.Cell(r, totalCols).SetValue(grandTotal);
                ws.Cell(r, totalCols).Style.NumberFormat.Format = "#,##0";
                var totalRange = ws.Range(r, 1, r, totalCols);
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.BackgroundColor = SoftGreenBg;
                totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.OutsideBorderColor = BorderColor;
            }

            ws.SheetView.FreezeRows(2);
            var filterLastRow = Math.Max(2, r - 1);
            ws.Range(2, 1, filterLastRow, totalCols).SetAutoFilter();
            SafeAdjustColumns(ws, totalCols);
        }

        private static void BuildAlertsAndDefensesSheet(XLWorkbook workbook, DashboardOperationalAlerts alerts)
        {
            var ws = workbook.Worksheets.Add("Alertas y Sustentaciones");
            var row = 1;

            // Sección 1: Estancamiento y SLA Acuerdo 015
            row = WriteSectionHeader(
                ws,
                row,
                $"Inscripciones Estancadas (> {alerts.StaleThresholdDays} días) — Total: {alerts.TotalStaleCount} | Incumplimientos SLA Acuerdo 015: {alerts.TotalEvaluationSlaBreachCount}",
                10);
            WriteTableHeaders(ws, row,
                "Id", "Modalidad", "Estudiantes", "Fase Actual", "Estado Fase",
                "Ciclo de Vida", "Días Sin Actividad", "Última Actividad (CO)", "Responsable", "SLA Acuerdo 015");
            row++;

            if (alerts.StaleItems.Count == 0)
            {
                SetSafeText(ws.Cell(row, 1), "Sin inscripciones estancadas bajo los filtros actuales.");
                ws.Range(row, 1, row, 10).Merge();
                row += 2;
            }
            else
            {
                var slaDays = alerts.EvaluatorOverdueBusinessDays > 0 ? alerts.EvaluatorOverdueBusinessDays : 25;
                for (var i = 0; i < alerts.StaleItems.Count; i++)
                {
                    var item = alerts.StaleItems[i];
                    ws.Cell(row, 1).SetValue(item.InscriptionId);
                    SetSafeText(ws.Cell(row, 2), item.ModalityName);
                    SetSafeText(ws.Cell(row, 3), item.Students);
                    SetSafeText(ws.Cell(row, 4), item.CurrentPhase);
                    SetSafeText(ws.Cell(row, 5), item.PhaseState);
                    SetSafeText(ws.Cell(row, 6), item.LifecycleStatus);
                    ws.Cell(row, 7).SetValue(item.DaysSinceActivity);
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                    SetSafeText(ws.Cell(row, 8), DashboardTimeHelper.FormatColombiaFromIsoOrRaw(item.LastActivityAt, includeTime: true));
                    SetSafeText(ws.Cell(row, 9), item.ResponsibleParty);
                    SetSafeText(ws.Cell(row, 10), item.SlaBreachAcuerdo015 ? $"INCUMPLE (>{slaDays} días háb.)" : "En plazo");

                    StyleDataRow(ws, row, 1, 10, i % 2 == 1);
                    if (item.SlaBreachAcuerdo015)
                    {
                        ws.Cell(row, 10).Style.Fill.BackgroundColor = AlertRedBg;
                        ws.Cell(row, 10).Style.Font.FontColor = AlertRedText;
                        ws.Cell(row, 10).Style.Font.Bold = true;
                    }
                    row++;
                }
                row++;
            }

            // Sección 2: Riesgo de vencimiento de plazo
            row = WriteSectionHeader(ws, row, $"Riesgo de Vencimiento de Plazo (MaxTermPeriods) — Total: {alerts.TotalTermRiskCount}", 9);
            WriteTableHeaders(ws, row,
                "Id", "Modalidad", "Estudiantes", "Periodo Inicio", "Periodos Transcurridos",
                "Plazo Máximo", "Nivel de Riesgo", "Fase Actual", "Estado Fase");
            row++;

            if (alerts.TermRiskItems.Count == 0)
            {
                SetSafeText(ws.Cell(row, 1), "Sin inscripciones en riesgo de vencimiento de plazo.");
                ws.Range(row, 1, row, 9).Merge();
                row += 2;
            }
            else
            {
                for (var i = 0; i < alerts.TermRiskItems.Count; i++)
                {
                    var item = alerts.TermRiskItems[i];
                    ws.Cell(row, 1).SetValue(item.InscriptionId);
                    SetSafeText(ws.Cell(row, 2), item.ModalityName);
                    SetSafeText(ws.Cell(row, 3), item.Students);
                    SetSafeText(ws.Cell(row, 4), item.StartPeriod);
                    ws.Cell(row, 5).SetValue(item.PeriodsElapsed);
                    ws.Cell(row, 6).SetValue(item.MaxTermPeriods);
                    SetSafeText(ws.Cell(row, 7), item.RiskLevel == "EXCEEDED" ? "EXCEDIDO" : "ALERTA");
                    SetSafeText(ws.Cell(row, 8), item.CurrentPhase);
                    SetSafeText(ws.Cell(row, 9), item.PhaseState);

                    StyleDataRow(ws, row, 1, 9, i % 2 == 1);
                    ws.Cell(row, 7).Style.Fill.BackgroundColor = item.RiskLevel == "EXCEEDED" ? AlertRedBg : AlertAmberBg;
                    ws.Cell(row, 7).Style.Font.FontColor = item.RiskLevel == "EXCEEDED" ? AlertRedText : AlertAmberText;
                    ws.Cell(row, 7).Style.Font.Bold = true;
                    row++;
                }
                row++;
            }

            // Sección 3: Próximas sustentaciones
            row = WriteSectionHeader(ws, row, $"Próximas Sustentaciones Programadas ({alerts.UpcomingDefenses.Count})", 8);
            WriteTableHeaders(ws, row,
                "Id", "Título", "Estudiantes", "Fecha y Hora (CO)", "Modalidad", "Lugar / Enlace", "Intento", "Jurados");
            row++;

            if (alerts.UpcomingDefenses.Count == 0)
            {
                SetSafeText(ws.Cell(row, 1), "No hay sustentaciones programadas a futuro.");
                ws.Range(row, 1, row, 8).Merge();
                row += 2;
            }
            else
            {
                for (var i = 0; i < alerts.UpcomingDefenses.Count; i++)
                {
                    var item = alerts.UpcomingDefenses[i];
                    ws.Cell(row, 1).SetValue(item.InscriptionId);
                    SetSafeText(ws.Cell(row, 2), item.Title);
                    SetSafeText(ws.Cell(row, 3), item.Students);
                    SetSafeText(ws.Cell(row, 4), DashboardTimeHelper.FormatColombiaFromIsoOrRaw(item.ScheduledDateTime, includeTime: true));
                    SetSafeText(ws.Cell(row, 5), item.PresentationModality);
                    SetSafeText(ws.Cell(row, 6), item.LocationOrLink);
                    ws.Cell(row, 7).SetValue(item.AttemptNumber);
                    SetSafeText(ws.Cell(row, 8), item.Juries);
                    StyleDataRow(ws, row, 1, 8, i % 2 == 1);
                    row++;
                }
                row++;
            }

            // Sección 4: Retrabajo por modalidad
            row = WriteSectionHeader(ws, row, "Retrabajo por Modalidad (Observaciones y Documentos Reemplazados)", 5);
            WriteTableHeaders(ws, row,
                "Modalidad", "Total Inscripciones", "Evaluaciones con Obs.", "Prom. Obs. / Inscripción", "Documentos Reemplazados");
            row++;

            for (var i = 0; i < alerts.ReworkByModality.Count; i++)
            {
                var item = alerts.ReworkByModality[i];
                SetSafeText(ws.Cell(row, 1), item.ModalityName);
                ws.Cell(row, 2).SetValue(item.TotalInscriptions);
                ws.Cell(row, 3).SetValue(item.ObservationEvaluations);
                ws.Cell(row, 4).SetValue(item.AvgObservationsPerInscription);
                ws.Cell(row, 5).SetValue(item.ReplacedDocumentsCount);
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                StyleDataRow(ws, row, 1, 5, i % 2 == 1);
                row++;
            }

            ws.SheetView.FreezeRows(2);
            SafeAdjustColumns(ws, 10);
        }

        private static int WriteCategoryTable(
            IXLWorksheet ws,
            int startRow,
            string sectionTitle,
            string categoryHeader,
            List<CategoryCount> items,
            int total)
        {
            var row = WriteSectionHeader(ws, startRow, sectionTitle, 3);
            WriteTableHeaders(ws, row, categoryHeader, "Cantidad", "Participación (%)");
            row++;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                SetSafeText(ws.Cell(row, 1), item.Label);
                ws.Cell(row, 2).SetValue(item.Count);
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 3).SetValue(total > 0 ? (double)item.Count / total : 0.0);
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
                StyleDataRow(ws, row, 1, 3, i % 2 == 1);
                row++;
            }

            return row;
        }

        private static int WriteSectionHeader(IXLWorksheet ws, int row, string title, int spanCols)
        {
            ws.Range(row, 1, row, spanCols).Merge();
            var cell = ws.Cell(row, 1);
            SetSafeText(cell, title);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Font.FontColor = InstitutionalGreen;
            cell.Style.Fill.BackgroundColor = SoftGreenBg;
            ws.Row(row).Height = 22;
            return row + 1;
        }

        private static void WriteTableHeaders(IXLWorksheet ws, int row, params string[] headers)
        {
            for (var col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                SetSafeText(cell, headers[col]);
                cell.Style.Fill.BackgroundColor = InstitutionalGreen;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = BorderColor;
            }
            ws.Row(row).Height = 22;
        }

        private static void StyleDataRow(IXLWorksheet ws, int row, int firstCol, int lastCol, bool isZebra)
        {
            var range = ws.Range(row, firstCol, row, lastCol);
            if (isZebra)
                range.Style.Fill.BackgroundColor = ZebraFill;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorderColor = BorderColor;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = BorderColor;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void SetCellValue(IXLCell cell, object value, string? numberFormat)
        {
            switch (value)
            {
                case int i:
                    cell.SetValue(i);
                    break;
                case double d:
                    cell.SetValue(d);
                    break;
                case decimal m:
                    cell.SetValue((double)m);
                    break;
                default:
                    SetSafeText(cell, value?.ToString());
                    break;
            }

            if (!string.IsNullOrEmpty(numberFormat))
                cell.Style.NumberFormat.Format = numberFormat;
        }

        /// <summary>
        /// Neutraliza inyección de fórmulas de hojas de cálculo (DDE/CSV/Excel) prefijando con apóstrofe.
        /// </summary>
        public static string SanitizeText(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
                ? "'" + value
                : value;
        }

        private static void SetSafeText(IXLCell cell, string? value)
        {
            var sanitized = SanitizeText(value);
            // ClosedXML interpreta un único apóstrofe inicial como bandera IncludeQuotePrefix y lo remueve
            // del texto almacenado; duplicar el prefijo en la asignación preserva tanto IncludeQuotePrefix=true
            // como el carácter literal "'" en el valor de celda (protege también ante re-exportación a CSV).
            if (sanitized.StartsWith('\''))
            {
                cell.SetValue("'" + sanitized);
                cell.Style.IncludeQuotePrefix = true;
            }
            else
            {
                cell.SetValue(sanitized);
            }
        }

        private static void SafeAdjustColumns(IXLWorksheet ws, int columnCount)
        {
            try
            {
                ws.Columns(1, columnCount).AdjustToContents();
                for (var col = 1; col <= columnCount; col++)
                {
                    var column = ws.Column(col);
                    if (column.Width > MaxColumnWidth)
                        column.Width = MaxColumnWidth;
                    else if (column.Width < MinColumnWidth)
                        column.Width = MinColumnWidth;
                }
            }
            catch
            {
                // Respaldo seguro para contenedores Linux mínimos sin fuentes del sistema instaladas.
                for (var col = 1; col <= columnCount; col++)
                {
                    var maxLen = ws.Column(col).CellsUsed()
                        .Select(c => c.GetString().Length)
                        .DefaultIfEmpty(12)
                        .Max();
                    ws.Column(col).Width = Math.Clamp(maxLen + 4, MinColumnWidth, MaxColumnWidth);
                }
            }
        }

        private static string DescribeFilters(DashboardFilter filter, DashboardFilterOptions options)
        {
            var parts = new List<string>();

            if (filter.DateFrom.HasValue || filter.DateTo.HasValue)
            {
                var from = filter.DateFrom.HasValue ? DashboardTimeHelper.FormatDate(filter.DateFrom) : "...";
                var to = filter.DateTo.HasValue ? DashboardTimeHelper.FormatDate(filter.DateTo) : "...";
                parts.Add($"Fechas: {from} a {to}");
            }

            if (filter.ModalityCodes is { Count: > 0 })
            {
                var names = filter.ModalityCodes.Select(code =>
                    options.Modalities.FirstOrDefault(m => m.Value == code)?.Label ?? code);
                parts.Add($"Modalidades: {string.Join(", ", names)}");
            }

            if (filter.StateCodes is { Count: > 0 })
            {
                var names = filter.StateCodes.Select(code =>
                    options.States.FirstOrDefault(s => s.Value == code)?.Label ?? code);
                parts.Add($"Estado Inscripción: {string.Join(", ", names)}");
            }

            if (filter.LifecycleStatuses is { Count: > 0 })
                parts.Add($"Ciclo de Vida: {string.Join(", ", filter.LifecycleStatuses)}");

            if (filter.AcademicPeriodIds is { Count: > 0 })
            {
                var names = filter.AcademicPeriodIds.Select(id =>
                    options.AcademicPeriods.FirstOrDefault(p => p.Value == id.ToString(CultureInfo.InvariantCulture))?.Label ?? id.ToString(CultureInfo.InvariantCulture));
                parts.Add($"Periodos: {string.Join(", ", names)}");
            }

            if (filter.AcademicProgramIds is { Count: > 0 })
            {
                var names = filter.AcademicProgramIds.Select(id =>
                    options.Programs.FirstOrDefault(p => p.Value == id.ToString(CultureInfo.InvariantCulture))?.Label ?? id.ToString(CultureInfo.InvariantCulture));
                parts.Add($"Programas: {string.Join(", ", names)}");
            }

            if (filter.FacultyIds is { Count: > 0 })
            {
                var names = filter.FacultyIds.Select(id =>
                    options.Faculties.FirstOrDefault(f => f.Value == id.ToString(CultureInfo.InvariantCulture))?.Label ?? id.ToString(CultureInfo.InvariantCulture));
                parts.Add($"Facultades: {string.Join(", ", names)}");
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
                parts.Add($"Búsqueda: \"{filter.Search.Trim()}\"");

            parts.Add(filter.ActiveTeachersOnly ? "Carga docente: Solo activas" : "Carga docente: Histórico");
            parts.Add($"Umbral estancamiento: {filter.StaleDaysThreshold} días");

            return parts.Count > 0 ? string.Join(" | ", parts) : "Sin filtros específicos (Vista global)";
        }
    }
}
