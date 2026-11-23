using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Api.Responses;
using Application.Common.Services.Notifications;
using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Notifications
{
    public class SlaAlertSettingsControllerIntegrationTests : IntegrationTestBase
    {
        private static readonly Regex UnresolvedPlaceholderRegex = new(@"\{\w+\}", RegexOptions.Compiled);

        public SlaAlertSettingsControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Theory]
        [InlineData("GET", "/api/SlaAlertSettings")]
        [InlineData("PUT", "/api/SlaAlertSettings")]
        [InlineData("POST", "/api/SlaAlertSettings/run-now")]
        [InlineData("GET", "/api/SlaAlertSettings/logs")]
        public async Task Endpoints_RequireN3CFNOA_Returning401WhenAnonymous_And403WithoutN3CFNOA(string method, string url)
        {
            using var context = GetFreshDbContext();

            var httpMethod = method switch
            {
                "PUT" => HttpMethod.Put,
                "POST" => HttpMethod.Post,
                _ => HttpMethod.Get
            };

            object? body = httpMethod == HttpMethod.Put ? BuildValidUpdateRequest() : null;

            using var anonReq = CreateRequest(httpMethod, url, anonymous: true, body: body);
            var anonRes = await _client.SendAsync(anonReq);
            anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var insufficientPermissions = string.Join(",",
                PermissionCodes.Configuraciones.N2CFP,
                PermissionCodes.Configuraciones.N2CFNO,
                PermissionCodes.ModalidadesGrado.N3MGPG);

            using var forbidReq = CreateRequest(httpMethod, url, permissions: insufficientPermissions, body: body);
            var forbidRes = await _client.SendAsync(forbidReq);
            forbidRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetOverview_WithN3CFNOA_ReturnsSixOrderedRules_SixCcOptions_Modalities_AndZeroUnresolvedPlaceholders()
        {
            using var context = GetFreshDbContext();
            await SeedSlaEmailConfigsAndModalitiesAsync(context);

            using var req = CreateRequest(
                HttpMethod.Get,
                "/api/SlaAlertSettings",
                permissions: PermissionCodes.Configuraciones.N3CFNOA);

            var res = await _client.SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var payload = await res.Content.ReadFromJsonAsync<ApiResponse<SlaAlertSettingsOverviewDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();

            var data = payload.Data!;
            data.Global.IsEnabled.Should().BeTrue();
            data.Global.ScheduleHourColombia.Should().Be(7);
            data.Global.ScheduleMinuteColombia.Should().Be(30);
            data.Global.CronExpression.Should().Be("30 7 * * *");
            data.Global.EvaluatorWarningBusinessDays.Should().Be(20);
            data.Global.EvaluatorOverdueBusinessDays.Should().Be(25);

            data.AvailableCcOptions.Select(o => o.Key).Should().Equal(
                "REL:DIRECTOR_ASSIGNED",
                "REL:CO_DIRECTOR_ASSIGNED",
                "REL:ASESOR_ASSIGNED",
                "ROLE:COMMITTEE_DIRECTOR",
                "ROLE:SECRETARY",
                "ROLE:DEPARTMENT_HEAD");

            data.Rules.Select(r => r.EventName).Should().Equal(
                SlaDeadlineAlertService.AlertEvaluatorApproaching,
                SlaDeadlineAlertService.AlertEvaluatorOverdue,
                SlaDeadlineAlertService.AlertCommitteeOverdue,
                SlaDeadlineAlertService.AlertDefenseReminder,
                SlaDeadlineAlertService.AlertStudentObservations,
                SlaDeadlineAlertService.AlertTermLimitWarning);

            foreach (var rule in data.Rules)
            {
                rule.SampleSubject.Should().NotBeNullOrWhiteSpace();
                rule.SampleHtml.Should().NotBeNullOrWhiteSpace();
                UnresolvedPlaceholderRegex.IsMatch(rule.SampleSubject).Should().BeFalse(
                    $"SampleSubject de {rule.EventName} no debe contener placeholders sin resolver");
                UnresolvedPlaceholderRegex.IsMatch(rule.SampleHtml).Should().BeFalse(
                    $"SampleHtml de {rule.EventName} no debe contener placeholders sin resolver");
            }

            var overdueRule = data.Rules.Single(r => r.EventName == SlaDeadlineAlertService.AlertEvaluatorOverdue);
            overdueRule.SelectedCcKeys.Should().Equal("ROLE:COMMITTEE_DIRECTOR", "ROLE:SECRETARY");

            data.ModalityTermLimits.Should().HaveCount(2);
            data.ModalityTermLimits.Should().Contain(m => m.Code == ModalityCodes.ProyectoGrado && m.MaximumTermPeriods == 3 && m.AllowsExtension);
        }

        [Fact]
        public async Task PutSettings_RejectsInvalidThresholdsAndInvalidCcKeys_With400AndSpanishMessages()
        {
            using var context = GetFreshDbContext();
            await SeedSlaEmailConfigsAndModalitiesAsync(context);

            // 1. evaluatorWarningBusinessDays >= evaluatorOverdueBusinessDays -> 400
            var equalThresholds = BuildValidUpdateRequest();
            equalThresholds.EvaluatorWarningBusinessDays = 25;
            equalThresholds.EvaluatorOverdueBusinessDays = 25;

            using var badRangeReq = CreateRequest(
                HttpMethod.Put,
                "/api/SlaAlertSettings",
                permissions: PermissionCodes.Configuraciones.N3CFNOA,
                body: equalThresholds);

            var badRangeRes = await _client.SendAsync(badRangeReq);
            badRangeRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var badRangePayload = await badRangeRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
            badRangePayload!.Success.Should().BeFalse();
            badRangePayload.Errors.Should().Contain(e => e.Contains("menor", StringComparison.OrdinalIgnoreCase));

            // 2. Invalid CC key -> 400
            var invalidCc = BuildValidUpdateRequest();
            invalidCc.Rules![0].SelectedCcKeys = ["ROLE:STUDENT_HACK"];

            using var badCcReq = CreateRequest(
                HttpMethod.Put,
                "/api/SlaAlertSettings",
                permissions: PermissionCodes.Configuraciones.N3CFNOA,
                body: invalidCc);

            var badCcRes = await _client.SendAsync(badCcReq);
            badCcRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var badCcPayload = await badCcRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
            badCcPayload!.Success.Should().BeFalse();
            badCcPayload.Errors.Should().Contain(e => e.Contains("ROLE:STUDENT_HACK", StringComparison.OrdinalIgnoreCase));

            // 3. Invalid scheduleHourColombia & defenseReminderHours -> 400
            var invalidSchedule = BuildValidUpdateRequest();
            invalidSchedule.ScheduleHourColombia = 24;
            invalidSchedule.DefenseReminderHours = 200;

            using var badSchedReq = CreateRequest(
                HttpMethod.Put,
                "/api/SlaAlertSettings",
                permissions: PermissionCodes.Configuraciones.N3CFNOA,
                body: invalidSchedule);

            var badSchedRes = await _client.SendAsync(badSchedReq);
            badSchedRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PutSettings_UpdatesGlobalThresholds_TogglesActive_ReconcilesCcWithoutTouchingToRules()
        {
            using var context = GetFreshDbContext();
            await SeedSlaEmailConfigsAndModalitiesAsync(context);

            // Snapshot original TO rules before PUT
            List<EmailRecipientRule> originalToRules;
            using (var verifyScope = _factory.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                originalToRules = await db.EmailRecipientRules
                    .AsNoTracking()
                    .Where(r => r.RecipientType == "TO")
                    .OrderBy(r => r.Id)
                    .ToListAsync();
            }
            originalToRules.Should().NotBeEmpty();

            var updateReq = new UpdateSlaAlertSettingsRequest
            {
                IsEnabled = true,
                ScheduleHourColombia = 8,
                ScheduleMinuteColombia = 15,
                WeekdaysOnly = true,
                EvaluatorWarningBusinessDays = 14,
                EvaluatorOverdueBusinessDays = 20,
                CommitteeOverdueBusinessDays = 7,
                StudentObservationsBusinessDays = 10,
                DefenseReminderHours = 36,
                WarnOnLastAllowedPeriod = false,
                Rules =
                [
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertEvaluatorApproaching,
                        IsActive = true,
                        SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED", "REL:ASESOR_ASSIGNED"]
                    },
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertEvaluatorOverdue,
                        IsActive = true,
                        // Remueve ROLE:SECRETARY (soft-disable) y añade ROLE:DEPARTMENT_HEAD
                        SelectedCcKeys = ["ROLE:COMMITTEE_DIRECTOR", "ROLE:DEPARTMENT_HEAD"]
                    },
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertCommitteeOverdue,
                        IsActive = false,
                        SelectedCcKeys = []
                    },
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertDefenseReminder,
                        IsActive = true,
                        SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED"]
                    },
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertStudentObservations,
                        IsActive = true,
                        SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED", "REL:CO_DIRECTOR_ASSIGNED"]
                    },
                    new UpdateSlaAlertRuleRequest
                    {
                        EventName = SlaDeadlineAlertService.AlertTermLimitWarning,
                        IsActive = false,
                        SelectedCcKeys = ["ROLE:SECRETARY"]
                    }
                ]
            };

            using var putHttp = CreateRequest(
                HttpMethod.Put,
                "/api/SlaAlertSettings",
                userId: "1",
                permissions: PermissionCodes.Configuraciones.N3CFNOA,
                body: updateReq);

            var putRes = await _client.SendAsync(putHttp);
            putRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var putPayload = await putRes.Content.ReadFromJsonAsync<ApiResponse<SlaAlertSettingsOverviewDto>>();
            putPayload!.Success.Should().BeTrue();
            var updated = putPayload.Data!;

            updated.Global.ScheduleHourColombia.Should().Be(8);
            updated.Global.ScheduleMinuteColombia.Should().Be(15);
            updated.Global.WeekdaysOnly.Should().BeTrue();
            updated.Global.CronExpression.Should().Be("15 8 * * 1-5");
            updated.Global.EvaluatorWarningBusinessDays.Should().Be(14);
            updated.Global.EvaluatorOverdueBusinessDays.Should().Be(20);
            updated.Global.CommitteeOverdueBusinessDays.Should().Be(7);
            updated.Global.StudentObservationsBusinessDays.Should().Be(10);
            updated.Global.DefenseReminderHours.Should().Be(36);
            updated.Global.WarnOnLastAllowedPeriod.Should().BeFalse();
            updated.Global.UpdatedAtColombia.Should().NotBeNullOrWhiteSpace();

            updated.Rules.Single(r => r.EventName == SlaDeadlineAlertService.AlertCommitteeOverdue).IsActive.Should().BeFalse();
            updated.Rules.Single(r => r.EventName == SlaDeadlineAlertService.AlertTermLimitWarning).IsActive.Should().BeFalse();
            updated.Rules.Single(r => r.EventName == SlaDeadlineAlertService.AlertEvaluatorOverdue)
                .SelectedCcKeys.Should().Equal("ROLE:COMMITTEE_DIRECTOR", "ROLE:DEPARTMENT_HEAD");

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var afterToRules = await db.EmailRecipientRules
                    .AsNoTracking()
                    .Where(r => r.RecipientType == "TO")
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                afterToRules.Should().HaveCount(originalToRules.Count);
                for (var i = 0; i < originalToRules.Count; i++)
                {
                    afterToRules[i].Id.Should().Be(originalToRules[i].Id);
                    afterToRules[i].EmailNotificationConfigId.Should().Be(originalToRules[i].EmailNotificationConfigId);
                    afterToRules[i].RuleType.Should().Be(originalToRules[i].RuleType);
                    afterToRules[i].RuleValue.Should().Be(originalToRules[i].RuleValue);
                    afterToRules[i].Priority.Should().Be(originalToRules[i].Priority);
                    afterToRules[i].StatusRegister.Should().BeTrue();
                }

                // Verificar soft-disable de SECRETARY en SLA_EVALUATOR_OVERDUE
                var overdueCfg = await db.EmailNotificationConfigs.SingleAsync(c => c.EventName == SlaDeadlineAlertService.AlertEvaluatorOverdue);
                var secretaryRule = await db.EmailRecipientRules.SingleAsync(r =>
                    r.EmailNotificationConfigId == overdueCfg.Id && r.RecipientType == "CC" && r.RuleValue == "SECRETARY");
                secretaryRule.StatusRegister.Should().BeFalse();
            }
        }

        [Fact]
        public async Task RunNow_And_GetLogs_RecordMetrics_AndFilterByAlertCodeAndStudentSearch()
        {
            using var context = GetFreshDbContext();
            await SeedSlaEmailConfigsAndModalitiesAsync(context);

            var nowUtc = DateTimeOffset.UtcNow;
            var idType = new IdentificationType { Code = "CC", Name = "Cédula", StatusRegister = true };
            var faculty = new Faculty { Code = "FIT", Name = "Ingenierías", StatusRegister = true };
            context.Set<IdentificationType>().Add(idType);
            context.Set<Faculty>().Add(faculty);
            await context.SaveChangesAsync();

            var program = new AcademicProgram { Code = "SIS", Name = "Sistemas", IdFaculty = faculty.Id, StatusRegister = true };
            context.Set<AcademicProgram>().Add(program);
            await context.SaveChangesAsync();

            var student = new User
            {
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                Identification = "1065998877",
                FirstName = "Mariana",
                LastName = "Quintero",
                Email = "mquintero@unicesar.edu.co",
                StatusRegister = true
            };
            context.Set<User>().Add(student);

            var stateAprob = new StateInscription { Code = StateInscriptionCodes.Aprobado, Name = "Aprobado", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2026-1", StartDate = nowUtc.AddMonths(-2), EndDate = nowUtc.AddMonths(4), StatusRegister = true };
            context.Set<StateInscription>().Add(stateAprob);
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            var pgModality = await context.Set<Modality>().SingleAsync(m => m.Code == ModalityCodes.ProyectoGrado);
            var inscription = new InscriptionModality
            {
                IdModality = pgModality.Id,
                IdStateInscription = stateAprob.Id,
                IdAcademicPeriod = period.Id,
                CreatedAt = nowUtc.AddDays(-45),
                UpdatedAt = nowUtc.AddDays(-45),
                StatusRegister = true
            };
            context.Set<InscriptionModality>().Add(inscription);
            await context.SaveChangesAsync();

            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdInscriptionModality = inscription.Id,
                IdUser = student.Id,
                IsConfirmed = true,
                StatusRegister = true
            });

            // Persistir singleton SlaAlertConfiguration + 2 registros de bitácora para probar run-now + filtros de logs
            context.SlaAlertConfigurations.Add(new SlaAlertConfiguration
            {
                Id = 1,
                IsEnabled = true,
                CreatedAt = nowUtc.AddDays(-1),
                OperationRegister = "SEED",
                StatusRegister = true
            });

            context.EmailAlertDispatchLogs.AddRange(
                new EmailAlertDispatchLog
                {
                    IdInscriptionModality = inscription.Id,
                    AlertCode = SlaDeadlineAlertService.AlertEvaluatorOverdue,
                    EventName = SlaDeadlineAlertService.AlertEvaluatorOverdue,
                    StageStateSnapshot = "PG_FASE_ANTEPROYECTO:AP_EN_EVALUACION:2026-04-01",
                    DeduplicationKey = $"{inscription.Id}|SLA_EVALUATOR_OVERDUE|SNAP1",
                    RecipientsSummary = "jurado@unicesar.edu.co",
                    BusinessDaysElapsed = 27,
                    CreatedAt = nowUtc.AddMinutes(-10),
                    OperationRegister = "SLA_ALERT_DISPATCH",
                    StatusRegister = true
                },
                new EmailAlertDispatchLog
                {
                    IdInscriptionModality = inscription.Id,
                    AlertCode = SlaDeadlineAlertService.AlertStudentObservations,
                    EventName = SlaDeadlineAlertService.AlertStudentObservations,
                    StageStateSnapshot = "PG_FASE_ANTEPROYECTO:AP_CON_OBSERVACIONES:2026-04-10",
                    DeduplicationKey = $"{inscription.Id}|SLA_STUDENT_OBSERVATIONS_REMINDER|SNAP2",
                    RecipientsSummary = "mquintero@unicesar.edu.co",
                    BusinessDaysElapsed = 16,
                    CreatedAt = nowUtc.AddMinutes(-2),
                    OperationRegister = "SLA_ALERT_DISPATCH",
                    StatusRegister = true
                });
            await context.SaveChangesAsync();

            // Ejecutar POST /api/SlaAlertSettings/run-now
            using var runReq = CreateRequest(
                HttpMethod.Post,
                "/api/SlaAlertSettings/run-now",
                permissions: PermissionCodes.Configuraciones.N3CFNOA);
            var runRes = await _client.SendAsync(runReq);
            runRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var runPayload = await runRes.Content.ReadFromJsonAsync<ApiResponse<SlaAlertExecutionSummary>>();
            runPayload!.Success.Should().BeTrue();
            runPayload.Data!.EvaluatedInscriptions.Should().BeGreaterThanOrEqualTo(1);

            // Verificar que GET /api/SlaAlertSettings reporte las métricas de última ejecución
            using var getOverviewReq = CreateRequest(
                HttpMethod.Get,
                "/api/SlaAlertSettings",
                permissions: PermissionCodes.Configuraciones.N3CFNOA);
            var getOverviewRes = await _client.SendAsync(getOverviewReq);
            var overviewPayload = await getOverviewRes.Content.ReadFromJsonAsync<ApiResponse<SlaAlertSettingsOverviewDto>>();
            overviewPayload!.Data!.Global.LastRunAtColombia.Should().NotBeNullOrWhiteSpace();
            overviewPayload.Data.Global.LastRunEvaluatedCount.Should().Be(runPayload.Data.EvaluatedInscriptions);

            // Consultar GET /api/SlaAlertSettings/logs filtrando por estudiante y código de alerta
            using var logsByStudentReq = CreateRequest(
                HttpMethod.Get,
                "/api/SlaAlertSettings/logs?page=1&pageSize=15&search=Mariana",
                permissions: PermissionCodes.Configuraciones.N3CFNOA);
            var logsByStudentRes = await _client.SendAsync(logsByStudentReq);
            logsByStudentRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var logsPayload = await logsByStudentRes.Content.ReadFromJsonAsync<ApiResponse<SlaAlertDispatchLogsPageDto>>();
            logsPayload!.Success.Should().BeTrue();
            logsPayload.Data!.TotalCount.Should().Be(2);
            logsPayload.Data.Items.First().AlertCode.Should().Be(SlaDeadlineAlertService.AlertStudentObservations);
            logsPayload.Data.Items.First().StudentsSummary.Should().Contain("Mariana Quintero");
            logsPayload.Data.Items.First().ModalityName.Should().Be("Proyecto de Grado");

            using var logsByCodeReq = CreateRequest(
                HttpMethod.Get,
                $"/api/SlaAlertSettings/logs?page=1&pageSize=15&alertCode={SlaDeadlineAlertService.AlertEvaluatorOverdue}&search=1065998877",
                permissions: PermissionCodes.Configuraciones.N3CFNOA);
            var logsByCodeRes = await _client.SendAsync(logsByCodeReq);
            var logsByCodePayload = await logsByCodeRes.Content.ReadFromJsonAsync<ApiResponse<SlaAlertDispatchLogsPageDto>>();
            logsByCodePayload!.Data!.TotalCount.Should().Be(1);
            logsByCodePayload.Data.Items.Single().BusinessDaysElapsed.Should().Be(27);
        }

        private static UpdateSlaAlertSettingsRequest BuildValidUpdateRequest() => new()
        {
            IsEnabled = true,
            ScheduleHourColombia = 7,
            ScheduleMinuteColombia = 30,
            WeekdaysOnly = false,
            EvaluatorWarningBusinessDays = 20,
            EvaluatorOverdueBusinessDays = 25,
            CommitteeOverdueBusinessDays = 10,
            StudentObservationsBusinessDays = 15,
            DefenseReminderHours = 48,
            WarnOnLastAllowedPeriod = true,
            Rules =
            [
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertEvaluatorApproaching,
                    IsActive = true,
                    SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED"]
                },
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertEvaluatorOverdue,
                    IsActive = true,
                    SelectedCcKeys = ["ROLE:COMMITTEE_DIRECTOR", "ROLE:SECRETARY"]
                },
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertCommitteeOverdue,
                    IsActive = true,
                    SelectedCcKeys = []
                },
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertDefenseReminder,
                    IsActive = true,
                    SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED", "REL:CO_DIRECTOR_ASSIGNED"]
                },
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertStudentObservations,
                    IsActive = true,
                    SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED"]
                },
                new UpdateSlaAlertRuleRequest
                {
                    EventName = SlaDeadlineAlertService.AlertTermLimitWarning,
                    IsActive = true,
                    SelectedCcKeys = ["REL:DIRECTOR_ASSIGNED", "ROLE:SECRETARY"]
                }
            ]
        };

        private static async Task SeedSlaEmailConfigsAndModalitiesAsync(AppDbContext context)
        {
            context.Set<Modality>().AddRange(
                new Modality
                {
                    Code = ModalityCodes.ProyectoGrado,
                    Name = "Proyecto de Grado",
                    MaximumTermPeriods = 3,
                    AllowsExtension = true,
                    StatusRegister = true,
                    OperationRegister = "TEST_SEED"
                },
                new Modality
                {
                    Code = ModalityCodes.PracticaAcademica,
                    Name = "Práctica Académica",
                    MaximumTermPeriods = 2,
                    AllowsExtension = false,
                    StatusRegister = true,
                    OperationRegister = "TEST_SEED"
                });

            var configs = new (string EventName, string Subject, string Body, (string RecipientType, string RuleType, string RuleValue, int Priority)[] Rules)[]
            {
                (
                    SlaDeadlineAlertService.AlertEvaluatorApproaching,
                    "SIGEPRO · Recordatorio ({RemainingBusinessDays} días): {WorkTitle}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{StudentNames} ({StudentEmails}) - {BusinessDaysElapsed}/{MaxBusinessDays} - {NormativeReference} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE", 1),
                        ("CC", "BY_ENTITY_RELATION", "DIRECTOR_ASSIGNED", 2)
                    ]
                ),
                (
                    SlaDeadlineAlertService.AlertEvaluatorOverdue,
                    "SIGEPRO · Vencido ({BusinessDaysElapsed} días): {WorkTitle}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{ActivitySinceDate} - {MaxBusinessDays} - {NormativeReference} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE", 1),
                        ("CC", "BY_ROLE", "COMMITTEE_DIRECTOR", 2),
                        ("CC", "BY_ROLE", "SECRETARY", 3)
                    ]
                ),
                (
                    SlaDeadlineAlertService.AlertCommitteeOverdue,
                    "SIGEPRO · Comité ({BusinessDaysElapsed} días): {ModalityName} - {StudentNames}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{PendingActionDescription} - {CurrentStageName} - {CurrentStateName} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "COMMITTEE_STAGE_RESPONSIBLE", 1)
                    ]
                ),
                (
                    SlaDeadlineAlertService.AlertDefenseReminder,
                    "SIGEPRO · Sustentación ({ScheduledDateTime}): {WorkTitle}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{PresentationModality} - {LocationOrLink} - #{AttemptNumber} - {JuryNames} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1),
                        ("TO", "BY_ENTITY_RELATION", "JURY_EVALUATOR_ASSIGNED", 2),
                        ("CC", "BY_ENTITY_RELATION", "DIRECTOR_ASSIGNED", 3),
                        ("CC", "BY_ENTITY_RELATION", "CO_DIRECTOR_ASSIGNED", 4)
                    ]
                ),
                (
                    SlaDeadlineAlertService.AlertStudentObservations,
                    "SIGEPRO · Observaciones ({BusinessDaysElapsed} días): {ModalityName}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{StudentNames} - {ActivitySinceDate} - {ObservationsBlock} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1),
                        ("CC", "BY_ENTITY_RELATION", "DIRECTOR_ASSIGNED", 2)
                    ]
                ),
                (
                    SlaDeadlineAlertService.AlertTermLimitWarning,
                    "SIGEPRO · Plazo ({RiskStatusLabel} {PeriodsElapsed}/{MaxTermPeriods}): {ModalityName}",
                    "<div style=\"border-top:4px solid {TopAccentColor};\">{StartAcademicPeriod} - {NormativeGuidance} - <a href=\"{LoginUrl}\">Ingresar</a></div>",
                    [
                        ("TO", "BY_ENTITY_RELATION", "STUDENT_ASSIGNED", 1),
                        ("CC", "BY_ENTITY_RELATION", "DIRECTOR_ASSIGNED", 2)
                    ]
                )
            };

            foreach (var item in configs)
            {
                var entity = new EmailNotificationConfig
                {
                    EventName = item.EventName,
                    SubjectTemplate = item.Subject,
                    BodyTemplate = item.Body,
                    CreatedAt = DateTimeOffset.UtcNow,
                    OperationRegister = "TEST_SEED",
                    StatusRegister = true
                };
                context.EmailNotificationConfigs.Add(entity);
                await context.SaveChangesAsync();

                foreach (var r in item.Rules)
                {
                    context.EmailRecipientRules.Add(new EmailRecipientRule
                    {
                        EmailNotificationConfigId = entity.Id,
                        RecipientType = r.RecipientType,
                        RuleType = r.RuleType,
                        RuleValue = r.RuleValue,
                        Priority = r.Priority,
                        CreatedAt = DateTimeOffset.UtcNow,
                        OperationRegister = "TEST_SEED",
                        StatusRegister = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            string? userId = null,
            string? permissions = null,
            object? body = null,
            bool anonymous = false)
        {
            var request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            if (anonymous)
            {
                request.Headers.Add("X-Test-Anonymous", "true");
                return request;
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                request.Headers.Add("X-Test-UserId", userId);
            }

            if (permissions != null)
            {
                request.Headers.Add("X-Test-Permissions", string.IsNullOrWhiteSpace(permissions) ? "NONE" : permissions);
            }

            return request;
        }
    }
}
