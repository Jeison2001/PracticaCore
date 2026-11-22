using Application.Common.Services;
using Application.Common.Services.Notifications;
using Application.Common.Services.Notifications.Builders;
using Application.Common.Services.Notifications.Handlers;
using Application.Features.AcademicPractices.EventHandlers;
using Application.Features.MinorModalities.EventHandlers;
using Application.Features.Proposals.EventHandlers;
using Application.Features.Security.EventHandlers;
using Application.Shared.Commands.InscriptionWithStudents;
using Application.Shared.Commands.InscriptionWithStudents.Handlers;
using Application.Shared.Commands.UserRoles;
using Application.Shared.Commands.UserRoles.Handlers;
using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.DTOs.UserInscriptionModalities;
using Application.Shared.DTOs.UserRoles;
using Application.Shared.Mappings;
using Application.Shared.Queries.PreliminaryProjects;
using Application.Shared.Queries.PreliminaryProjects.Handlers;
using Application.Shared.Queries.ProjectFinals;
using Application.Shared.Queries.ProjectFinals.Handlers;
using AutoMapper;
using Domain.Common;
using Domain.Common.Notifications;
using Domain.Common.Teachers;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Jobs;
using Domain.Interfaces.Services.Notifications;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Services
{
    public class AuditFixesRegressionTests
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
            var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(GenericProfile).Assembly), NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        [Fact]
        public async Task H01_UserChangeHandler_ResolvesFrontendUrl_FallsBackToAllowedOrigins_AndDefaultLocalhost()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            var captured = new List<Dictionary<string, object>>();
            var queueMock = new Mock<IEmailNotificationQueueService>();
            queueMock.Setup(q => q.EnqueueEventNotification("USER_CREATED", It.IsAny<Dictionary<string, object>>()))
                .Callback<string, Dictionary<string, object>>((_, d) => captured.Add(d))
                .Returns("job-1");

            var user = new User { Id = 10, Email = "est@unicesar.edu.co", FirstName = "Juan", LastName = "Perez" };

            // 1. App:FrontendUrl con slash final
            var cfgFrontend = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:FrontendUrl"] = "https://pegi.unicesar.edu.co/"
                })
                .Build();
            await new UserChangeHandler(queueMock.Object, uow, NullLogger<UserChangeHandler>.Instance, cfgFrontend)
                .HandleCreationAsync(user);
            captured.Last()["LoginUrl"].Should().Be("https://pegi.unicesar.edu.co/iniciar-sesion");

            // 2. Fallback a AllowedOrigins[0]
            var cfgOrigins = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AllowedOrigins:0"] = "https://app.pegi.edu.co/"
                })
                .Build();
            await new UserChangeHandler(queueMock.Object, uow, NullLogger<UserChangeHandler>.Instance, cfgOrigins)
                .HandleCreationAsync(user);
            captured.Last()["LoginUrl"].Should().Be("https://app.pegi.edu.co/iniciar-sesion");

            // 3. Fallback a localhost:4200
            await new UserChangeHandler(queueMock.Object, uow, NullLogger<UserChangeHandler>.Instance)
                .HandleCreationAsync(user);
            captured.Last()["LoginUrl"].Should().Be("http://localhost:4200/iniciar-sesion");
        }

        [Fact]
        public async Task H03_UserRoleAndApprovalHandlers_PreserveRealAuditUserId_OrNullWithoutMagicOnesOrZeros()
        {
            using var context = CreateInMemoryContext();
            var studentRole = new Role { Id = 2, Code = "STUDENT", Name = "Estudiante", StatusRegister = true };
            var perm = new Permission { Id = 10, Code = "N1MG", StatusRegister = true };
            context.Set<Role>().Add(studentRole);
            context.Set<Permission>().Add(perm);
            context.Set<RolePermission>().Add(new RolePermission { Id = 1, IdRole = 2, IdPermission = 10, StatusRegister = true });
            await context.SaveChangesAsync();

            var assignPermHandler = new AssignPermissionsOnRoleAssignedHandler(
                new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance),
                NullLogger<AssignPermissionsOnRoleAssignedHandler>.Instance);

            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns<object, CancellationToken>(async (evt, ct) =>
                {
                    if (evt is UserRoleAssignedEvent roleEvt)
                    {
                        await assignPermHandler.Handle(roleEvt, ct);
                    }
                });

            var uow = new UnitOfWork(context, mediatorMock.Object, NullLogger<UnitOfWork>.Instance);
            var cmdHandler = new CreateUserRoleWithCargosCommandHandler(
                uow.GetRepository<UserRole, int>(),
                uow.GetRepository<Role, int>(),
                Mock.Of<ITeacherEnabledCargoRepository>(),
                CreateMapper(),
                uow);

            // 1. Admin real (UserId = 77) -> UserRole.IdUserCreatedAt == 77 y UserPermission.IdUserCreatedAt == 77 (no 1)
            await cmdHandler.Handle(
                new CreateUserRoleWithCargosCommand
                {
                    Dto = new UserRoleDto { IdUser = 500, IdRole = 2, StatusRegister = true },
                    CurrentUser = new CurrentUserInfo { UserId = 77 }
                },
                CancellationToken.None);

            var perm500 = await context.Set<UserPermission>().SingleAsync(up => up.IdUser == 500);
            perm500.IdUserCreatedAt.Should().Be(77);

            // 2. Origen sistema (UserId = null) -> TriggeredByUserId == null y UserPermission.IdUserCreatedAt == null (no 1)
            await cmdHandler.Handle(
                new CreateUserRoleWithCargosCommand
                {
                    Dto = new UserRoleDto { IdUser = 501, IdRole = 2, StatusRegister = true },
                    CurrentUser = new CurrentUserInfo { UserId = null }
                },
                CancellationToken.None);

            var perm501 = await context.Set<UserPermission>().SingleAsync(up => up.IdUser == 501);
            perm501.IdUserCreatedAt.Should().BeNull();

            // 3. ObservationHistoryHelper con evaluatorId <= 0 no inserta Evaluation inválida (vía PatchInscriptionModalityCommandHandler)
            context.Set<EvaluationType>().Add(new EvaluationType { Id = 1, Code = EvaluationTypeCodes.InscriptionModality, Name = "IM", StatusRegister = true });
            context.Set<InscriptionModality>().Add(new InscriptionModality { Id = 899, IdModality = 1, IdStateInscription = 1, StatusRegister = true });
            await context.SaveChangesAsync();

            var patchInscHandler = new global::Application.Shared.Commands.InscriptionModalities.Handlers.PatchInscriptionModalityCommandHandler(
                uow.GetRepository<InscriptionModality, int>(),
                uow,
                CreateMapper(),
                Mock.Of<IJobEnqueuer>(),
                NullLogger<global::Application.Shared.Commands.InscriptionModalities.Handlers.PatchInscriptionModalityCommandHandler>.Instance);

            await patchInscHandler.Handle(
                new global::Application.Shared.Commands.InscriptionModalities.PatchInscriptionModalityCommand(
                    899,
                    new InscriptionModalityPatchDto { Observations = "Observación sin evaluador válido" },
                    new CurrentUserInfo { UserId = 0 }),
                CancellationToken.None);

            (await context.Set<Evaluation>().CountAsync()).Should().Be(0);

            // 4. InscriptionModality + *OnApprovalHandlers guardan null cuando TriggeredByUserId <= 0 o null
            var inscEntity = new InscriptionModality { Id = 900, IdModality = 2, IdStateInscription = 1 };
            inscEntity.IdStateInscription = 2; // sin IdUserUpdatedAt ni IdUserCreatedAt
            inscEntity.DomainEvents.OfType<InscriptionStateChangedEvent>().Single().TriggeredByUserId.Should().BeNull();

            context.Set<Modality>().AddRange(
                new Modality { Id = 1, Code = ModalityCodes.ProyectoGrado, Name = "PG", RequiresApproval = true, StatusRegister = true },
                new Modality { Id = 2, Code = ModalityCodes.PracticaAcademica, Name = "PA", RequiresApproval = true, StatusRegister = true },
                new Modality { Id = 3, Code = ModalityCodes.CoTerminal, Name = "CT", RequiresApproval = true, StatusRegister = true });
            context.Set<StateInscription>().Add(new StateInscription { Id = 2, Code = StateInscriptionCodes.Aprobado, Name = "Aprobado", StatusRegister = true });
            context.Set<StageModality>().AddRange(
                new StageModality { Id = 10, IdModality = 1, Code = StageModalityCodes.PgFasePropuesta, StageOrder = 1, StatusRegister = true },
                new StageModality { Id = 20, IdModality = 2, Code = StageModalityCodes.PaFaseInscripcion, StageOrder = 1, StatusRegister = true },
                new StageModality { Id = 30, IdModality = 3, Code = "CTF1", StageOrder = 1, StatusRegister = true });
            context.Set<StateStage>().AddRange(
                new StateStage { Id = 200, IdStageModality = 20, Code = StateStageCodes.PaInscripcionPendDoc, IsInitialState = true, StatusRegister = true },
                new StateStage { Id = 300, IdStageModality = 30, Code = "CT_PEND", IsInitialState = true, StatusRegister = true });
            context.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 901, IdModality = 1, IdStateInscription = 2, StatusRegister = true },
                new InscriptionModality { Id = 902, IdModality = 2, IdStateInscription = 2, StatusRegister = true },
                new InscriptionModality { Id = 903, IdModality = 3, IdStateInscription = 2, StatusRegister = true });
            await context.SaveChangesAsync();

            await new StartProposalPhaseOnApprovalHandler(uow, NullLogger<StartProposalPhaseOnApprovalHandler>.Instance)
                .Handle(new InscriptionStateChangedEvent(901, 1, 2, null), CancellationToken.None);
            await new CreateAcademicPracticeOnApprovalHandler(uow, NullLogger<CreateAcademicPracticeOnApprovalHandler>.Instance)
                .Handle(new InscriptionStateChangedEvent(902, 2, 2, 0), CancellationToken.None);
            await new StartMinorModalityPhaseOnApprovalHandler(uow, NullLogger<StartMinorModalityPhaseOnApprovalHandler>.Instance)
                .Handle(new InscriptionStateChangedEvent(903, 3, 2, null), CancellationToken.None);
            await context.SaveChangesAsync();

            (await context.Set<InscriptionModality>().FindAsync(901))!.IdUserUpdatedAt.Should().BeNull();
            (await context.Set<AcademicPractice>().FindAsync(902))!.IdUserCreatedAt.Should().BeNull();
            (await context.Set<CoTerminal>().FindAsync(903))!.IdUserCreatedAt.Should().BeNull();
        }

        [Fact]
        public async Task H06_BuildersAndQueryHandlers_FormatAttendance_ResolveCoTerminalAndRealState_AndMapStudentMetrics()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);
            var studentDataMock = new Mock<IStudentDataService>();
            studentDataMock.Setup(s => s.GetStudentDataByInscriptionAsync(It.IsAny<int>()))
                .ReturnsAsync(("Ana Lopez", "ana@unicesar.edu.co", 1));
            studentDataMock.Setup(s => s.GetStudentDataByUserIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(("Ana Lopez", "ana@unicesar.edu.co", 1));

            context.Set<Modality>().AddRange(
                new Modality { Id = 3, Code = ModalityCodes.CoTerminal, Name = "Co-Terminal", RequiresApproval = true, StatusRegister = true },
                new Modality { Id = 6, Code = ModalityCodes.GradoPromedio, Name = "Grado Promedio", RequiresApproval = false, StatusRegister = true },
                new Modality { Id = 4, Code = ModalityCodes.SeminarioAct, Name = "Seminario", RequiresApproval = true, StatusRegister = true });
            context.Set<AcademicPeriod>().Add(new AcademicPeriod { Id = 1, Code = "2026-2", StatusRegister = true });
            context.Set<StateInscription>().AddRange(
                new StateInscription { Id = 1, Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", StatusRegister = true },
                new StateInscription { Id = 4, Code = StateInscriptionCodes.NoAplica, Name = "No Aplica", StatusRegister = true });
            context.Set<StateStage>().Add(new StateStage { Id = 20, Code = "SA_PEND", Name = "Pendiente", StatusRegister = true });
            context.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 100, IdModality = 3, IdAcademicPeriod = 1, IdStateInscription = 1, StatusRegister = true },
                new InscriptionModality { Id = 101, IdModality = 6, IdAcademicPeriod = 1, IdStateInscription = 4, StatusRegister = true },
                new InscriptionModality { Id = 102, IdModality = 4, IdAcademicPeriod = 1, IdStateInscription = 1, StatusRegister = true },
                new InscriptionModality { Id = 103, IdModality = 4, IdAcademicPeriod = 1, IdStateInscription = 1, StatusRegister = true });
            context.Set<Seminar>().AddRange(
                new Seminar { Id = 102, IdStateStage = 20, AttendancePercentage = null, StatusRegister = true },
                new Seminar { Id = 103, IdStateStage = 20, AttendancePercentage = 88.45m, StatusRegister = true });
            await context.SaveChangesAsync();

            // 1. SeminarEventDataBuilder: null -> "-" (no "%"), valor -> "88.5%" (con invariante/cultura actual F1)
            var seminarBuilder = new SeminarEventDataBuilder(uow, studentDataMock.Object, NullLogger<SeminarEventDataBuilder>.Instance);
            var nullAttData = await seminarBuilder.BuildEventDataAsync(102, "SEMINAR_UPDATED");
            var valAttData = await seminarBuilder.BuildEventDataAsync(103, "SEMINAR_UPDATED");
            nullAttData["Attendance"].Should().Be("-");
            valAttData["Attendance"].Should().Be($"{88.45m:F1}%");

            // 2. InscriptionEventDataBuilder: COTERMINAL resuelve pasos de co-terminalidad y GradoPromedio usa "No Aplica"
            var inscBuilder = new InscriptionEventDataBuilder(uow, studentDataMock.Object, NullLogger<InscriptionEventDataBuilder>.Instance);
            var ctData = await inscBuilder.BuildInscriptionEventDataAsync(100, "INSCRIPTION_APPROVED");
            ctData["NextSteps"].ToString().Should().Contain("co-terminalidad");

            var gpBasicData = await inscBuilder.BuildBasicInscriptionDataAsync(101, 6, 1, [1]);
            gpBasicData["InscriptionState"].Should().Be("No Aplica");

            // 3. Los 6 handlers de consulta mapean CumulativeAverage, ApprovedCredits, TotalAcademicCredits y CurrentAcademicPeriod
            var studentUser = new User
            {
                Id = 301,
                FirstName = "Ana",
                LastName = "Lopez",
                Identification = "1065",
                Email = "ana@unicesar.edu.co",
                CurrentAcademicPeriod = "2026-2",
                CumulativeAverage = 4.42,
                ApprovedCredits = 148,
                TotalAcademicCredits = 160
            };
            var uimList = new List<UserInscriptionModality>
            {
                new() { Id = 1, IdInscriptionModality = 200, IdUser = 301, User = studentUser, IsConfirmed = true, StatusRegister = true }
            };
            var proposal = new Proposal { Id = 200, Title = "Tesis", IdStateStage = 1 };
            var pp = new PreliminaryProject { Id = 200, IdStateStage = 1 };
            var pf = new ProjectFinal { Id = 200, IdStateStage = 1 };

            var ppRepoMock = new Mock<IPreliminaryProjectRepository>();
            ppRepoMock.Setup(r => r.GetAllWithProposalAndStudentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<bool>()))
                .ReturnsAsync(new PaginatedResult<(PreliminaryProject, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = [(pp, proposal, uimList, [])],
                    TotalRecords = 1
                });
            ppRepoMock.Setup(r => r.GetByTeacherIdWithProposalAndStudentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<bool>()))
                .ReturnsAsync(new PaginatedResult<(PreliminaryProject, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = [(pp, proposal, uimList, [])],
                    TotalRecords = 1
                });
            ppRepoMock.Setup(r => r.GetByUserIdWithProposalAndStudentsAsync(301, null))
                .ReturnsAsync([(pp, proposal, uimList, [])]);

            var pfRepoMock = new Mock<IProjectFinalRepository>();
            pfRepoMock.Setup(r => r.GetAllWithProposalAndStudentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<bool>()))
                .ReturnsAsync(new PaginatedResult<(ProjectFinal, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = [(pf, proposal, uimList, [])],
                    TotalRecords = 1
                });
            pfRepoMock.Setup(r => r.GetByTeacherIdWithProposalAndStudentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<long?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<bool>()))
                .ReturnsAsync(new PaginatedResult<(ProjectFinal, Proposal, List<UserInscriptionModality>, List<AssignedTeacher>)>
                {
                    Items = [(pf, proposal, uimList, [])],
                    TotalRecords = 1
                });
            pfRepoMock.Setup(r => r.GetByUserIdWithProposalAndStudentsAsync(301, null))
                .ReturnsAsync([(pf, proposal, uimList, [])]);

            static void AssertStudentMetrics(UserInscriptionModalityDto dto)
            {
                dto.CurrentAcademicPeriod.Should().Be("2026-2");
                dto.CumulativeAverage.Should().Be(4.42);
                dto.ApprovedCredits.Should().Be(148);
                dto.TotalAcademicCredits.Should().Be(160);
            }

            AssertStudentMetrics((await new GetAllPreliminaryProjectsHandler(ppRepoMock.Object).Handle(new GetAllPreliminaryProjectsQuery(), CancellationToken.None)).Items.Single().Proposal.Students.Single());
            AssertStudentMetrics((await new GetPreliminaryProjectsByTeacherHandler(ppRepoMock.Object).Handle(new GetPreliminaryProjectsByTeacherQuery(1, 1, 10, "", false, new()), CancellationToken.None)).Items.Single().Proposal.Students.Single());
            AssertStudentMetrics((await new GetPreliminaryProjectsByUserHandler(ppRepoMock.Object).Handle(new GetPreliminaryProjectsByUserQuery(301), CancellationToken.None)).Single().Proposal.Students.Single());
            AssertStudentMetrics((await new GetAllProjectFinalsHandler(pfRepoMock.Object).Handle(new GetAllProjectFinalsQuery(), CancellationToken.None)).Items.Single().Proposal.Students.Single());
            AssertStudentMetrics((await new GetProjectFinalsByTeacherHandler(pfRepoMock.Object).Handle(new GetProjectFinalsByTeacherQuery(1, 1, 10, "", false, new()), CancellationToken.None)).Items.Single().Proposal.Students.Single());
            AssertStudentMetrics((await new GetProjectFinalsByUserHandler(pfRepoMock.Object).Handle(new GetProjectFinalsByUserQuery(301), CancellationToken.None)).Single().Proposal.Students.Single());
        }

        [Fact]
        public async Task S04_EmailServicesAndInscriptionValidation_EnforceActiveRecordsConfirmedAssignmentsAndFailClosedFacultyFilter()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            context.Set<AcademicProgram>().AddRange(
                new AcademicProgram { Id = 1, Code = "SIS", Name = "Sistemas", IdFaculty = 10, StatusRegister = true },
                new AcademicProgram { Id = 2, Code = "DER", Name = "Derecho", IdFaculty = 20, StatusRegister = true });

            var coordRole = new Role { Id = 1, Code = "COORD", Name = "Coord", StatusRegister = true };
            var inactiveRole = new Role { Id = 2, Code = "OLD_ROLE", Name = "Old", StatusRegister = false };
            var studentRole = new Role { Id = 3, Code = "STUDENT", Name = "Estudiante", StatusRegister = true };
            context.Set<Role>().AddRange(coordRole, inactiveRole, studentRole);

            context.Set<User>().AddRange(
                new User { Id = 1, Email = "coord.ing@unicesar.edu.co", FirstName = "C", LastName = "Ing", Identification = "11", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 2, Email = "coord.der@unicesar.edu.co", FirstName = "C", LastName = "Der", Identification = "22", IdIdentificationType = 1, IdAcademicProgram = 2, StatusRegister = true },
                new User { Id = 3, Email = "coord.inactrole@unicesar.edu.co", FirstName = "C", LastName = "Inact", Identification = "33", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 4, Email = "dir.confirmed@unicesar.edu.co", FirstName = "D", LastName = "Ok", Identification = "44", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 5, Email = "dir.unconfirmed@unicesar.edu.co", FirstName = "D", LastName = "No", Identification = "55", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 6, Email = "dir.revoked@unicesar.edu.co", FirstName = "D", LastName = "Rev", Identification = "66", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 7, Email = "stu.active@unicesar.edu.co", FirstName = "S", LastName = "Act", Identification = "77", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true },
                new User { Id = 8, Email = "stu.inactive@unicesar.edu.co", FirstName = "S", LastName = "Inact", Identification = "88", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = false },
                new User { Id = 9, Email = "stu.inactiverole@unicesar.edu.co", FirstName = "S", LastName = "NoRole", Identification = "99", IdIdentificationType = 1, IdAcademicProgram = 1, StatusRegister = true });

            context.Set<UserRole>().AddRange(
                new UserRole { Id = 1, IdUser = 1, IdRole = 1, StatusRegister = true },
                new UserRole { Id = 2, IdUser = 2, IdRole = 1, StatusRegister = true },
                new UserRole { Id = 3, IdUser = 3, IdRole = 1, StatusRegister = false }, // asignación de rol inactiva
                new UserRole { Id = 4, IdUser = 3, IdRole = 2, StatusRegister = true },  // rol inactivo
                new UserRole { Id = 5, IdUser = 7, IdRole = 3, StatusRegister = true },
                new UserRole { Id = 6, IdUser = 8, IdRole = 3, StatusRegister = true },
                new UserRole { Id = 7, IdUser = 9, IdRole = 3, StatusRegister = false }); // STUDENT inactivo

            context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment { Id = 1, Code = "DIRECTOR", Name = "Director", StatusRegister = true });
            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment { Id = 1, IdInscriptionModality = 50, IdTypeTeachingAssignment = 1, IdTeacher = 4, IsConfirmed = true, RevocationDate = null, StatusRegister = true },
                new TeachingAssignment { Id = 2, IdInscriptionModality = 50, IdTypeTeachingAssignment = 1, IdTeacher = 5, IsConfirmed = false, RevocationDate = null, StatusRegister = true },
                new TeachingAssignment { Id = 3, IdInscriptionModality = 50, IdTypeTeachingAssignment = 1, IdTeacher = 6, IsConfirmed = true, RevocationDate = DateTimeOffset.UtcNow, StatusRegister = true });

            context.Set<EmailNotificationConfig>().Add(new EmailNotificationConfig
            {
                Id = 1,
                EventName = "TEST_EVENT",
                SubjectTemplate = "Sub",
                BodyTemplate = "Body",
                StatusRegister = true
            });
            context.Set<EmailRecipientRule>().AddRange(
                new EmailRecipientRule { Id = 1, EmailNotificationConfigId = 1, RecipientType = "TO", RuleType = "FIXED_EMAIL", RuleValue = "inactive.rule@unicesar.edu.co", StatusRegister = false },
                new EmailRecipientRule { Id = 2, EmailNotificationConfigId = 1, RecipientType = "TO", RuleType = "BY_ROLE", RuleValue = "COORD", Conditions = "{\"FacultyId\": 10}", StatusRegister = true },
                new EmailRecipientRule { Id = 3, EmailNotificationConfigId = 1, RecipientType = "CC", RuleType = "BY_ROLE", RuleValue = "OLD_ROLE", StatusRegister = true },
                new EmailRecipientRule { Id = 4, EmailNotificationConfigId = 1, RecipientType = "BCC", RuleType = "BY_ENTITY_RELATION", RuleValue = "DIRECTOR_ASSIGNED", StatusRegister = true });

            await context.SaveChangesAsync();

            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);
            EmailNotification? sentEmail = null;
            var notifServiceMock = new Mock<INotificationService>();
            notifServiceMock.Setup(n => n.SendEmailAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
                .Callback<EmailNotification, CancellationToken>((e, _) => sentEmail = e)
                .ReturnsAsync(true);

            var eventService = new EmailNotificationEventService(uow, resolver, notifServiceMock.Object, NullLogger<EmailNotificationEventService>.Instance);
            await eventService.ProcessEventAsync("TEST_EVENT", new Dictionary<string, object>
            {
                ["FacultyId"] = 10,
                ["InscriptionModalityId"] = 50
            });

            sentEmail.Should().NotBeNull();
            sentEmail!.To.Should().Be("coord.ing@unicesar.edu.co"); // filtra facultad 10, excluye UserRole inactivo e ignora regla inactiva
            sentEmail.Cc.Should().BeEmpty(); // OLD_ROLE inactivo
            sentEmail.Bcc.Should().Equal("dir.confirmed@unicesar.edu.co"); // excluye no confirmado y revocado

            // Malformed JSON en Conditions -> fail-closed (lista vacía, no envía a otras facultades)
            var malformedRecipients = await resolver.ResolveRecipientsAsync(
                [new EmailRecipientRule { Id = 99, RecipientType = "TO", RuleType = "BY_ROLE", RuleValue = "COORD", Conditions = "{invalid-json", StatusRegister = true }],
                new Dictionary<string, object> { ["FacultyId"] = 10 });
            malformedRecipients.To.Should().BeEmpty();

            // UserService e inscripción grupal: usuario inactivo o con UserRole STUDENT inactivo son rechazados
            var userService = new UserService(context);
            (await userService.GetUserIdByIdentification(1, "88")).Id.Should().Be(0);
            (await userService.GetUserIdByIdentification(1, "77")).Id.Should().Be(7);

            context.Set<Modality>().Add(new Modality { Id = 1, Code = ModalityCodes.ProyectoGrado, Name = "PG", MaxStudents = 2, RequiresApproval = true, StatusRegister = true });
            var periodSvcMock = new Mock<IAcademicPeriodService>();
            periodSvcMock.Setup(p => p.GetActiveAcademicPeriodAsync()).ReturnsAsync(new AcademicPeriod { Id = 1, Code = "2026-2", StatusRegister = true });

            var inscHandler = new CreateInscriptionWithStudentsHandler(
                Mock.Of<IMediator>(),
                CreateMapper(),
                NullLogger<CreateInscriptionWithStudentsHandler>.Instance,
                userService,
                periodSvcMock.Object,
                uow,
                Mock.Of<IJobEnqueuer>());

            // Co-estudiante 8 inactivo -> KeyNotFoundException (GetUserIdByIdentification retorna Id=0)
            var inactiveUserCmd = new CreateInscriptionWithStudentsCommand(
                new InscriptionWithStudentsCreateDto
                {
                    InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                    Students =
                    [
                        new UserInscriptionModalityCreateDto { IdIdentificationType = 1, Identification = "77" },
                        new UserInscriptionModalityCreateDto { IdIdentificationType = 1, Identification = "88" }
                    ]
                },
                new CurrentUserInfo { UserId = 7 });
            await FluentActions.Invoking(() => inscHandler.Handle(inactiveUserCmd, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>();

            // Co-estudiante 9 con UserRole STUDENT inactivo -> InvalidOperationException
            var inactiveRoleCmd = new CreateInscriptionWithStudentsCommand(
                new InscriptionWithStudentsCreateDto
                {
                    InscriptionModality = new InscriptionModalityCreateDto { IdModality = 1 },
                    Students =
                    [
                        new UserInscriptionModalityCreateDto { IdIdentificationType = 1, Identification = "77" },
                        new UserInscriptionModalityCreateDto { IdIdentificationType = 1, Identification = "99" }
                    ]
                },
                new CurrentUserInfo { UserId = 7 });
            await FluentActions.Invoking(() => inscHandler.Handle(inactiveRoleCmd, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no existe o no tiene el rol 'STUDENT'*");
        }
    }
}
