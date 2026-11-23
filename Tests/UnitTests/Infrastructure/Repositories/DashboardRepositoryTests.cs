using Domain.Common.Dashboard;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Repositories
{
    public class DashboardRepositoryTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static SeedIds SeedCatalog(AppDbContext ctx)
        {
            var faculty = new Faculty { Id = 1, Code = "ING", Name = "Ingenierías y Tecnológicas", StatusRegister = true };
            var program = new AcademicProgram { Id = 1, Code = "SIS", Name = "Ingeniería de Sistemas", IdFaculty = 1, Faculty = faculty, StatusRegister = true };
            var idType = new IdentificationType { Id = 1, Code = "CC", Name = "Cédula", StatusRegister = true };

            var stPend = new StateInscription { Id = 1, Code = "PENDIENTE", Name = "Pendiente", StatusRegister = true };
            var stAprob = new StateInscription { Id = 2, Code = "APROBADO", Name = "Aprobado", StatusRegister = true };
            var stRech = new StateInscription { Id = 3, Code = "RECHAZADO", Name = "Rechazado", IsRejectionState = true, StatusRegister = true };
            var stNoApl = new StateInscription { Id = 4, Code = "NO_APLICA", Name = "No Aplica", StatusRegister = true };

            var mPg = new Modality { Id = 1, Code = "PROYECTO_GRADO", Name = "Proyecto de Grado", MaximumTermPeriods = 3, StatusRegister = true };
            var mPa = new Modality { Id = 2, Code = "PRACTICA_ACADEMICA", Name = "Práctica Académica", MaximumTermPeriods = 2, StatusRegister = true };
            var mCt = new Modality { Id = 3, Code = "COTERMINAL", Name = "Co-Terminal", MaximumTermPeriods = 2, StatusRegister = true };
            var mSem = new Modality { Id = 4, Code = "SEMINARIO_ACT", Name = "Seminario de Actualización", MaximumTermPeriods = 1, StatusRegister = true };
            var mArt = new Modality { Id = 5, Code = "PUBLICACION_ARTICULO", Name = "Publicación de Artículo", MaximumTermPeriods = 2, StatusRegister = true };
            var mGp = new Modality { Id = 6, Code = "GRADO_PROMEDIO", Name = "Grado por Promedio", MaximumTermPeriods = null, StatusRegister = true };
            var mSp = new Modality { Id = 7, Code = "SABER_PRO", Name = "Saber Pro", MaximumTermPeriods = null, StatusRegister = true };

            var stagePg1 = new StageModality { Id = 1, IdModality = 1, StageOrder = 1, Code = "PG_FASE_PROPUESTA", Name = "Propuesta", StatusRegister = true };
            var stagePg2 = new StageModality { Id = 2, IdModality = 1, StageOrder = 2, Code = "PG_FASE_ANTEPROYECTO", Name = "Anteproyecto", StatusRegister = true };
            var stagePg3 = new StageModality { Id = 3, IdModality = 1, StageOrder = 3, Code = "PG_FASE_PROYECTO", Name = "Proyecto Final", StatusRegister = true };
            var stagePg4 = new StageModality { Id = 4, IdModality = 1, StageOrder = 4, Code = "PG_FASE_SUSTENTACION", Name = "Sustentación", StatusRegister = true };
            var stagePa1 = new StageModality { Id = 5, IdModality = 2, StageOrder = 1, Code = "PA_FASE_INSCRIPCION", Name = "Inscripción PA", StatusRegister = true };
            var stageCt1 = new StageModality { Id = 6, IdModality = 3, StageOrder = 1, Code = "CT_FASE", Name = "Co-Terminal", StatusRegister = true };
            var stageSem1 = new StageModality { Id = 7, IdModality = 4, StageOrder = 1, Code = "SEM_FASE", Name = "Seminario", StatusRegister = true };
            var stageArt1 = new StageModality { Id = 8, IdModality = 5, StageOrder = 1, Code = "ART_FASE", Name = "Artículo", StatusRegister = true };
            var stageGp1 = new StageModality { Id = 9, IdModality = 6, StageOrder = 1, Code = "GP_FASE", Name = "Grado Promedio", StatusRegister = true };
            var stageSp1 = new StageModality { Id = 10, IdModality = 7, StageOrder = 1, Code = "SP_FASE", Name = "Saber Pro", StatusRegister = true };

            // StateStages
            var ssPropPert = new StateStage { Id = 1, IdStageModality = 1, Code = "PROP_PERTINENTE", Name = "Propuesta Pertinente", StatusRegister = true };
            var ssPropRad = new StateStage { Id = 2, IdStageModality = 1, Code = "PROP_RADICADA", Name = "Propuesta Radicada", StatusRegister = true };
            var ssApEval = new StateStage { Id = 3, IdStageModality = 2, Code = "AP_EN_EVALUACION", Name = "Anteproyecto en Evaluación", StatusRegister = true };
            var ssApObs = new StateStage { Id = 4, IdStageModality = 2, Code = "AP_CON_OBSERVACIONES", Name = "Anteproyecto con Observaciones", StatusRegister = true };
            var ssPfinfEval = new StateStage { Id = 5, IdStageModality = 3, Code = "PFINF_RADICADO_EN_EVALUACION", Name = "Informe Final en Evaluación", StatusRegister = true };
            var ssSustProg = new StateStage { Id = 6, IdStageModality = 4, Code = "SUST_PROGRAMADA", Name = "Sustentación Programada", StatusRegister = true };
            var ssSustAprob = new StateStage { Id = 7, IdStageModality = 4, Code = "SUST_APROBADA", Name = "Sustentación Aprobada", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssSustReintento = new StateStage { Id = 8, IdStageModality = 4, Code = "SUST_NO_APROBADA_REINTENTO", Name = "No Aprobada (Reintento)", StatusRegister = true };
            var ssSustReprob = new StateStage { Id = 9, IdStageModality = 4, Code = "SUST_REPROBADA_FINAL", Name = "Reprobada Definitiva", IsFinalStateForModalityOverall = true, IsRejectionState = true, StatusRegister = true };

            var ssPaAprob = new StateStage { Id = 10, IdStageModality = 5, Code = "PA_APROBADO", Name = "Práctica Aprobada", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssPaEval = new StateStage { Id = 11, IdStageModality = 5, Code = "PA_INFORME_FINAL_EN_REVISION", Name = "Informe Final PA en Revisión", StatusRegister = true };
            var ssCtRad = new StateStage { Id = 12, IdStageModality = 6, Code = "CT_RADICADO", Name = "Co-Terminal Radicado", StatusRegister = true };
            var ssSemAprob = new StateStage { Id = 13, IdStageModality = 7, Code = "SEM_APROBADO", Name = "Seminario Aprobado", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssArtAprob = new StateStage { Id = 14, IdStageModality = 8, Code = "ART_PUB_APROBADO", Name = "Artículo Aprobado", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssGpAprob = new StateStage { Id = 15, IdStageModality = 9, Code = "GP_APROBADO", Name = "Grado por Promedio Aprobado", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssGpRech = new StateStage { Id = 16, IdStageModality = 9, Code = "GP_RECHAZADO", Name = "Grado por Promedio Rechazado", IsFinalStateForModalityOverall = true, IsRejectionState = true, StatusRegister = true };
            var ssSpAprob = new StateStage { Id = 17, IdStageModality = 10, Code = "SP_APROBADO", Name = "Saber Pro Aprobado", IsFinalStateForModalityOverall = true, StatusRegister = true };
            var ssSpRad = new StateStage { Id = 18, IdStageModality = 10, Code = "SP_RADICADO", Name = "Saber Pro Radicado", StatusRegister = true };

            var now = DateTimeOffset.UtcNow;
            var p1 = new AcademicPeriod { Id = 1, Code = "2024-1", StartDate = now.AddMonths(-24), EndDate = now.AddMonths(-18), StatusRegister = true };
            var p2 = new AcademicPeriod { Id = 2, Code = "2024-2", StartDate = now.AddMonths(-18), EndDate = now.AddMonths(-12), StatusRegister = true };
            var p3 = new AcademicPeriod { Id = 3, Code = "2025-1", StartDate = now.AddMonths(-12), EndDate = now.AddMonths(-6), StatusRegister = true };
            var p4 = new AcademicPeriod { Id = 4, Code = "2025-2", StartDate = now.AddMonths(-6), EndDate = now.AddMonths(-1), StatusRegister = true };
            var p5 = new AcademicPeriod { Id = 5, Code = "2026-1", StartDate = now.AddMonths(-1), EndDate = now.AddMonths(5), StatusRegister = true };

            var rl1 = new ResearchLine { Id = 1, Code = "RL1", Name = "Ingeniería de Software", StatusRegister = true };
            var rsl1 = new ResearchSubLine { Id = 1, Code = "RSL1", Name = "Arquitectura Cloud", IdResearchLine = 1, StatusRegister = true };
            var rsl2 = new ResearchSubLine { Id = 2, Code = "RSL2", Name = "Inteligencia Artificial", IdResearchLine = 1, StatusRegister = true };

            var roleDirector = new TypeTeachingAssignment { Id = 1, Code = "DIRECTOR", Name = "Director", StatusRegister = true };
            var roleJurado = new TypeTeachingAssignment { Id = 2, Code = "JURADO_EVALUADOR", Name = "Jurado Evaluador", StatusRegister = true };

            var evalType = new EvaluationType { Id = 1, Code = "COMITE", Name = "Comité", StatusRegister = true };
            var docClass = new DocumentClass { Id = 1, Code = "DOC", Name = "Documento", StatusRegister = true };
            var docType = new DocumentType { Id = 1, Code = "DT1", Name = "Informe", IdDocumentClass = 1, StatusRegister = true };

            var student1 = new User { Id = 1, IdIdentificationType = 1, Identification = "10650001", Email = "ana@unicesar.edu.co", FirstName = "Ana", LastName = "Gómez", IdAcademicProgram = 1, StatusRegister = true };
            var student2 = new User { Id = 2, IdIdentificationType = 1, Identification = "10650002", Email = "luis@unicesar.edu.co", FirstName = "Luis", LastName = "Martínez", IdAcademicProgram = 1, StatusRegister = true };
            var teacher1 = new User { Id = 10, IdIdentificationType = 1, Identification = "770010", Email = "prof1@unicesar.edu.co", FirstName = "Carlos", LastName = "Ríos", IdAcademicProgram = 1, StatusRegister = true };
            var teacher2 = new User { Id = 11, IdIdentificationType = 1, Identification = "770011", Email = "prof2@unicesar.edu.co", FirstName = "María", LastName = "Díaz", IdAcademicProgram = 1, StatusRegister = true };

            ctx.Set<Faculty>().Add(faculty);
            ctx.Set<AcademicProgram>().Add(program);
            ctx.Set<IdentificationType>().Add(idType);
            ctx.Set<StateInscription>().AddRange(stPend, stAprob, stRech, stNoApl);
            ctx.Set<Modality>().AddRange(mPg, mPa, mCt, mSem, mArt, mGp, mSp);
            ctx.Set<StageModality>().AddRange(stagePg1, stagePg2, stagePg3, stagePg4, stagePa1, stageCt1, stageSem1, stageArt1, stageGp1, stageSp1);
            ctx.Set<StateStage>().AddRange(
                ssPropPert, ssPropRad, ssApEval, ssApObs, ssPfinfEval, ssSustProg, ssSustAprob, ssSustReintento, ssSustReprob,
                ssPaAprob, ssPaEval, ssCtRad, ssSemAprob, ssArtAprob, ssGpAprob, ssGpRech, ssSpAprob, ssSpRad);
            ctx.Set<AcademicPeriod>().AddRange(p1, p2, p3, p4, p5);
            ctx.Set<ResearchLine>().Add(rl1);
            ctx.Set<ResearchSubLine>().AddRange(rsl1, rsl2);
            ctx.Set<TypeTeachingAssignment>().AddRange(roleDirector, roleJurado);
            ctx.Set<EvaluationType>().Add(evalType);
            ctx.Set<DocumentClass>().Add(docClass);
            ctx.Set<DocumentType>().Add(docType);
            ctx.Set<User>().AddRange(student1, student2, teacher1, teacher2);
            ctx.SaveChanges();

            return new SeedIds();
        }

        private sealed class SeedIds { }

        [Fact]
        public async Task ProyectoGrado_FourPhases_Resolves_True_Active_PhaseState_In_Breakdown_Scope_And_Export()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // Inscripción 1: Fase 1 (Proposal -> PROP_RADICADA)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 1, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 1,
                CreatedAt = now.AddDays(-10), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal
            {
                Id = 1, Title = "Proyecto Fase 1", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 2,
                CreatedAt = now.AddDays(-10), StatusRegister = true
            });

            // Inscripción 2: Fase 2 (Proposal=PROP_PERTINENTE, PreliminaryProject=AP_CON_OBSERVACIONES)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 2, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 2,
                CreatedAt = now.AddDays(-40), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal
            {
                Id = 2, Title = "Proyecto Fase 2", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1,
                CreatedAt = now.AddDays(-40), StatusRegister = true
            });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject
            {
                Id = 2, IdStateStage = 4, CreatedAt = now.AddDays(-20), StatusRegister = true
            });

            // Inscripción 3: Fase 3 (ProjectFinal=PFINF_RADICADO_EN_EVALUACION)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 3, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 4, IdStageModality = 3,
                CreatedAt = now.AddDays(-90), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal
            {
                Id = 3, Title = "Proyecto Fase 3", IdResearchLine = 1, IdResearchSubLine = 2, IdStateStage = 1,
                CreatedAt = now.AddDays(-90), StatusRegister = true
            });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject
            {
                Id = 3, IdStateStage = 3, CreatedAt = now.AddDays(-60), StatusRegister = true
            });
            ctx.Set<ProjectFinal>().Add(new ProjectFinal
            {
                Id = 3, IdStateStage = 5, CreatedAt = now.AddDays(-15), StatusRegister = true
            });

            // Inscripción 4: Fase 4 (ProjectPresentation=SUST_APROBADA -> COMPLETED)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 4, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 4,
                CreatedAt = now.AddDays(-120), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal
            {
                Id = 4, Title = "Proyecto Fase 4 Aprobado", IdResearchLine = 1, IdResearchSubLine = 2, IdStateStage = 1,
                CreatedAt = now.AddDays(-120), StatusRegister = true
            });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject
            {
                Id = 4, IdStateStage = 3, CreatedAt = now.AddDays(-90), StatusRegister = true
            });
            ctx.Set<ProjectFinal>().Add(new ProjectFinal
            {
                Id = 4, IdStateStage = 5, CreatedAt = now.AddDays(-50), StatusRegister = true
            });
            ctx.Set<ProjectPresentation>().Add(new ProjectPresentation
            {
                Id = 4, IdStateStage = 7, AttemptNumber = 1, PresentationModality = "PRESENCIAL",
                ScheduledDateTime = now.AddDays(-20), ActualDateTime = now.AddDays(-20),
                CreatedAt = now.AddDays(-30), StatusRegister = true
            });

            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);
            var filter = new DashboardFilter { ModalityCodes = new List<string> { "PROYECTO_GRADO" } };

            // 1. Breakdown ByStateStage refleja las 4 fases reales y NO congela todo en PROP_PERTINENTE
            var breakdown = await repo.GetModalityBreakdownAsync(filter);
            breakdown.Should().NotBeNull();
            breakdown!.ByStateStage.Should().BeEquivalentTo(new[]
            {
                new CategoryCount("PROP_RADICADA", "Propuesta Radicada", 1),
                new CategoryCount("AP_CON_OBSERVACIONES", "Anteproyecto con Observaciones", 1),
                new CategoryCount("PFINF_RADICADO_EN_EVALUACION", "Informe Final en Evaluación", 1),
                new CategoryCount("SUST_APROBADA", "Sustentación Aprobada", 1)
            });

            // 2. Filtro scope StateStageCodes = ["PROP_PERTINENTE"] NO debe devolver inscripciones que ya avanzaron a fases 2-4
            var filteredPropPert = await repo.GetSummaryAsync(new DashboardFilter
            {
                ModalityCodes = new List<string> { "PROYECTO_GRADO" },
                ModalityScope = new ModalityScopedFilter { StateStageCodes = new List<string> { "PROP_PERTINENTE" } }
            });
            filteredPropPert.TotalInscriptions.Should().Be(0);

            // 3. Filtro scope StateStageCodes = ["SUST_APROBADA"] devuelve exactamente la inscripción 4
            var filteredSust = await repo.GetExportRowsAsync(new DashboardFilter
            {
                ModalityCodes = new List<string> { "PROYECTO_GRADO" },
                ModalityScope = new ModalityScopedFilter { StateStageCodes = new List<string> { "SUST_APROBADA" } }
            });
            filteredSust.Should().HaveCount(1);
            filteredSust[0].InscriptionId.Should().Be(4);
            filteredSust[0].PhaseState.Should().Be("Sustentación Aprobada");
            filteredSust[0].LifecycleCode.Should().Be("COMPLETED");

            // 4. Summary lifecycle + AvgCycleDays (~100 días entre CreatedAt -120 y ActualDateTime -20)
            var summary = await repo.GetSummaryAsync(filter);
            summary.TotalInscriptions.Should().Be(4);
            summary.Completed.Should().Be(1);
            summary.InReview.Should().Be(2); // PROP_RADICADA + PFINF_RADICADO_EN_EVALUACION
            summary.WithObservations.Should().Be(1); // AP_CON_OBSERVACIONES
            summary.InProgress.Should().Be(0);
            summary.RejectedOverall.Should().Be(0);
            summary.AvgCycleDays.Should().BeApproximately(100.0, 0.5);
        }

        [Fact]
        public async Task SaberPro_And_GradoPromedio_NoAplica_Computes_Lifecycle_And_Rates_Correctly()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // 1. SABER_PRO: NO_APLICA + SP_APROBADO -> COMPLETED
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 10, IdModality = 7, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 10,
                CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-5), StatusRegister = true
            });
            ctx.Set<SaberPro>().Add(new SaberPro
            {
                Id = 10, IdStateStage = 17, ResultQuintile = "Q5", ResultScore = 215m,
                CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-5), StatusRegister = true
            });

            // 2. GRADO_PROMEDIO: NO_APLICA + GP_RECHAZADO -> REJECTED
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 11, IdModality = 6, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 9,
                CreatedAt = now.AddDays(-18), StatusRegister = true
            });
            ctx.Set<AcademicAverage>().Add(new AcademicAverage
            {
                Id = 11, IdStateStage = 16, CertifiedAverage = 4.35m, HasFailedSubjects = true,
                CreatedAt = now.AddDays(-18), StatusRegister = true
            });

            // 3. SABER_PRO: NO_APLICA + SP_RADICADO -> IN_REVIEW
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 12, IdModality = 7, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 10,
                CreatedAt = now.AddDays(-5), StatusRegister = true
            });
            ctx.Set<SaberPro>().Add(new SaberPro
            {
                Id = 12, IdStateStage = 18, ResultQuintile = "Q4", ResultScore = 185m,
                CreatedAt = now.AddDays(-5), StatusRegister = true
            });

            // 4. PROYECTO_GRADO: APROBADO (requiere aval) + PROP_PERTINENTE -> IN_PROGRESS
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 13, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 1,
                CreatedAt = now.AddDays(-10), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal
            {
                Id = 13, Title = "Propuesta Activa", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1,
                CreatedAt = now.AddDays(-10), StatusRegister = true
            });

            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);
            var summary = await repo.GetSummaryAsync(new DashboardFilter());

            summary.TotalInscriptions.Should().Be(4);
            summary.Completed.Should().Be(1);
            summary.RejectedOverall.Should().Be(1);
            summary.InReview.Should().Be(1);
            summary.InProgress.Should().Be(1);
            summary.WithObservations.Should().Be(0);

            (summary.Completed + summary.InReview + summary.WithObservations + summary.InProgress + summary.RejectedOverall)
                .Should().Be(summary.TotalInscriptions);

            summary.CompletionRate.Should().Be(25.0);
            summary.SuccessRateOverClosed.Should().Be(50.0); // 1 / (1 + 1)
            summary.NotApplicable.Should().Be(3);
            summary.ApprovalRate.Should().Be(100.0); // 1 aprobado / (4 total - 3 NO_APLICA)

            summary.ByState.Should().Contain(s => s.Key == "NO_APLICA" && s.Label == "Automático (No requiere aval)");

            // Filtro LifecycleStatuses en SQL
            var onlyCompleted = await repo.GetSummaryAsync(new DashboardFilter
            {
                LifecycleStatuses = new List<string> { "COMPLETED" }
            });
            onlyCompleted.TotalInscriptions.Should().Be(1);
            onlyCompleted.Completed.Should().Be(1);
        }

        [Fact]
        public async Task DateTo_Inclusive_Colombia_EndOfDay_Includes_LateNight_Colombia_Record()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);

            // 2026-03-15 23:30 Colombia (UTC-5) == 2026-03-16 04:30 UTC
            var lateColombiaSameDayUtc = new DateTimeOffset(2026, 3, 16, 4, 30, 0, TimeSpan.Zero);
            // 2026-03-16 00:15 Colombia (UTC-5) == 2026-03-16 05:15 UTC (día siguiente!)
            var nextDayColombiaUtc = new DateTimeOffset(2026, 3, 16, 5, 15, 0, TimeSpan.Zero);

            ctx.Set<InscriptionModality>().AddRange(
                new InscriptionModality
                {
                    Id = 20, IdModality = 6, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 9,
                    CreatedAt = lateColombiaSameDayUtc, StatusRegister = true
                },
                new InscriptionModality
                {
                    Id = 21, IdModality = 6, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 9,
                    CreatedAt = nextDayColombiaUtc, StatusRegister = true
                });
            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);
            var filter = new DashboardFilter
            {
                DateFrom = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero),
                DateTo = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero)
            };

            var rows = await repo.GetExportRowsAsync(filter);

            rows.Should().HaveCount(1);
            rows[0].InscriptionId.Should().Be(20);
            rows[0].CreatedAt.Should().Be("2026-03-15 23:30");
        }

        [Fact]
        public async Task ActiveTeachersOnly_Excludes_Completed_And_Rejected_When_True_And_Includes_When_False()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // Inscripción 30: ACTIVA (IN_REVIEW) con teacher1 (Director) y teacher2 (Jurado)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 30, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 2,
                CreatedAt = now.AddDays(-20), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 30, Title = "Activa", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 30, IdStateStage = 3, StatusRegister = true });

            // Inscripción 31: COMPLETED (SUST_APROBADA) con teacher1 (Director)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 31, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 4, IdStageModality = 4,
                CreatedAt = now.AddDays(-100), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 31, Title = "Completada", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true });
            ctx.Set<ProjectPresentation>().Add(new ProjectPresentation { Id = 31, IdStateStage = 7, StatusRegister = true });

            // Inscripción 32: REJECTED (RECHAZADO) con teacher1 (Jurado)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 32, IdModality = 1, IdStateInscription = 3, IdAcademicPeriod = 4, IdStageModality = 1,
                CreatedAt = now.AddDays(-60), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 32, Title = "Rechazada", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 2, StatusRegister = true });

            ctx.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment { Id = 1, IdInscriptionModality = 30, IdTeacher = 10, IdTypeTeachingAssignment = 1, StatusRegister = true },
                new TeachingAssignment { Id = 2, IdInscriptionModality = 30, IdTeacher = 11, IdTypeTeachingAssignment = 2, StatusRegister = true },
                new TeachingAssignment { Id = 3, IdInscriptionModality = 31, IdTeacher = 10, IdTypeTeachingAssignment = 1, StatusRegister = true },
                new TeachingAssignment { Id = 4, IdInscriptionModality = 32, IdTeacher = 10, IdTypeTeachingAssignment = 2, StatusRegister = true }
            );
            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);

            var activeSummary = await repo.GetSummaryAsync(new DashboardFilter { ActiveTeachersOnly = true });
            var t1Active = activeSummary.TeacherLoadDetailed.Single(t => t.TeacherId == 10);
            t1Active.Total.Should().Be(1);
            t1Active.ByRole.Should().ContainKey("Director").WhoseValue.Should().Be(1);
            t1Active.ByRole.Should().NotContainKey("Jurado Evaluador");

            var historicSummary = await repo.GetSummaryAsync(new DashboardFilter { ActiveTeachersOnly = false });
            var t1Historic = historicSummary.TeacherLoadDetailed.Single(t => t.TeacherId == 10);
            t1Historic.Total.Should().Be(3);
            t1Historic.ByRole["Director"].Should().Be(2);
            t1Historic.ByRole["Jurado Evaluador"].Should().Be(1);
        }

        [Fact]
        public async Task GetModalityBreakdownAsync_Populates_NumericStats_Buckets_TopEntities_And_DefenseSummary()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // Prácticas Académicas con horas 600, 640, 720, 960 -> Count=4, Min=600, Max=960, Avg=730, Median=680
            var hours = new[] { 600, 640, 720, 960 };
            for (var idx = 0; idx < hours.Length; idx++)
            {
                var id = 40 + idx;
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 5,
                    CreatedAt = now.AddDays(-10), StatusRegister = true
                });
                ctx.Set<AcademicPractice>().Add(new AcademicPractice
                {
                    Id = id, IdStateStage = 10, Title = $"Práctica {idx}",
                    InstitutionName = idx < 3 ? "Drummond Ltd" : "Ecopetrol",
                    PracticeHours = hours[idx], IsEmprendimiento = idx == 3, StatusRegister = true
                });
            }

            // Proyecto de Grado con 3 sustentaciones: 1er intento aprobada, 2do intento aprobada, 1 reprobada final
            for (var idx = 0; idx < 3; idx++)
            {
                var id = 50 + idx;
                ctx.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = id, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 4,
                    CreatedAt = now.AddDays(-60), StatusRegister = true
                });
                ctx.Set<Proposal>().Add(new Proposal
                {
                    Id = id, Title = $"PG {idx}", IdResearchLine = 1, IdResearchSubLine = idx < 2 ? 1 : 2, IdStateStage = 1, StatusRegister = true
                });
            }
            ctx.Set<ProjectPresentation>().AddRange(
                new ProjectPresentation { Id = 50, IdStateStage = 7, AttemptNumber = 1, PresentationModality = "PRESENCIAL", ScheduledDateTime = now.AddDays(-5), StatusRegister = true },
                new ProjectPresentation { Id = 51, IdStateStage = 7, AttemptNumber = 2, PresentationModality = "VIRTUAL", ScheduledDateTime = now.AddDays(-3), StatusRegister = true },
                new ProjectPresentation { Id = 52, IdStateStage = 9, AttemptNumber = 2, PresentationModality = "PRESENCIAL", ScheduledDateTime = now.AddDays(-1), StatusRegister = true }
            );
            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);

            // PA Breakdown
            var paBreakdown = await repo.GetModalityBreakdownAsync(new DashboardFilter { ModalityCodes = new List<string> { "PRACTICA_ACADEMICA" } });
            paBreakdown.Should().NotBeNull();
            paBreakdown!.NumericStats.Should().Be(new NumericSummaryStats(4, 600m, 730m, 680m, 960m, "horas"));
            paBreakdown.NumericBuckets.Should().Contain(b => b.Key == "< 640" && b.Count == 1);
            paBreakdown.NumericBuckets.Should().Contain(b => b.Key == "640" && b.Count == 1);
            paBreakdown.NumericBuckets.Should().Contain(b => b.Key == "641-720" && b.Count == 1);
            paBreakdown.NumericBuckets.Should().Contain(b => b.Key == "721-960" && b.Count == 1);
            paBreakdown.TopEntitiesLabel.Should().Be("Top entidades / empresas receptoras");
            paBreakdown.TopEntities.First().Should().Be(new CategoryCount("Drummond Ltd", "Drummond Ltd", 3));

            // PG Breakdown
            var pgBreakdown = await repo.GetModalityBreakdownAsync(new DashboardFilter { ModalityCodes = new List<string> { "PROYECTO_GRADO" } });
            pgBreakdown.Should().NotBeNull();
            pgBreakdown!.TopEntitiesLabel.Should().Be("Top sublíneas de investigación");
            pgBreakdown.TopEntities.First().Should().Be(new CategoryCount("Arquitectura Cloud", "Arquitectura Cloud", 2));
            pgBreakdown.DefenseSummary.Should().NotBeNull();
            pgBreakdown.DefenseSummary!.TotalScheduled.Should().Be(3);
            pgBreakdown.DefenseSummary.ApprovedFirstAttempt.Should().Be(1);
            pgBreakdown.DefenseSummary.ApprovedSecondAttempt.Should().Be(1);
            pgBreakdown.DefenseSummary.RejectedFinal.Should().Be(1);
            pgBreakdown.DefenseSummary.ByModality.Should().Contain(m => m.Key == "PRESENCIAL" && m.Count == 2);
            pgBreakdown.DefenseSummary.ByModality.Should().Contain(m => m.Key == "VIRTUAL" && m.Count == 1);
        }

        [Fact]
        public async Task GetOperationalAlertsAsync_Detects_Stale_Acuerdo015Sla_TermRisk_Defenses_And_Rework()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // Inscripción 60: PG en AP_EN_EVALUACION hace 45 días, iniciada en 2024-1 (5 periodos >= MaxTerm 3 -> EXCEEDED)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 60, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 1, IdStageModality = 2,
                CreatedAt = now.AddDays(-200), UpdatedAt = now.AddDays(-45), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 60, Title = "Proyecto Estancado", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, CreatedAt = now.AddDays(-200), StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 60, IdStateStage = 3, CreatedAt = now.AddDays(-45), UpdatedAt = now.AddDays(-45), StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 1, IdInscriptionModality = 60, IdUser = 1, IsConfirmed = true, StatusRegister = true });

            // Inscripción 61: PG con sustentación futura programada + jurado asignado + periodo 2025-1 (3 periodos == MaxTerm 3 -> EXCEEDED)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 61, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 4, IdStageModality = 4, // 2025-2 -> 2 periodos == MaxTerm 3 - 1 -> WARNING
                CreatedAt = now.AddDays(-100), UpdatedAt = now.AddDays(-2), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 61, Title = "Proyecto Defensa Futura", IdResearchLine = 1, IdResearchSubLine = 2, IdStateStage = 1, CreatedAt = now.AddDays(-100), StatusRegister = true });
            ctx.Set<ProjectPresentation>().Add(new ProjectPresentation
            {
                Id = 61, IdStateStage = 6, ScheduledDateTime = now.AddDays(5), PresentationModality = "VIRTUAL",
                LocationOrLink = "https://meet.google.com/abc", AttemptNumber = 1,
                CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2), StatusRegister = true
            });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 2, IdInscriptionModality = 61, IdUser = 2, IsConfirmed = true, StatusRegister = true });
            ctx.Set<TeachingAssignment>().Add(new TeachingAssignment
            {
                Id = 10, IdInscriptionModality = 61, IdTeacher = 11, IdTypeTeachingAssignment = 2, StatusRegister = true
            });

            // Retrabajo: 2 evaluaciones con observaciones + 1 documento reemplazado para PG
            ctx.Set<Evaluation>().AddRange(
                new Evaluation { Id = 1, EntityType = "PreliminaryProject", EntityId = 60, IdEvaluationType = 1, IdEvaluator = 11, Result = "AP_CON_OBSERVACIONES", StatusRegister = true },
                new Evaluation { Id = 2, EntityType = "Proposal", EntityId = 61, IdEvaluationType = 1, IdEvaluator = 10, Result = "PROP_NO_PERTINENTE", StatusRegister = true }
            );
            ctx.Set<Document>().AddRange(
                new Document { Id = 1, IdInscriptionModality = 60, IdDocumentType = 1, OriginalFileName = "v1.pdf", StoredFileName = "v1.pdf", StoragePath = "/v1", MimeType = "application/pdf", StatusRegister = true },
                new Document { Id = 2, IdInscriptionModality = 60, IdDocumentType = 1, IdDocumentOld = 1, OriginalFileName = "v2.pdf", StoredFileName = "v2.pdf", StoragePath = "/v2", MimeType = "application/pdf", StatusRegister = true }
            );

            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);
            var alerts = await repo.GetOperationalAlertsAsync(new DashboardFilter { StaleDaysThreshold = 30 });

            // Estancadas + SLA Acuerdo 015
            alerts.TotalStaleCount.Should().Be(1);
            alerts.TotalEvaluationSlaBreachCount.Should().Be(1);
            var stale = alerts.StaleItems.Should().ContainSingle().Subject;
            stale.InscriptionId.Should().Be(60);
            stale.SlaBreachAcuerdo015.Should().BeTrue();
            stale.ResponsibleParty.Should().Be("Jurados / Evaluadores");
            stale.DaysSinceActivity.Should().BeGreaterThanOrEqualTo(44);

            // Riesgo de plazo (MaxTermPeriods)
            alerts.TotalTermRiskCount.Should().Be(2);
            alerts.TermRiskItems.Should().Contain(t => t.InscriptionId == 60 && t.RiskLevel == "EXCEEDED" && t.PeriodsElapsed == 5);
            alerts.TermRiskItems.Should().Contain(t => t.InscriptionId == 61 && t.RiskLevel == "WARNING" && t.PeriodsElapsed == 2);

            // Próximas sustentaciones
            var defense = alerts.UpcomingDefenses.Should().ContainSingle().Subject;
            defense.InscriptionId.Should().Be(61);
            defense.Title.Should().Be("Proyecto Defensa Futura");
            defense.Juries.Should().Contain("María Díaz");
            defense.PresentationModality.Should().Be("Virtual");

            // Retrabajo
            var pgRework = alerts.ReworkByModality.Should().ContainSingle(r => r.ModalityCode == "PROYECTO_GRADO").Subject;
            pgRework.TotalInscriptions.Should().Be(2);
            pgRework.ObservationEvaluations.Should().Be(2);
            pgRework.AvgObservationsPerInscription.Should().Be(1.0);
            pgRework.ReplacedDocumentsCount.Should().Be(1);
        }

        [Fact]
        public async Task GetOperationalAlertsAsync_TermRisk_Ignores_FirstPeriod_Of_TwoPeriodModality_And_Classifies_Warning_Vs_Exceeded()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // PRACTICA_ACADEMICA tiene MaximumTermPeriods = 2:
            // - Periodo 5 (2026-1): 1/2 -> NO debe alertarse
            // - Periodo 4 (2025-2): 2/2 -> WARNING (último periodo permitido)
            // - Periodo 3 (2025-1): 3/2 -> EXCEEDED
            ctx.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 80, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 5, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new InscriptionModality { Id = 81, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 4, IdStageModality = 5, CreatedAt = now.AddDays(-120), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new InscriptionModality { Id = 82, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 5, CreatedAt = now.AddDays(-240), UpdatedAt = now.AddDays(-1), StatusRegister = true }
            );
            ctx.Set<AcademicPractice>().AddRange(
                new AcademicPractice { Id = 80, IdStateStage = 11, Title = "PA 1/2", CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new AcademicPractice { Id = 81, IdStateStage = 11, Title = "PA 2/2", CreatedAt = now.AddDays(-120), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new AcademicPractice { Id = 82, IdStateStage = 11, Title = "PA 3/2", CreatedAt = now.AddDays(-240), UpdatedAt = now.AddDays(-1), StatusRegister = true }
            );

            // PROYECTO_GRADO tiene MaximumTermPeriods = 3:
            // - Periodo 5 (2026-1): 1/3 -> NO debe alertarse
            // - Periodo 4 (2025-2): 2/3 -> WARNING (maxTerm >= 3 && elapsed == maxTerm - 1)
            // - Periodo 3 (2025-1): 3/3 -> WARNING (último periodo permitido)
            // - Periodo 2 (2024-2): 4/3 -> EXCEEDED
            ctx.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 83, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 1, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new InscriptionModality { Id = 84, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 4, IdStageModality = 1, CreatedAt = now.AddDays(-120), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new InscriptionModality { Id = 85, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 3, IdStageModality = 1, CreatedAt = now.AddDays(-240), UpdatedAt = now.AddDays(-1), StatusRegister = true },
                new InscriptionModality { Id = 86, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 2, IdStageModality = 1, CreatedAt = now.AddDays(-360), UpdatedAt = now.AddDays(-1), StatusRegister = true }
            );
            ctx.Set<Proposal>().AddRange(
                new Proposal { Id = 83, Title = "PG 1/3", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true },
                new Proposal { Id = 84, Title = "PG 2/3", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true },
                new Proposal { Id = 85, Title = "PG 3/3", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true },
                new Proposal { Id = 86, Title = "PG 4/3", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true }
            );

            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);
            var alerts = await repo.GetOperationalAlertsAsync(new DashboardFilter());

            alerts.TotalTermRiskCount.Should().Be(5);
            alerts.TermRiskItems.Should().NotContain(t => t.InscriptionId == 80); // 1/2 no alertado
            alerts.TermRiskItems.Should().NotContain(t => t.InscriptionId == 83); // 1/3 no alertado

            alerts.TermRiskItems.Should().ContainSingle(t => t.InscriptionId == 81 && t.PeriodsElapsed == 2 && t.MaxTermPeriods == 2 && t.RiskLevel == "WARNING");
            alerts.TermRiskItems.Should().ContainSingle(t => t.InscriptionId == 82 && t.PeriodsElapsed == 3 && t.MaxTermPeriods == 2 && t.RiskLevel == "EXCEEDED");
            alerts.TermRiskItems.Should().ContainSingle(t => t.InscriptionId == 84 && t.PeriodsElapsed == 2 && t.MaxTermPeriods == 3 && t.RiskLevel == "WARNING");
            alerts.TermRiskItems.Should().ContainSingle(t => t.InscriptionId == 85 && t.PeriodsElapsed == 3 && t.MaxTermPeriods == 3 && t.RiskLevel == "WARNING");
            alerts.TermRiskItems.Should().ContainSingle(t => t.InscriptionId == 86 && t.PeriodsElapsed == 4 && t.MaxTermPeriods == 3 && t.RiskLevel == "EXCEEDED");
        }

        [Fact]
        public async Task GetPagedRecordsAsync_And_GetExportRowsAsync_Support_Search_Pagination_Sorting_And_AllModality_Details()
        {
            using var ctx = CreateInMemoryContext();
            SeedCatalog(ctx);
            var now = DateTimeOffset.UtcNow;

            // 1. PG (Ana Gómez, teacher Carlos Ríos)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 70, IdModality = 1, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 2,
                CreatedAt = now.AddDays(-30), StatusRegister = true
            });
            ctx.Set<Proposal>().Add(new Proposal { Id = 70, Title = "Plataforma Cloud PEGI", IdResearchLine = 1, IdResearchSubLine = 1, IdStateStage = 1, StatusRegister = true });
            ctx.Set<PreliminaryProject>().Add(new PreliminaryProject { Id = 70, IdStateStage = 3, StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 10, IdInscriptionModality = 70, IdUser = 1, IsConfirmed = true, StatusRegister = true });
            ctx.Set<TeachingAssignment>().Add(new TeachingAssignment { Id = 20, IdInscriptionModality = 70, IdTeacher = 10, IdTypeTeachingAssignment = 1, StatusRegister = true });

            // 2. SABER_PRO (Luis Martínez)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 71, IdModality = 7, IdStateInscription = 4, IdAcademicPeriod = 5, IdStageModality = 10,
                CreatedAt = now.AddDays(-10), StatusRegister = true
            });
            ctx.Set<SaberPro>().Add(new SaberPro { Id = 71, IdStateStage = 17, ResultQuintile = "Q5", ResultScore = 220m, StatusRegister = true });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 11, IdInscriptionModality = 71, IdUser = 2, IsConfirmed = true, StatusRegister = true });

            // 3. PRACTICA_ACADEMICA (Luis Martínez)
            ctx.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 72, IdModality = 2, IdStateInscription = 2, IdAcademicPeriod = 5, IdStageModality = 5,
                CreatedAt = now.AddDays(-5), StatusRegister = true
            });
            ctx.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = 72, IdStateStage = 11, Title = "Automatización ERP", InstitutionName = "Bancolombia",
                PracticeHours = 720, IsEmprendimiento = false, StatusRegister = true
            });
            ctx.Set<UserInscriptionModality>().Add(new UserInscriptionModality { Id = 12, IdInscriptionModality = 72, IdUser = 2, IsConfirmed = true, StatusRegister = true });

            await ctx.SaveChangesAsync();

            var repo = new DashboardRepository(ctx);

            // Paginación + Ordenamiento
            var page1 = await repo.GetPagedRecordsAsync(new DashboardFilter { Page = 1, PageSize = 2, SortBy = "id", SortDir = "asc" });
            page1.TotalCount.Should().Be(3);
            page1.Items.Should().HaveCount(2);
            page1.Items.Select(x => x.InscriptionId).Should().ContainInOrder(70, 71);

            var page2 = await repo.GetPagedRecordsAsync(new DashboardFilter { Page = 2, PageSize = 2, SortBy = "id", SortDir = "asc" });
            page2.TotalCount.Should().Be(3);
            page2.Items.Should().ContainSingle(x => x.InscriptionId == 72);

            // Búsqueda por título / docente / empresa
            var searchByTitle = await repo.GetPagedRecordsAsync(new DashboardFilter { Search = "cloud pegi" });
            searchByTitle.TotalCount.Should().Be(1);
            searchByTitle.Items[0].InscriptionId.Should().Be(70);

            var searchByInstitution = await repo.GetPagedRecordsAsync(new DashboardFilter { Search = "bancolombia" });
            searchByInstitution.TotalCount.Should().Be(1);
            searchByInstitution.Items[0].InscriptionId.Should().Be(72);

            // GetExportRowsAsync SIN filtro de modalidad (ModalityCodes = null) DEBE poblar PhaseState y ModalityDetail para TODAS las modalidades
            var allExport = await repo.GetExportRowsAsync(new DashboardFilter());
            allExport.Should().HaveCount(3);

            var pgRow = allExport.Single(r => r.InscriptionId == 70);
            pgRow.PhaseState.Should().Be("Anteproyecto en Evaluación");
            pgRow.ModalityDetail.Should().Contain("Plataforma Cloud PEGI");
            pgRow.ResearchLine.Should().Be("Ingeniería de Software");
            pgRow.ResearchSubLine.Should().Be("Arquitectura Cloud");
            pgRow.Teachers.Should().Contain("Carlos Ríos (Director)");
            pgRow.StudentIdentifications.Should().Be("10650001");
            pgRow.StudentEmails.Should().Be("ana@unicesar.edu.co");

            var spRow = allExport.Single(r => r.InscriptionId == 71);
            spRow.PhaseState.Should().Be("Saber Pro Aprobado");
            spRow.ModalityDetail.Should().Contain("Quintil: Q5").And.Contain("220");
            spRow.LifecycleCode.Should().Be("COMPLETED");

            var paRow = allExport.Single(r => r.InscriptionId == 72);
            paRow.PhaseState.Should().Be("Informe Final PA en Revisión");
            paRow.ModalityDetail.Should().Contain("Práctica").And.Contain("Bancolombia").And.Contain("720 h");
        }

        [Fact]
        public void Npgsql_SqlTranslation_CriticalExpressions_Compile_Cleanly_To_PostgresSql()
        {
            // Usa el compilador real de Npgsql 10 SIN abrir conexión para garantizar 100% traducibilidad SQL.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=127.0.0.1;Database=pegi_translation_check;Username=test;Password=test")
                .Options;

            using var ctx = new AppDbContext(options);
            var nowUtc = DateTimeOffset.UtcNow;

            // 1. Timeline Colombia (UTC-5)
            var timelineSql = ctx.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister)
                .Select(i => i.CreatedAt.UtcDateTime.AddHours(-5))
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToQueryString();
            timelineSql.Should().Contain("GROUP BY");

            // 2. Correlated AcademicPeriod count (TermRisk)
            var termSql = ctx.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister && i.AcademicPeriod != null && i.Modality != null && i.Modality.MaximumTermPeriods > 0)
                .Select(i => new
                {
                    i.Id,
                    RawPeriods = ctx.Set<AcademicPeriod>()
                        .Count(ap => ap.StatusRegister && ap.StartDate >= i.AcademicPeriod!.StartDate && ap.StartDate <= nowUtc)
                })
                .ToQueryString();
            termSql.Should().Contain("AcademicPeriod");

            // 3. Cycle days difference + SQL Average aggregate
            var cycleDaysSql = ctx.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister && i.UpdatedAt != null)
                .Select(i => (i.UpdatedAt!.Value - i.CreatedAt).TotalDays)
                .Select(d => d < 0 ? 0.0 : d)
                .GroupBy(_ => 1)
                .Select(g => new { Avg = g.Average() })
                .ToQueryString();
            cycleDaysSql.Should().ContainEquivalentOf("avg(");

            // 4. Rework joins (Evaluation + Document)
            var baseIdModality = ctx.Set<InscriptionModality>()
                .AsNoTracking()
                .Where(i => i.StatusRegister && i.Modality != null)
                .Select(i => new { i.Id, ModalityCode = i.Modality!.Code });

            var evalReworkSql = ctx.Set<Evaluation>()
                .AsNoTracking()
                .Where(e => e.StatusRegister && e.Result != null &&
                    (e.Result.ToUpper().Contains("OBSERV")
                     || e.Result.ToUpper().Contains("NO_PERTINENTE")
                     || e.Result.ToUpper().Contains("REINTENTO")))
                .Join(baseIdModality, e => e.EntityId, i => i.Id, (e, i) => i.ModalityCode)
                .GroupBy(c => c)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToQueryString();
            evalReworkSql.Should().Contain("JOIN").And.Contain("GROUP BY");

            // 5. 1:1 Navigation properties on InscriptionModality (PG 4 phases)
            var pgSql = ctx.Set<InscriptionModality>()
                .AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    Prop = i.Proposal != null ? i.Proposal.StateStage.Code : null,
                    Prelim = i.PreliminaryProject != null && i.PreliminaryProject.StateStage != null ? i.PreliminaryProject.StateStage.Code : null,
                    Final = i.ProjectFinal != null && i.ProjectFinal.StateStage != null ? i.ProjectFinal.StateStage.Code : null,
                    Pres = i.ProjectPresentation != null && i.ProjectPresentation.StateStage != null ? i.ProjectPresentation.StateStage.Code : null
                })
                .ToQueryString();
            pgSql.Should().Contain("PreliminaryProject").And.Contain("ProjectFinal").And.Contain("ProjectPresentation");
        }
    }
}
