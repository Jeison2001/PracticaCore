using System.Net;
using Api.Authorization;
using Api.Controllers;
using Api.Responses;
using Application.Common.Services.Notifications;
using Domain.Common.Dashboard;
using Domain.Common.Notifications;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Notifications;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Notifications
{
    public class SlaDeadlineAlertServiceTests
    {
        private sealed class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset _utcNow;

            public ManualTimeProvider(DateTimeOffset initialUtcNow) => _utcNow = initialUtcNow;

            public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"sla-alerts-{Guid.NewGuid():N}")
                .Options;
            return new AppDbContext(options);
        }

        /// <summary>
        /// Retrocede exactamente N días hábiles desde la fecha actual en zona horaria Colombia.
        /// </summary>
        private static DateTimeOffset SubtractBusinessDaysColombia(DateTimeOffset fromUtc, int businessDays)
        {
            var cursor = fromUtc;
            var remaining = businessDays;
            while (remaining > 0)
            {
                cursor = cursor.AddDays(-1);
                var colombiaDow = TimeZoneInfo.ConvertTime(cursor, DashboardTimeHelper.ColombiaTimeZone).DayOfWeek;
                if (colombiaDow != DayOfWeek.Saturday && colombiaDow != DayOfWeek.Sunday)
                {
                    remaining--;
                }
            }
            return cursor;
        }

        private static void SeedBaseCatalog(AppDbContext ctx, DateTimeOffset nowUtc)
        {
            var faculty = new Faculty { Id = 1, Code = "FIT", Name = "Ingenierías y Tecnológicas", StatusRegister = true };
            var program = new AcademicProgram { Id = 1, Code = "SIS", Name = "Ingeniería de Sistemas", IdFaculty = 1, Faculty = faculty, StatusRegister = true };
            var idType = new IdentificationType { Id = 1, Code = "CC", Name = "Cédula", StatusRegister = true };

            ctx.Set<Faculty>().Add(faculty);
            ctx.Set<AcademicProgram>().Add(program);
            ctx.Set<IdentificationType>().Add(idType);

            ctx.Set<StateInscription>().AddRange(
                new StateInscription { Id = 1, Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true },
                new StateInscription { Id = 2, Code = StateInscriptionCodes.Aprobado, Name = "Aprobado", StatusRegister = true },
                new StateInscription { Id = 3, Code = StateInscriptionCodes.Rechazado, Name = "Rechazado", IsRejectionState = true, StatusRegister = true },
                new StateInscription { Id = 4, Code = StateInscriptionCodes.NoAplica, Name = "No Aplica", StatusRegister = true });

            ctx.Set<Modality>().AddRange(
                new Modality { Id = 1, Code = ModalityCodes.ProyectoGrado, Name = "Proyecto de Grado", MaximumTermPeriods = 3, StatusRegister = true },
                new Modality { Id = 2, Code = ModalityCodes.PracticaAcademica, Name = "Práctica Académica", MaximumTermPeriods = 2, StatusRegister = true },
                new Modality { Id = 3, Code = ModalityCodes.CoTerminal, Name = "Co-Terminal", MaximumTermPeriods = 2, StatusRegister = true },
                new Modality { Id = 4, Code = ModalityCodes.SaberPro, Name = "Saber Pro", MaximumTermPeriods = null, StatusRegister = true });

            ctx.Set<StageModality>().AddRange(
                new StageModality { Id = 1, IdModality = 1, StageOrder = 1, Code = StageModalityCodes.PgFasePropuesta, Name = "Propuesta", StatusRegister = true },
                new StageModality { Id = 2, IdModality = 1, StageOrder = 2, Code = StageModalityCodes.PgFaseAnteproyecto, Name = "Anteproyecto", StatusRegister = true },
                new StageModality { Id = 3, IdModality = 1, StageOrder = 3, Code = StageModalityCodes.PgFaseProyecto, Name = "Proyecto Final", StatusRegister = true },
                new StageModality { Id = 4, IdModality = 1, StageOrder = 4, Code = StageModalityCodes.PgFaseSustentacion, Name = "Sustentación", StatusRegister = true },
                new StageModality { Id = 5, IdModality = 2, StageOrder = 3, Code = StageModalityCodes.PaFaseEvaluacion, Name = "Evaluación Práctica", StatusRegister = true },
                new StageModality { Id = 6, IdModality = 3, StageOrder = 1, Code = "CT_FASE_UNICA", Name = "Evaluación Co-Terminal", StatusRegister = true },
                new StageModality { Id = 7, IdModality = 4, StageOrder = 1, Code = "SP_FASE_UNICA", Name = "Verificación Saber Pro", StatusRegister = true });

            ctx.Set<StateStage>().AddRange(
                new StateStage { Id = 1, IdStageModality = 1, Code = StateStageCodes.PropRadicada, Name = "Propuesta Radicada", StatusRegister = true },
                new StateStage { Id = 2, IdStageModality = 1, Code = StateStageCodes.PropNoPertinente, Name = "Propuesta No Pertinente", StatusRegister = true },
                new StateStage { Id = 3, IdStageModality = 2, Code = StateStageCodes.ApRadicadoPendAsigEval, Name = "Anteproyecto Radicado Pendiente Jurados", StatusRegister = true },
                new StateStage { Id = 4, IdStageModality = 2, Code = StateStageCodes.ApEnEvaluacion, Name = "Anteproyecto en Evaluación", StatusRegister = true },
                new StateStage { Id = 5, IdStageModality = 2, Code = StateStageCodes.ApConObservaciones, Name = "Anteproyecto con Observaciones", StatusRegister = true },
                new StateStage { Id = 6, IdStageModality = 3, Code = StateStageCodes.PfinfRadicadoEnEvaluacion, Name = "Informe Final en Evaluación", StatusRegister = true },
                new StateStage { Id = 7, IdStageModality = 4, Code = StateStageCodes.SustPendienteProgramacion, Name = "Pendiente de Programación", StatusRegister = true },
                new StateStage { Id = 8, IdStageModality = 4, Code = StateStageCodes.SustProgramada, Name = "Sustentación Programada", StatusRegister = true },
                new StateStage { Id = 9, IdStageModality = 4, Code = StateStageCodes.SustNoAprobadaReintento, Name = "No Aprobada (Reintento)", StatusRegister = true },
                new StateStage { Id = 10, IdStageModality = 4, Code = StateStageCodes.SustAprobada, Name = "Sustentación Aprobada", IsFinalStateForModalityOverall = true, StatusRegister = true },
                new StateStage { Id = 11, IdStageModality = 5, Code = StateStageCodes.PaInformeFinalEnRevision, Name = "Informe Final Práctica en Revisión", StatusRegister = true },
                new StateStage { Id = 12, IdStageModality = 6, Code = "CT_RADICADO", Name = "Co-Terminal Radicado", StatusRegister = true },
                new StateStage { Id = 13, IdStageModality = 7, Code = "SP_RADICADO", Name = "Saber Pro Radicado", StatusRegister = true },
                new StateStage { Id = 14, IdStageModality = 1, Code = StateStageCodes.PropPertinente, Name = "Propuesta Pertinente", StatusRegister = true });

            // 3 periodos académicos activos para probar 1/2, 2/2 y 3/2
            ctx.Set<AcademicPeriod>().AddRange(
                new AcademicPeriod { Id = 1, Code = "2025-1", StartDate = nowUtc.AddMonths(-13), EndDate = nowUtc.AddMonths(-8), StatusRegister = true },
                new AcademicPeriod { Id = 2, Code = "2025-2", StartDate = nowUtc.AddMonths(-7), EndDate = nowUtc.AddMonths(-2), StatusRegister = true },
                new AcademicPeriod { Id = 3, Code = "2026-1", StartDate = nowUtc.AddMonths(-1), EndDate = nowUtc.AddMonths(4), StatusRegister = true });

            ctx.Set<TypeTeachingAssignment>().AddRange(
                new TypeTeachingAssignment { Id = 1, Code = "DIRECTOR", Name = "Director", StatusRegister = true },
                new TypeTeachingAssignment { Id = 2, Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", StatusRegister = true },
                new TypeTeachingAssignment { Id = 3, Code = "EVALUADOR", Name = "Evaluador", StatusRegister = true });

            ctx.Set<User>().AddRange(
                new User { Id = 1, IdIdentificationType = 1, Identification = "106501", Email = "estudiante1@unicesar.edu.co", FirstName = "Valeria", LastName = "Mendoza", IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 2, IdIdentificationType = 1, Identification = "106502", Email = "jurado1@unicesar.edu.co", FirstName = "Carlos", LastName = "Ríos", IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 3, IdIdentificationType = 1, Identification = "106503", Email = "comite.pg@unicesar.edu.co", FirstName = "Elena", LastName = "Vargas", IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 4, IdIdentificationType = 1, Identification = "106504", Email = "director.comite@unicesar.edu.co", FirstName = "Jorge", LastName = "Pineda", IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 5, IdIdentificationType = 1, Identification = "106505", Email = "secretaria.comite@unicesar.edu.co", FirstName = "Laura", LastName = "Suárez", IdAcademicProgram = 1, StatusRegister = true });

            SeedAllSixAlertEmailConfigs(ctx);
            ctx.SaveChanges();
        }

        private static void SeedAllSixAlertEmailConfigs(AppDbContext ctx)
        {
            var eventsAndRules = new (int Id, string EventName, string Subject, string Body, string RuleType, string RuleValue)[]
            {
                (101, SlaDeadlineAlertService.AlertEvaluatorApproaching,
                    "Aviso SLA: {WorkTitle} ({RemainingBusinessDays} días restantes)",
                    "Color:{TopAccentColor}|Días:{BusinessDaysElapsed}/{MaxBusinessDays}|Norma:{NormativeReference}",
                    "BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE"),
                (102, SlaDeadlineAlertService.AlertEvaluatorOverdue,
                    "Vencimiento SLA: {WorkTitle} ({BusinessDaysElapsed} días hábiles)",
                    "Color:{TopAccentColor}|Desde:{ActivitySinceDate}|Norma:{NormativeReference}",
                    "BY_ENTITY_RELATION", "STAGE_EVALUATOR_RESPONSIBLE"),
                (103, SlaDeadlineAlertService.AlertCommitteeOverdue,
                    "Pendiente Comité: {ModalityName} - {WorkTitle}",
                    "Acción:{PendingActionDescription}|Días:{BusinessDaysElapsed}|Etapa:{CurrentStageName}|{CurrentStateName}",
                    "BY_ENTITY_RELATION", "COMMITTEE_STAGE_RESPONSIBLE"),
                (104, SlaDeadlineAlertService.AlertDefenseReminder,
                    "Sustentación próxima: {WorkTitle}",
                    "Fecha:{ScheduledDateTime}|Mod:{PresentationModality}|Lugar:{LocationOrLink}|Intento:{AttemptNumber}|Jurados:{JuryNames}",
                    "EVENT_PARTICIPANT", "STUDENT"),
                (105, SlaDeadlineAlertService.AlertStudentObservations,
                    "Observaciones pendientes: {WorkTitle}",
                    "Días:{BusinessDaysElapsed}|{ObservationsBlock}",
                    "EVENT_PARTICIPANT", "STUDENT"),
                (106, SlaDeadlineAlertService.AlertTermLimitWarning,
                    "Alerta plazo reglamentario: {RiskStatusLabel} ({ModalityName})",
                    "Periodo:{StartAcademicPeriod}|Transcurridos:{PeriodsElapsed}/{MaxTermPeriods}|Guía:{NormativeGuidance}",
                    "EVENT_PARTICIPANT", "STUDENT")
            };

            var ruleId = 500;
            foreach (var item in eventsAndRules)
            {
                ctx.Set<EmailNotificationConfig>().Add(new EmailNotificationConfig
                {
                    Id = item.Id,
                    EventName = item.EventName,
                    SubjectTemplate = item.Subject,
                    BodyTemplate = item.Body,
                    OperationRegister = "TEST_SEED",
                    StatusRegister = true
                });

                ctx.Set<EmailRecipientRule>().Add(new EmailRecipientRule
                {
                    Id = ruleId++,
                    EmailNotificationConfigId = item.Id,
                    RecipientType = "TO",
                    RuleType = item.RuleType,
                    RuleValue = item.RuleValue,
                    Priority = 1,
                    OperationRegister = "TEST_SEED",
                    StatusRegister = true
                });
            }
        }

        private static (SlaDeadlineAlertService Service, List<EmailNotification> SentEmails) BuildFullPipeline(
            AppDbContext ctx,
            TimeProvider timeProvider,
            Dictionary<string, string?>? extraConfig = null,
            Func<EmailNotification, Task<bool>>? sendOverride = null)
        {
            var sentEmails = new List<EmailNotification>();
            var notificationMock = new Mock<INotificationService>();
            notificationMock
                .Setup(n => n.SendEmailAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
                .Returns<EmailNotification, CancellationToken>(async (email, _) =>
                {
                    if (sendOverride != null)
                    {
                        var ok = await sendOverride(email);
                        if (ok) sentEmails.Add(email);
                        return ok;
                    }
                    sentEmails.Add(email);
                    return true;
                });

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(extraConfig ?? new Dictionary<string, string?>())
                .Build();

            var uow = new UnitOfWork(ctx, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);
            var eventService = new EmailNotificationEventService(
                uow,
                resolver,
                notificationMock.Object,
                NullLogger<EmailNotificationEventService>.Instance,
                config);
            var slaRepo = new SlaAlertRepository(ctx, NullLogger<SlaAlertRepository>.Instance);
            var slaService = new SlaDeadlineAlertService(
                slaRepo,
                eventService,
                NullLogger<SlaDeadlineAlertService>.Instance,
                config,
                timeProvider);

            return (slaService, sentEmails);
        }

        [Fact]
        public void CountBusinessDaysElapsed_SkipsWeekends_And_RespectsColombiaTimezone()
        {
            // Viernes 13 de marzo 2026 10:00 COT (15:00 UTC) -> Domingo 15 de marzo 2026 22:00 COT (Lunes 16 03:00 UTC)
            // En Colombia sigue siendo domingo 15 => 0 días hábiles.
            var friMorningUtc = new DateTimeOffset(2026, 3, 13, 15, 0, 0, TimeSpan.Zero);
            var sunNightColombiaUtc = new DateTimeOffset(2026, 3, 16, 3, 0, 0, TimeSpan.Zero);
            DashboardTimeHelper.CountBusinessDaysElapsed(friMorningUtc, sunNightColombiaUtc).Should().Be(0);

            // Viernes 13 de marzo 2026 -> Lunes 16 de marzo 2026 08:00 COT (13:00 UTC) => 1 día hábil (lunes).
            var monMorningUtc = new DateTimeOffset(2026, 3, 16, 13, 0, 0, TimeSpan.Zero);
            DashboardTimeHelper.CountBusinessDaysElapsed(friMorningUtc, monMorningUtc).Should().Be(1);

            // 4 semanas exactas (28 días calendario) = 20 días hábiles exactos.
            var fourWeeksLaterUtc = friMorningUtc.AddDays(28);
            DashboardTimeHelper.CountBusinessDaysElapsed(friMorningUtc, fourWeeksLaterUtc).Should().Be(20);

            // 5 semanas exactas (35 días calendario) = 25 días hábiles exactos (Acuerdo 015 Arts. 15 y 24).
            var fiveWeeksLaterUtc = friMorningUtc.AddDays(35);
            DashboardTimeHelper.CountBusinessDaysElapsed(friMorningUtc, fiveWeeksLaterUtc).Should().Be(25);

            // Fecha fin anterior o igual => 0.
            DashboardTimeHelper.CountBusinessDaysElapsed(monMorningUtc, friMorningUtc).Should().Be(0);
        }

        [Fact]
        public async Task EvaluatorSla_FiresApproachingAt20To24BusinessDays_And_OverdueAt25BusinessDays()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 14, 0, 0, TimeSpan.Zero); // Miércoles 09:00 COT
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            // Inscripción 1: 19 días hábiles -> NO dispara alerta
            var t19 = SubtractBusinessDaysColombia(nowUtc, 19);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 10, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t19, UpdatedAt = t19, StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 10, Title = "PG En Plazo", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, CreatedAt = t19, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 10, IdStateStage = 4, CreatedAt = t19, UpdatedAt = t19, StatusRegister = true });

            // Inscripción 2: 22 días hábiles -> SLA_EVALUATOR_APPROACHING (faltan 3 días hábiles)
            var t22 = SubtractBusinessDaysColombia(nowUtc, 22);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 11, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t22, UpdatedAt = t22, StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 11, Title = "PG Próximo Vencer", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, CreatedAt = t22, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 11, IdStateStage = 4, CreatedAt = t22, UpdatedAt = t22, StatusRegister = true });

            // Inscripción 3: 26 días hábiles en Práctica Académica -> SLA_EVALUATOR_OVERDUE (Art. 24)
            var t26 = SubtractBusinessDaysColombia(nowUtc, 26);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 12, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 5,
                CreatedAt = t26, UpdatedAt = t26, StatusRegister = true
            });
            ctx.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = 12, Title = "Práctica Hospitalaria", IdStateStage = 11, CreatedAt = t26, UpdatedAt = t26, StatusRegister = true
            });

            foreach (var insId in new[] { 10, 11, 12 })
            {
                ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    Id = insId, IdInscriptionModality = insId, IdUser = 1, IsConfirmed = true, StatusRegister = true
                });
                ctx.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    Id = insId, IdInscriptionModality = insId, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true
                });
            }
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.EvaluatedInscriptions.Should().Be(3);
            summary.AlertsTriggered.Should().Be(2);
            summary.AlertsFailed.Should().Be(0);
            sentEmails.Should().HaveCount(2);

            var approachingMail = sentEmails.Single(e => e.Subject.Contains("PG Próximo Vencer"));
            approachingMail.ToAddresses.Should().Contain("jurado1@unicesar.edu.co");
            approachingMail.Subject.Should().Contain("(3 días restantes)");
            WebUtility.HtmlDecode(approachingMail.Body).Should().Contain("#D97706").And.Contain("22/25").And.Contain("Artículo 15");

            var overdueMail = sentEmails.Single(e => e.Subject.Contains("Práctica Hospitalaria"));
            overdueMail.ToAddresses.Should().Contain("jurado1@unicesar.edu.co");
            WebUtility.HtmlDecode(overdueMail.Body).Should().Contain("#DC2626").And.Contain("Artículo 24");
        }

        [Fact]
        public async Task CommitteeOverdue_FiresAt10BusinessDays_ForPendingInscriptionAndMinorModalities()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 14, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            // Permisos N3MGPR y N3SPSGR asignados a comite.pg@unicesar.edu.co (User 3)
            ctx.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = PermissionCodes.ModalidadesGrado.N3MGPR, Description = "Revisar Inscripción", StatusRegister = true },
                new Permission { Id = 2, Code = PermissionCodes.SaberPro.N3SPSGR, Description = "Revisar Saber Pro", StatusRegister = true });
            ctx.Set<UserPermission>().AddRange(
                new UserPermission { Id = 1, IdUser = 3, IdPermission = 1, StatusRegister = true },
                new UserPermission { Id = 2, IdUser = 3, IdPermission = 2, StatusRegister = true });

            var t11 = SubtractBusinessDaysColombia(nowUtc, 11);

            // Inscripción 20: StateInscription == PENDIENTE
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 20, IdModality = 1, IdStateInscription = 1, IdAcademicPeriod = 3,
                CreatedAt = t11, UpdatedAt = t11, StatusRegister = true
            });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 20, IdInscriptionModality = 20, IdUser = 1, IsConfirmed = true, StatusRegister = true });

            // Inscripción 21: SABER_PRO en SP_RADICADO con 11 días hábiles
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 21, IdModality = 4, IdStateInscription = 4, IdAcademicPeriod = 3, IdStageModality = 7,
                CreatedAt = t11, UpdatedAt = t11, StatusRegister = true
            });
            ctx.Set<SaberPro>().Add(new SaberPro { Id = 21, IdStateStage = 13, ResultScore = 195, CreatedAt = t11, UpdatedAt = t11, StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 21, IdInscriptionModality = 21, IdUser = 1, IsConfirmed = true, StatusRegister = true });

            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.AlertsTriggered.Should().Be(2);
            sentEmails.Should().HaveCount(2);
            sentEmails.Should().OnlyContain(e => e.ToAddresses.Contains("comite.pg@unicesar.edu.co"));
            sentEmails.Select(e => WebUtility.HtmlDecode(e.Body)).Should().Contain(b => b.Contains("Revisión de aval inicial de inscripción"));
            sentEmails.Select(e => WebUtility.HtmlDecode(e.Body)).Should().Contain(b => b.Contains("Saber Pro"));
        }

        [Fact]
        public async Task DefenseReminder_FiresOnlyWithin48HoursFutureWindow()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            void SeedDefense(int id, string title, DateTimeOffset scheduledUtc)
            {
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 4,
                    CreatedAt = nowUtc.AddDays(-2), UpdatedAt = nowUtc.AddDays(-2), StatusRegister = true
                });
                ctx.Set<Proposal>().Add(new Proposal { Id = id, Title = title, IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, StatusRegister = true });
                ctx.Set<ProjectPresentation>().Add(new ProjectPresentation
                {
                    Id = id,
                    IdStateStage = 8, // SUST_PROGRAMADA
                    ScheduledDateTime = scheduledUtc,
                    PresentationModality = "PRESENCIAL",
                    LocationOrLink = "Auditorio Bloque D",
                    AttemptNumber = 1,
                    CreatedAt = nowUtc.AddDays(-2),
                    StatusRegister = true
                });
                ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = id, IdInscriptionModality = id, IdUser = 1, IsConfirmed = true, StatusRegister = true });
                ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = id, IdInscriptionModality = id, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true });
            }

            SeedDefense(30, "Defensa En 24h", nowUtc.AddHours(24));   // Debe disparar
            SeedDefense(31, "Defensa En 72h", nowUtc.AddHours(72));   // Fuera de ventana (>48h)
            SeedDefense(32, "Defensa Pasada", nowUtc.AddHours(-2));   // En el pasado
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.AlertsTriggered.Should().Be(1);
            sentEmails.Should().ContainSingle();
            var mail = sentEmails[0];
            mail.Subject.Should().Contain("Defensa En 24h");
            mail.ToAddresses.Should().Contain("estudiante1@unicesar.edu.co");
            WebUtility.HtmlDecode(mail.Body).Should().Contain("Presencial")
                .And.Contain("Auditorio Bloque D")
                .And.Contain("Carlos Ríos");
        }

        [Fact]
        public async Task StudentObservationsReminder_FiresAt15BusinessDays_WithFormattedObservationsBlock()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            var t16 = SubtractBusinessDaysColombia(nowUtc, 16);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 40, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t16, UpdatedAt = t16, StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 40, Title = "Red Neuronal Agrícola", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject
            {
                Id = 40,
                IdStateStage = 5, // AP_CON_OBSERVACIONES
                Observations = "Ajustar marco metodológico y población objetivo.",
                CreatedAt = t16,
                UpdatedAt = t16,
                StatusRegister = true
            });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 40, IdInscriptionModality = 40, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.AlertsTriggered.Should().Be(1);
            sentEmails.Should().ContainSingle();
            sentEmails[0].ToAddresses.Should().Contain("estudiante1@unicesar.edu.co");
            sentEmails[0].Body.Should().Contain("<table role=\"presentation\"")
                .And.Contain("Ajustar marco metodol&#243;gico y poblaci&#243;n objetivo.");
        }

        [Fact]
        public async Task TermLimitWarning_FiresAt2Of2Warning_And_3Of2Exceeded_And_NotAt1Of2()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            // Modalidad 3 (Co-Terminal) tiene MaximumTermPeriods = 2.
            // Usamos actividad reciente (hace 1 día) para aislar solo la alerta de plazo por periodos.
            var recent = nowUtc.AddDays(-1);

            void SeedCoTerminal(int id, int academicPeriodId, string programName)
            {
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 3, IdStateInscription = 2, IdAcademicPeriod = academicPeriodId, IdStageModality = 6,
                    CreatedAt = recent, UpdatedAt = recent, StatusRegister = true
                });
                ctx.Set<CoTerminal>().Add(new CoTerminal
                {
                    Id = id, IdStateStage = 12, PostgraduateProgramName = programName, CreatedAt = recent, UpdatedAt = recent, StatusRegister = true
                });
                ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = id, IdInscriptionModality = id, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            }

            SeedCoTerminal(50, academicPeriodId: 3, "Maestría 1 de 2"); // 2026-1 -> 1/2 (No alerta)
            SeedCoTerminal(51, academicPeriodId: 2, "Maestría 2 de 2"); // 2025-2 -> 2/2 (WARNING)
            SeedCoTerminal(52, academicPeriodId: 1, "Maestría 3 de 2"); // 2025-1 -> 3/2 (EXCEEDED)
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.EvaluatedInscriptions.Should().Be(3);
            summary.AlertsTriggered.Should().Be(2);
            summary.DispatchedKeys.Should().Contain("51|SLA_TERM_LIMIT_WARNING|WARNING:2_OF_2");
            summary.DispatchedKeys.Should().Contain("52|SLA_TERM_LIMIT_WARNING|EXCEEDED:3_OF_2");

            sentEmails.Should().HaveCount(2);
            sentEmails.Should().Contain(e => e.Subject.Contains("Último periodo académico permitido") && e.Body.Contains("2/2"));
            sentEmails.Should().Contain(e => e.Subject.Contains("Plazo reglamentario excedido") && e.Body.Contains("3/2"));
        }

        [Fact]
        public async Task Idempotency_SecondRunSkipsAlreadySent_WhileAdvancingActivityOrThresholdTriggersNewAlert()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            var t21 = SubtractBusinessDaysColombia(nowUtc, 21);
            var inscription = new InscriptionModality
            {
                Id = 60, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t21, UpdatedAt = t21, StatusRegister = true
            };
            var prelim = new PreliminaryProject
            {
                Id = 60, IdStateStage = 4, CreatedAt = t21, UpdatedAt = t21, StatusRegister = true
            };
            ctx.Set<InscriptionModality>().Add(inscription);
            ctx.Set<Proposal>().Add(new Proposal { Id = 60, Title = "Tesis Idempotente", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(prelim);
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 60, IdInscriptionModality = 60, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = 60, IdInscriptionModality = 60, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true });
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);

            // 1ra ejecución: envía SLA_EVALUATOR_APPROACHING
            var run1 = await service.ProcessDueAlertsAsync();
            run1.AlertsTriggered.Should().Be(1);
            run1.AlertsSkippedAlreadySent.Should().Be(0);
            sentEmails.Should().HaveCount(1);

            // 2da ejecución inmediata: 0 correos duplicados
            var run2 = await service.ProcessDueAlertsAsync();
            run2.AlertsTriggered.Should().Be(0);
            run2.AlertsSkippedAlreadySent.Should().Be(1);
            sentEmails.Should().HaveCount(1);

            // Avanzar reloj +5 días hábiles (llega a 26 días hábiles -> SLA_EVALUATOR_OVERDUE)
            clock.SetUtcNow(nowUtc.AddDays(7));
            var run3 = await service.ProcessDueAlertsAsync();
            run3.AlertsTriggered.Should().Be(1);
            run3.DispatchedKeys.Single().Should().Contain("SLA_EVALUATOR_OVERDUE");
            sentEmails.Should().HaveCount(2);

            // Si avanza la actividad de la fase a un nuevo ciclo (ej. re-radicación hace 21 días hábiles respecto a futuro)
            var futureNow = nowUtc.AddDays(45);
            var newCycleStart = SubtractBusinessDaysColombia(futureNow, 21);
            prelim.UpdatedAt = newCycleStart;
            inscription.UpdatedAt = newCycleStart;
            await ctx.SaveChangesAsync();

            clock.SetUtcNow(futureNow);
            var run4 = await service.ProcessDueAlertsAsync();
            run4.AlertsTriggered.Should().Be(1);
            sentEmails.Should().HaveCount(3);
        }

        [Fact]
        public async Task RecipientResolver_ResolvesEvaluatorFallback_And_CommitteeRoleFallback()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            // Configurar roles COMMITTEE_DIRECTOR y SECRETARY para fallback cuando nadie tiene el permiso específico
            ctx.Set<Role>().AddRange(
                new Role { Id = 10, Code = "COMMITTEE_DIRECTOR", Name = "Director Comité", StatusRegister = true },
                new Role { Id = 11, Code = "SECRETARY", Name = "Secretaría", StatusRegister = true });
            ctx.Set<UserRole>().AddRange(
                new UserRole { Id = 10, IdUser = 4, IdRole = 10, StatusRegister = true },
                new UserRole { Id = 11, IdUser = 5, IdRole = 11, StatusRegister = true });

            // Permiso N4PGPAA asignado a User 3 para probar FallbackPermissionCode en STAGE_EVALUATOR_RESPONSIBLE
            ctx.Set<Permission>().Add(new Permission { Id = 10, Code = PermissionCodes.ProyectoGrado.N4PGPAA, Description = "Anteproyecto Comité", StatusRegister = true });
            ctx.Set<UserPermission>().Add(new UserPermission { Id = 10, IdUser = 3, IdPermission = 10, StatusRegister = true });

            // Inscripción 70 en Proyecto de Grado (Sustentación) SIN ningún usuario con N4PGPSG y SIN jurados asignados
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 70, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 4, StatusRegister = true
            });
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);

            // Caso A: STAGE_EVALUATOR_RESPONSIBLE sin jurados asignados -> usa FallbackPermissionCode (N4PGPAA -> User 3)
            var evaluatorRules = new List<EmailRecipientRule>
            {
                new() { Id = 1, RecipientType = "TO", RuleType = "BY_ENTITY_RELATION", RuleValue = "STAGE_EVALUATOR_RESPONSIBLE", Priority = 1, StatusRegister = true }
            };
            var evaluatorResult = await resolver.ResolveRecipientsAsync(evaluatorRules, new Dictionary<string, object>
            {
                ["InscriptionId"] = 70,
                ["FallbackPermissionCode"] = PermissionCodes.ProyectoGrado.N4PGPAA
            });
            evaluatorResult.To.Should().Equal("comite.pg@unicesar.edu.co");

            // Caso B: COMMITTEE_STAGE_RESPONSIBLE sin usuarios con N4PGPSG -> cae a roles COMMITTEE_DIRECTOR y SECRETARY
            var committeeRules = new List<EmailRecipientRule>
            {
                new() { Id = 2, RecipientType = "TO", RuleType = "BY_ENTITY_RELATION", RuleValue = "COMMITTEE_STAGE_RESPONSIBLE", Priority = 1, StatusRegister = true }
            };
            var committeeResult = await resolver.ResolveRecipientsAsync(committeeRules, new Dictionary<string, object>
            {
                ["InscriptionModalityId"] = 70
            });
            committeeResult.To.Should().BeEquivalentTo(new[]
            {
                "director.comite@unicesar.edu.co",
                "secretaria.comite@unicesar.edu.co"
            });
        }

        [Fact]
        public async Task FaultIsolation_And_DashboardTriggerEndpoint_BehaveDeterministically()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            var t25 = SubtractBusinessDaysColombia(nowUtc, 25);
            foreach (var id in new[] { 80, 81 })
            {
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                    CreatedAt = t25, UpdatedAt = t25, StatusRegister = true
                });
                ctx.Set<Proposal>().Add(new Proposal { Id = id, Title = $"Proyecto {id}", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, StatusRegister = true });
                ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = id, IdStateStage = 4, CreatedAt = t25, UpdatedAt = t25, StatusRegister = true });
                ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = id, IdInscriptionModality = id, IdUser = 1, IsConfirmed = true, StatusRegister = true });
                ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = id, IdInscriptionModality = id, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true });
            }
            await ctx.SaveChangesAsync();

            // Simular fallo SMTP únicamente para "Proyecto 80"; "Proyecto 81" debe enviarse y persistir su log
            var (service, sentEmails) = BuildFullPipeline(
                ctx,
                clock,
                sendOverride: mail => Task.FromResult(!mail.Subject.Contains("Proyecto 80")));

            var controller = new DashboardController(Mock.Of<IMediator>());
            var classPerm = typeof(DashboardController)
                .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .Cast<RequirePermissionAttribute>()
                .Single();
            classPerm.PermissionCodes.Should().Contain(PermissionCodes.Reportes.N2EDD);

            var actionResult = await controller.RunSlaAlerts(service);
            var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
            var payload = okResult.Value.Should().BeOfType<ApiResponse<SlaAlertExecutionSummary>>().Subject;

            payload.Success.Should().BeTrue();
            payload.Data!.EvaluatedInscriptions.Should().Be(2);
            payload.Data.AlertsFailed.Should().Be(1);
            payload.Data.AlertsTriggered.Should().Be(1);
            sentEmails.Should().ContainSingle(e => e.Subject.Contains("Proyecto 81"));

            var logs = await ctx.Set<EmailAlertDispatchLog>().ToListAsync();
            logs.Should().ContainSingle(l => l.IdInscriptionModality == 81);
        }

        [Fact]
        public async Task DbConfiguration_IsEnabledFalse_DisablesEngineAndSkipsSending()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            ctx.SlaAlertConfigurations.Add(new SlaAlertConfiguration
            {
                Id = 1,
                IsEnabled = false,
                CreatedAt = nowUtc.AddDays(-1),
                OperationRegister = "TEST",
                StatusRegister = true
            });

            var t26 = SubtractBusinessDaysColombia(nowUtc, 26);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 90, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t26, UpdatedAt = t26, StatusRegister = true
            });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 90, IdStateStage = 4, CreatedAt = t26, UpdatedAt = t26, StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 90, IdInscriptionModality = 90, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = 90, IdInscriptionModality = 90, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true });
            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(
                ctx,
                clock,
                extraConfig: new Dictionary<string, string?> { ["SlaAlerts:Enabled"] = "true" });

            var summary = await service.ProcessDueAlertsAsync();
            summary.EvaluatedInscriptions.Should().Be(0);
            summary.AlertsTriggered.Should().Be(0);
            sentEmails.Should().BeEmpty();
        }

        [Fact]
        public async Task DbConfiguration_CustomThresholds_And_WarnOnLastAllowedPeriodFalse_AreHonored_And_PersistRunMetrics()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            var clock = new ManualTimeProvider(nowUtc);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            ctx.SlaAlertConfigurations.Add(new SlaAlertConfiguration
            {
                Id = 1,
                IsEnabled = true,
                ScheduleHourColombia = 8,
                ScheduleMinuteColombia = 15,
                WeekdaysOnly = true,
                EvaluatorWarningBusinessDays = 8,
                EvaluatorOverdueBusinessDays = 12,
                CommitteeOverdueBusinessDays = 4,
                StudentObservationsBusinessDays = 5,
                DefenseReminderHours = 12,
                WarnOnLastAllowedPeriod = false,
                CreatedAt = nowUtc.AddDays(-2),
                OperationRegister = "TEST",
                StatusRegister = true
            });

            // 1. Inscripción 91: 13 días hábiles en AP_EN_EVALUACION -> con umbral DB=12 dispara SLA_EVALUATOR_OVERDUE
            var t13 = SubtractBusinessDaysColombia(nowUtc, 13);
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 91, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2,
                CreatedAt = t13, UpdatedAt = t13, StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 91, Title = "PG Umbral 12", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 14, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 91, IdStateStage = 4, CreatedAt = t13, UpdatedAt = t13, StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 91, IdInscriptionModality = 91, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = 91, IdInscriptionModality = 91, IdTeacher = 2, IdTypeTeachingAssignment = 2, IsConfirmed = true, StatusRegister = true });

            // 2. Co-Terminal (MaxTerm=2) con WarnOnLastAllowedPeriod=false:
            //    - 92 en 2025-2 (2/2 WARNING) -> NO debe disparar
            //    - 93 en 2025-1 (3/2 EXCEEDED) -> SÍ debe disparar
            var recent = nowUtc.AddDays(-1);
            foreach (var (id, periodId, name) in new[] { (92, 2, "CT 2/2 Ignorado"), (93, 1, "CT 3/2 Excedido") })
            {
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 3, IdStateInscription = 2, IdAcademicPeriod = periodId, IdStageModality = 6,
                    CreatedAt = recent, UpdatedAt = recent, StatusRegister = true
                });
                ctx.Set<CoTerminal>().Add(new CoTerminal { Id = id, IdStateStage = 12, PostgraduateProgramName = name, CreatedAt = recent, UpdatedAt = recent, StatusRegister = true });
                ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = id, IdInscriptionModality = id, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            }

            await ctx.SaveChangesAsync();

            var (service, sentEmails) = BuildFullPipeline(ctx, clock);
            var summary = await service.ProcessDueAlertsAsync();

            summary.EvaluatedInscriptions.Should().Be(3);
            summary.AlertsTriggered.Should().Be(2);
            summary.DispatchedKeys.Should().Contain(k => k.StartsWith("91|SLA_EVALUATOR_OVERDUE|", StringComparison.Ordinal));
            summary.DispatchedKeys.Should().Contain("93|SLA_TERM_LIMIT_WARNING|EXCEEDED:3_OF_2");
            summary.DispatchedKeys.Should().NotContain(k => k.StartsWith("92|SLA_TERM_LIMIT_WARNING|", StringComparison.Ordinal));
            sentEmails.Should().HaveCount(2);

            var updatedCfg = await ctx.SlaAlertConfigurations.AsNoTracking().SingleAsync(x => x.Id == 1);
            updatedCfg.LastRunAt.Should().Be(nowUtc);
            updatedCfg.LastRunEvaluatedCount.Should().Be(3);
            updatedCfg.LastRunTriggeredCount.Should().Be(2);
            updatedCfg.LastRunSkippedCount.Should().Be(0);
            updatedCfg.LastRunFailedCount.Should().Be(0);
        }

        [Fact]
        public async Task RecipientResolver_CommitteeStageResponsible_UsesSiblingManagementPermissionBeforeInstitutionalRoleFallback()
        {
            var nowUtc = new DateTimeOffset(2026, 5, 20, 15, 0, 0, TimeSpan.Zero);
            using var ctx = CreateInMemoryContext();
            SeedBaseCatalog(ctx, nowUtc);

            // Agregar modalidades restantes para cubrir todas las ramas de sibling management permissions
            ctx.Set<Modality>().AddRange(
                new Modality { Id = 5, Code = ModalityCodes.PublicacionArticulo, Name = "Publicación de Artículo", StatusRegister = true },
                new Modality { Id = 6, Code = ModalityCodes.GradoPromedio, Name = "Grado por Promedio", StatusRegister = true },
                new Modality { Id = 7, Code = ModalityCodes.SeminarioAct, Name = "Seminario de Actualización", StatusRegister = true });

            // Roles COMMITTEE_DIRECTOR y SECRETARY existen con usuarios activos (User 4 y User 5)
            ctx.Set<Role>().AddRange(
                new Role { Id = 20, Code = "COMMITTEE_DIRECTOR", Name = "Director Comité", StatusRegister = true },
                new Role { Id = 21, Code = "SECRETARY", Name = "Secretaría", StatusRegister = true });
            ctx.Set<UserRole>().AddRange(
                new UserRole { Id = 20, IdUser = 4, IdRole = 20, StatusRegister = true },
                new UserRole { Id = 21, IdUser = 5, IdRole = 21, StatusRegister = true });

            // User 3 tendrá únicamente los permisos hermanos de GESTIÓN (SIN permisos primarios *R / N4PGPAA)
            var siblingPermissions = new (int PermId, string Code)[]
            {
                (301, PermissionCodes.ModalidadesGrado.N3MGPG),
                (302, PermissionCodes.ProyectoGrado.N4PGPPG),
                (303, PermissionCodes.ProyectoGrado.N4PGPAG),
                (304, PermissionCodes.ProyectoGrado.N4PGPSG),
                (305, PermissionCodes.PublicacionArticulo.N3PCSGG),
                (306, PermissionCodes.SaberPro.N3SPSGG),
                (307, PermissionCodes.GradoPromedio.N3GPSGG),
                (308, PermissionCodes.CoTerminal.N3CTSGG),
                (309, PermissionCodes.Seminario.N3SASGG)
            };

            foreach (var (permId, code) in siblingPermissions)
            {
                ctx.Set<Permission>().Add(new Permission { Id = permId, Code = code, Description = code, StatusRegister = true });
                ctx.Set<UserPermission>().Add(new UserPermission { Id = permId, IdUser = 3, IdPermission = permId, StatusRegister = true });
            }

            // Crear 9 inscripciones (una por cada rama del mapeo hermano)
            ctx.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 201, IdModality = 1, IdStateInscription = 1, IdAcademicPeriod = 3, StatusRegister = true }, // PENDIENTE -> N3MGPG
                new InscriptionModality { Id = 202, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 1, StatusRegister = true }, // PG_FASE_PROPUESTA -> N4PGPPG
                new InscriptionModality { Id = 203, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 2, StatusRegister = true }, // PG_FASE_ANTEPROYECTO -> N4PGPAG
                new InscriptionModality { Id = 204, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 4, StatusRegister = true }, // PG_FASE_SUSTENTACION -> N4PGPSG
                new InscriptionModality { Id = 205, IdModality = 5, IdStateInscription = 2, IdAcademicPeriod = 3, StatusRegister = true }, // PUBLICACION_ARTICULO -> N3PCSGG
                new InscriptionModality { Id = 206, IdModality = 4, IdStateInscription = 4, IdAcademicPeriod = 3, StatusRegister = true }, // SABER_PRO -> N3SPSGG
                new InscriptionModality { Id = 207, IdModality = 6, IdStateInscription = 4, IdAcademicPeriod = 3, StatusRegister = true }, // GRADO_PROMEDIO -> N3GPSGG
                new InscriptionModality { Id = 208, IdModality = 3, IdStateInscription = 2, IdAcademicPeriod = 3, StatusRegister = true }, // COTERMINAL -> N3CTSGG
                new InscriptionModality { Id = 209, IdModality = 7, IdStateInscription = 2, IdAcademicPeriod = 3, StatusRegister = true }  // SEMINARIO_ACT -> N3SASGG
            );
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);
            var rules = new List<EmailRecipientRule>
            {
                new() { Id = 1, RecipientType = "TO", RuleType = "BY_ENTITY_RELATION", RuleValue = "COMMITTEE_STAGE_RESPONSIBLE", Priority = 1, StatusRegister = true }
            };

            foreach (var insId in Enumerable.Range(201, 9))
            {
                var resolved = await resolver.ResolveRecipientsAsync(rules, new Dictionary<string, object>
                {
                    ["InscriptionModalityId"] = insId
                });

                // Debe resolver el permiso hermano de gestión (User 3) y NO caer a COMMITTEE_DIRECTOR / SECRETARY
                resolved.To.Should().Equal("comite.pg@unicesar.edu.co");
            }

            SlaCronHelper.BuildCron(7, 30, weekdaysOnly: false).Should().Be("30 7 * * *");
            SlaCronHelper.BuildCron(8, 5, weekdaysOnly: true).Should().Be("5 8 * * 1-5");
        }
    }
}
