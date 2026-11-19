using Application.Shared.Mappings;
using Application.Shared.Queries.AcademicAverages;
using Application.Shared.Queries.AcademicAverages.Handlers;
using Application.Shared.Queries.AcademicPractices;
using Application.Shared.Queries.AcademicPractices.Handlers;
using Application.Shared.Queries.CoTerminals;
using Application.Shared.Queries.CoTerminals.Handlers;
using Application.Shared.Queries.InscriptionWithStudents;
using Application.Shared.Queries.InscriptionWithStudents.Handlers;
using Application.Shared.Queries.PreliminaryProjects;
using Application.Shared.Queries.PreliminaryProjects.Handlers;
using Application.Shared.Queries.ProjectFinals;
using Application.Shared.Queries.ProjectFinals.Handlers;
using Application.Shared.Queries.ProjectPresentations;
using Application.Shared.Queries.ProjectPresentations.Handlers;
using Application.Shared.Queries.Proposals;
using Application.Shared.Queries.Proposals.Handlers;
using Application.Shared.Queries.SaberPros;
using Application.Shared.Queries.SaberPros.Handlers;
using Application.Shared.Queries.ScientificArticles;
using Application.Shared.Queries.ScientificArticles.Handlers;
using Application.Shared.Queries.Seminars;
using Application.Shared.Queries.Seminars.Handlers;
using Application.Shared.Queries.UserRoles;
using Application.Shared.Queries.UserRoles.Handlers;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Queries
{
    public class KeysetCursorPropagationTests
    {
        private static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<GenericProfile>();
                cfg.AddProfile<AcademicPracticeProfile>();
                cfg.AddProfile<MinorModalitiesProfile>();
            }, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        private static async Task<(DateTimeOffset T1, DateTimeOffset T2, int TeacherId)> SeedAllModalitiesAsync(AppDbContext context)
        {
            var t1 = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
            var t2 = new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero);
            const int teacherId = 500;

            context.Set<Modality>().Add(new Modality { Id = 1, Code = "MOD", Name = "Modalidad", StatusRegister = true });
            context.Set<StateInscription>().Add(new StateInscription { Id = 1, Code = "APROBADO", Name = "Aprobado", StatusRegister = true });
            context.Set<AcademicPeriod>().Add(new AcademicPeriod { Id = 1, Code = "2026-1", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(4), StatusRegister = true });
            context.Set<StageModality>().Add(new StageModality { Id = 1, Code = "STG_1", Name = "Etapa 1", IdModality = 1, StageOrder = 1, StatusRegister = true });
            context.Set<StateStage>().Add(new StateStage { Id = 1, Code = "SS_1", Name = "Estado 1", IdStageModality = 1, StatusRegister = true });
            context.Set<ResearchLine>().Add(new ResearchLine { Id = 1, Name = "Linea 1", Description = "Desc", StatusRegister = true });
            context.Set<ResearchSubLine>().Add(new ResearchSubLine { Id = 1, Name = "Sublinea 1", Description = "Desc", IdResearchLine = 1, StatusRegister = true });
            context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment { Id = 1, Code = "JURADO_EVALUADOR", Name = "Jurado", StatusRegister = true });
            context.Set<User>().Add(new User
            {
                Id = teacherId,
                FirstName = "Docente",
                LastName = "Prueba",
                Email = "docente@test.edu",
                Identification = "999999",
                StatusRegister = true
            });

            // Tres inscripciones: Id=1 (T1), Id=2 (T2), Id=3 (T2) -> orden esperado (CreatedAt DESC, Id DESC): 3, 2, 1
            var specs = new[]
            {
                (Id: 1, CreatedAt: t1),
                (Id: 2, CreatedAt: t2),
                (Id: 3, CreatedAt: t2)
            };

            foreach (var s in specs)
            {
                context.Set<InscriptionModality>().Add(new InscriptionModality
                {
                    Id = s.Id,
                    IdModality = 1,
                    IdStateInscription = 1,
                    IdAcademicPeriod = 1,
                    IdStageModality = 1,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    Id = s.Id,
                    IdInscriptionModality = s.Id,
                    IdTeacher = teacherId,
                    IdTypeTeachingAssignment = 1,
                    IsConfirmed = true,
                    StatusRegister = true
                });

                context.Set<Proposal>().Add(new Proposal
                {
                    Id = s.Id,
                    Title = $"Propuesta {s.Id}",
                    IdResearchLine = 1,
                    IdResearchSubLine = 1,
                    IdStateStage = 1,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<PreliminaryProject>().Add(new PreliminaryProject
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<ProjectFinal>().Add(new ProjectFinal
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<ProjectPresentation>().Add(new ProjectPresentation
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    AttemptNumber = 1,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<AcademicPractice>().Add(new AcademicPractice
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    Title = $"Practica {s.Id}",
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<AcademicAverage>().Add(new AcademicAverage
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    CertifiedAverage = 4.5m,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<CoTerminal>().Add(new CoTerminal
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    UniversityName = $"Uni {s.Id}",
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<SaberPro>().Add(new SaberPro
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    ResultScore = 200,
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<ScientificArticle>().Add(new ScientificArticle
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    ArticleTitle = $"Articulo {s.Id}",
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });

                context.Set<Seminar>().Add(new Seminar
                {
                    Id = s.Id,
                    IdStateStage = 1,
                    SeminarName = $"Seminario {s.Id}",
                    CreatedAt = s.CreatedAt,
                    StatusRegister = true
                });
            }

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return (t1, t2, teacherId);
        }

        private static void AssertCursorMatches<T>(PaginatedResult<T> page, long expectedCursorId, DateTimeOffset expectedCursorCreatedAt)
        {
            page.Items.Should().HaveCount(2);
            page.CursorId.Should().Be(expectedCursorId);
            page.CursorCreatedAt.Should().Be(expectedCursorCreatedAt);
        }

        [Fact]
        public async Task All11WithDetailsRepositoriesAndHandlers_PropagateCursorIdAndCursorCreatedAt()
        {
            using var context = CreateInMemoryContext();
            var (t1, t2, teacherId) = await SeedAllModalitiesAsync(context);
            var mapper = CreateMapper();
            var emptyFilters = new Dictionary<string, string>();

            // 1. Proposal (GetAll + GetByTeacher)
            var proposalRepo = new ProposalRepository(context);
            var propGetAllHandler = new GetAllProposalsHandler(NullLogger<GetAllProposalsHandler>.Instance, proposalRepo, mapper);
            var propGetTeacherHandler = new GetProposalsByTeacherQueryHandler(NullLogger<GetProposalsByTeacherQueryHandler>.Instance, proposalRepo, mapper);

            var propAll = await propGetAllHandler.Handle(new GetAllProposalsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None);
            AssertCursorMatches(propAll, 2, t2);
            var propAllPage2 = await propGetAllHandler.Handle(new GetAllProposalsQuery { PageNumber = 1, PageSize = 2, CursorId = propAll.CursorId, CursorCreatedAt = propAll.CursorCreatedAt, SkipTotalCount = true }, CancellationToken.None);
            propAllPage2.Items.Should().ContainSingle(x => x.Proposal.Id == 1);
            propAllPage2.CursorId.Should().Be(1);
            propAllPage2.CursorCreatedAt.Should().Be(t1);

            var propTeacher = await propGetTeacherHandler.Handle(new GetProposalsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None);
            AssertCursorMatches(propTeacher, 2, t2);

            // 2. PreliminaryProject
            var prelimRepo = new PreliminaryProjectRepository(context);
            var prelimAllHandler = new GetAllPreliminaryProjectsHandler(prelimRepo);
            var prelimTeacherHandler = new GetPreliminaryProjectsByTeacherHandler(prelimRepo);
            AssertCursorMatches(await prelimAllHandler.Handle(new GetAllPreliminaryProjectsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await prelimTeacherHandler.Handle(new GetPreliminaryProjectsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 3. ProjectFinal
            var finalRepo = new ProjectFinalRepository(context);
            var finalAllHandler = new GetAllProjectFinalsHandler(finalRepo);
            var finalTeacherHandler = new GetProjectFinalsByTeacherHandler(finalRepo);
            AssertCursorMatches(await finalAllHandler.Handle(new GetAllProjectFinalsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await finalTeacherHandler.Handle(new GetProjectFinalsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 4. ProjectPresentation (verifica también IdInscriptionModality = presentation.Id)
            var presRepo = new ProjectPresentationRepository(context);
            var presAllHandler = new GetAllProjectPresentationsHandler(presRepo);
            var presTeacherHandler = new GetProjectPresentationsByTeacherHandler(presRepo);
            var presAll = await presAllHandler.Handle(new GetAllProjectPresentationsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None);
            AssertCursorMatches(presAll, 2, t2);
            presAll.Items.Should().OnlyContain(x => x.ProjectPresentation.IdInscriptionModality == x.ProjectPresentation.Id);
            AssertCursorMatches(await presTeacherHandler.Handle(new GetProjectPresentationsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 5. AcademicPractice
            var apRepo = new AcademicPracticeRepository(context);
            var apAllHandler = new GetAllAcademicPracticesQueryHandler(apRepo, mapper);
            var apTeacherHandler = new GetAcademicPracticesByTeacherQueryHandler(apRepo, mapper);
            AssertCursorMatches(await apAllHandler.Handle(new GetAllAcademicPracticesQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await apTeacherHandler.Handle(new GetAcademicPracticesByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 6. AcademicAverage
            var aaRepo = new AcademicAverageRepository(context);
            var aaAllHandler = new GetAllAcademicAveragesHandler(aaRepo, mapper);
            var aaTeacherHandler = new GetAcademicAveragesByTeacherHandler(aaRepo, mapper);
            AssertCursorMatches(await aaAllHandler.Handle(new GetAllAcademicAveragesQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await aaTeacherHandler.Handle(new GetAcademicAveragesByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 7. CoTerminal (incluye verificación de desempate estable ThenByDescending(x => x.Id) en GetByTeacherAsync: orden 3, 2, 1)
            var ctRepo = new CoTerminalRepository(context);
            var ctAllHandler = new GetAllCoTerminalsHandler(ctRepo, mapper);
            var ctTeacherHandler = new GetCoTerminalsByTeacherHandler(ctRepo, mapper);
            AssertCursorMatches(await ctAllHandler.Handle(new GetAllCoTerminalsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            var ctTeacher = await ctTeacherHandler.Handle(new GetCoTerminalsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None);
            AssertCursorMatches(ctTeacher, 2, t2);
            ctTeacher.Items.Select(x => x.CoTerminal!.Id).Should().Equal(3, 2);

            // 8. SaberPro
            var spRepo = new SaberProRepository(context);
            var spAllHandler = new GetAllSaberProsHandler(spRepo, mapper);
            var spTeacherHandler = new GetSaberProsByTeacherHandler(spRepo, mapper);
            AssertCursorMatches(await spAllHandler.Handle(new GetAllSaberProsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await spTeacherHandler.Handle(new GetSaberProsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 9. ScientificArticle
            var saRepo = new ScientificArticleRepository(context);
            var saAllHandler = new GetAllScientificArticlesHandler(saRepo, mapper);
            var saTeacherHandler = new GetScientificArticlesByTeacherHandler(saRepo, mapper);
            AssertCursorMatches(await saAllHandler.Handle(new GetAllScientificArticlesQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await saTeacherHandler.Handle(new GetScientificArticlesByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 10. Seminar
            var semRepo = new SeminarRepository(context);
            var semAllHandler = new GetAllSeminarsHandler(semRepo, mapper);
            var semTeacherHandler = new GetSeminarsByTeacherHandler(semRepo, mapper);
            AssertCursorMatches(await semAllHandler.Handle(new GetAllSeminarsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
            AssertCursorMatches(await semTeacherHandler.Handle(new GetSeminarsByTeacherQuery(teacherId, 1, 2, "CreatedAt", true, emptyFilters), CancellationToken.None), 2, t2);

            // 11. InscriptionWithStudents
            var inscRepo = new InscriptionWithStudentsRepository(context);
            var inscHandler = new GetAllInscriptionWithStudentsHandler(inscRepo, NullLogger<GetAllInscriptionWithStudentsHandler>.Instance);
            AssertCursorMatches(await inscHandler.Handle(new GetAllInscriptionWithStudentsQuery { PageNumber = 1, PageSize = 2, SortBy = "CreatedAt", IsDescending = true }, CancellationToken.None), 2, t2);
        }

        [Fact]
        public async Task GetUserRolesByRoleCodeQueryHandler_PropagatesCursorAndHasMoreRows()
        {
            var cursorAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
            var userRoleRepoMock = new Mock<IUserRoleRepository>();
            userRoleRepoMock
                .Setup(r => r.GetUserRolesWithUserDetailsAsync(
                    "STUDENT", 1, 2, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<UserRole>
                {
                    Items =
                    [
                        new UserRole { Id = 10, IdUser = 1, IdRole = 1, StatusRegister = true },
                        new UserRole { Id = 9, IdUser = 2, IdRole = 1, StatusRegister = true }
                    ],
                    TotalRecords = -1,
                    PageNumber = 1,
                    PageSize = 2,
                    HasMoreRows = true,
                    CursorCreatedAt = cursorAt,
                    CursorId = 9
                });

            var handler = new GetUserRolesByRoleCodeQueryHandler(
                userRoleRepoMock.Object,
                Mock.Of<ITeachingAssignmentRepository>(),
                Mock.Of<IRepository<TeacherResearchProfile, int>>(),
                CreateMapper(),
                NullLogger<GetUserRolesByRoleCodeQueryHandler>.Instance);

            var result = await handler.Handle(new GetUserRolesByRoleCodeQuery { RoleCode = "STUDENT", PageNumber = 1, PageSize = 2 }, CancellationToken.None);

            result.HasMoreRows.Should().BeTrue();
            result.CursorId.Should().Be(9);
            result.CursorCreatedAt.Should().Be(cursorAt);
        }
    }
}
