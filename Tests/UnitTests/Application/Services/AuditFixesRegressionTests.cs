using Application.Common.Helpers;
using Application.Common.Services;
using Application.Common.Services.Documents;
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
using Domain.Enums;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Interfaces.Services.Jobs;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
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

            // 3. Fallback a URL institucional por defecto (sigepro-five.vercel.app)
            await new UserChangeHandler(queueMock.Object, uow, NullLogger<UserChangeHandler>.Instance)
                .HandleCreationAsync(user);
            captured.Last()["LoginUrl"].Should().Be("https://sigepro-five.vercel.app/iniciar-sesion");
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

        [Fact]
        public async Task Etapa1_EmailNotificationEventService_AndSmtp_SanitizeSinglePass_SupportRawHtmlKeys_MultiToWithoutBcc_AndPlainTextBody()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            context.Set<EmailNotificationConfig>().Add(new EmailNotificationConfig
            {
                Id = 10,
                EventName = "ETAPA1_TEMPLATE_TEST",
                SubjectTemplate = "[SIGEPRO] {ProjectTitle} & Revisión {MissingKey}",
                BodyTemplate = "<style>.x{color:red}</style><div data-accent=\"{TopAccentColor}\"><p>{StudentNames}</p><p>{Observations}</p>{ObservationsBlock}{EvaluationBadgeHtml}{SpecificObjectivesHtml}<ul>{NextSteps}</ul><a href=\"{LoginUrl}\">{AppUrl}</a>{UnknownPlaceholder}</div>",
                StatusRegister = true
            });

            context.Set<EmailRecipientRule>().AddRange(
                new EmailRecipientRule { Id = 1, EmailNotificationConfigId = 10, RecipientType = "TO", RuleType = "EVENT_PARTICIPANT", RuleValue = "STUDENT", Priority = 1, StatusRegister = true },
                new EmailRecipientRule { Id = 2, EmailNotificationConfigId = 10, RecipientType = "CC", RuleType = "FIXED_EMAIL", RuleValue = "EST1@unicesar.edu.co", Priority = 2, StatusRegister = true },
                new EmailRecipientRule { Id = 3, EmailNotificationConfigId = 10, RecipientType = "CC", RuleType = "FIXED_EMAIL", RuleValue = "comite@unicesar.edu.co", Priority = 3, StatusRegister = true },
                new EmailRecipientRule { Id = 4, EmailNotificationConfigId = 10, RecipientType = "BCC", RuleType = "FIXED_EMAIL", RuleValue = "COMITE@unicesar.edu.co", Priority = 4, StatusRegister = true },
                new EmailRecipientRule { Id = 5, EmailNotificationConfigId = 10, RecipientType = "BCC", RuleType = "FIXED_EMAIL", RuleValue = "auditoria@unicesar.edu.co", Priority = 5, StatusRegister = true });
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:FrontendUrl"] = "https://pegi.unicesar.edu.co/"
                })
                .Build();

            EmailNotification? capturedNotification = null;
            var notifMock = new Mock<INotificationService>();
            notifMock.Setup(n => n.SendEmailAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
                .Callback<EmailNotification, CancellationToken>((e, _) => capturedNotification = e)
                .ReturnsAsync(true);

            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);
            var eventService = new EmailNotificationEventService(
                uow,
                resolver,
                notifMock.Object,
                NullLogger<EmailNotificationEventService>.Instance,
                configuration);

            await eventService.ProcessEventAsync("ETAPA1_TEMPLATE_TEST", new Dictionary<string, object>
            {
                ["ProjectTitle"] = "Título\r\nCon Salto & {StudentEmails}",
                ["StudentNames"] = "Ana <script>alert(1)</script> & {LoginUrl}",
                ["StudentEmails"] = "est1@unicesar.edu.co, est2@unicesar.edu.co, EST1@UNICESAR.EDU.CO",
                ["Observations"] = "Ajustar marco teórico\r\nRevisar citas <img src=x>",
                ["ObservationsBlock"] = NotificationFormattingHelper.BuildObservationsBlockHtml("Revisar citas\nSegunda línea", "warning"),
                ["EvaluationBadgeHtml"] = NotificationFormattingHelper.BuildEvaluationBadgeHtml("CON OBSERVACIONES", "warning"),
                ["SpecificObjectivesHtml"] = NotificationFormattingHelper.BuildBulletedListHtml(["Objetivo <1>", "Objetivo 2"]),
                ["NextSteps"] = "<li>Corregir documento</li>",
                ["TopAccentColor"] = "#D97706"
            });

            capturedNotification.Should().NotBeNull();
            // Subject: sin HtmlEncode, sin saltos CR/LF, sin segunda pasada sobre {StudentEmails}, clave faltante -> ""
            capturedNotification!.Subject.Should().Be("[SIGEPRO] Título  Con Salto & {StudentEmails} & Revisión ");

            // Body: escapa campos de usuario + <br />, NO expande {LoginUrl} inyectado dentro de StudentNames, y respeta RawHtmlKeys
            capturedNotification.Body.Should().Contain("Ana &lt;script&gt;alert(1)&lt;/script&gt; &amp; {LoginUrl}");
            capturedNotification.Body.Should().Contain($"{System.Net.WebUtility.HtmlEncode("Ajustar marco teórico")}<br />Revisar citas &lt;img src=x&gt;");
            capturedNotification.Body.Should().Contain("<table role=\"presentation\"");
            capturedNotification.Body.Should().Contain("CON OBSERVACIONES</span>");
            capturedNotification.Body.Should().Contain("<li>Objetivo &lt;1&gt;</li><li>Objetivo 2</li>");
            capturedNotification.Body.Should().Contain("<li>Corregir documento</li>");
            capturedNotification.Body.Should().Contain("href=\"https://pegi.unicesar.edu.co/iniciar-sesion\">https://pegi.unicesar.edu.co</a>");
            capturedNotification.Body.Should().NotContain("{UnknownPlaceholder}");

            // Destinatarios: ambos estudiantes en ToAddresses, sin mover el 2.° a Bcc y deduplicando To -> Cc -> Bcc
            capturedNotification.To.Should().Be("est1@unicesar.edu.co");
            capturedNotification.ToAddresses.Should().Equal("est1@unicesar.edu.co", "est2@unicesar.edu.co");
            capturedNotification.Cc.Should().Equal("comite@unicesar.edu.co");
            capturedNotification.Bcc.Should().Equal("auditoria@unicesar.edu.co");

            // SmtpNotificationService.CreateMimeMessage: múltiples To, DefaultFromName institucional y TextBody plano
            var smtpService = new SmtpNotificationService(
                new ConfigurationBuilder().Build(),
                NullLogger<SmtpNotificationService>.Instance);
            var mime = smtpService.CreateMimeMessage(capturedNotification);

            mime.From.Mailboxes.Single().Name.Should().Be("SIGEPRO · Universidad Popular del Cesar");
            mime.To.Mailboxes.Select(m => m.Address).Should().Equal("est1@unicesar.edu.co", "est2@unicesar.edu.co");
            mime.Cc.Mailboxes.Select(m => m.Address).Should().Equal("comite@unicesar.edu.co");
            mime.Bcc.Mailboxes.Select(m => m.Address).Should().Equal("auditoria@unicesar.edu.co");
            mime.HtmlBody.Should().NotBeNullOrWhiteSpace();
            mime.TextBody.Should().NotBeNullOrWhiteSpace();
            mime.TextBody.Should().NotContain(".x{color:red}");
            mime.TextBody.Should().Contain("Ana <script>alert(1)</script> & {LoginUrl}");
            mime.TextBody.Should().Contain("Objetivo <1>");
        }

        [Fact]
        public async Task Etapa1_NotificationFormattingHelper_FormatsColombiaTime_AndObservationsBlocks()
        {
            // 2026-11-28 01:30 UTC == 2026-11-27 08:30 PM en América/Bogotá (UTC-5)
            var utcInstant = new DateTimeOffset(2026, 11, 28, 1, 30, 0, TimeSpan.Zero);
            NotificationFormattingHelper.ToColombiaDateTime(utcInstant).Should().Be("27/11/2026 08:30 PM");
            NotificationFormattingHelper.ToColombiaDate(utcInstant).Should().Be("27/11/2026");

            NotificationFormattingHelper.BuildObservationsBlockHtml(null, "approved").Should().BeEmpty();
            NotificationFormattingHelper.BuildObservationsBlockHtml("   ", "info").Should().BeEmpty();

            var warningFallback = NotificationFormattingHelper.BuildObservationsBlockHtml(" ", "warning");
            System.Net.WebUtility.HtmlDecode(warningFallback).Should().Contain("Consulte el detalle y los soportes de evaluación directamente en la plataforma SIGEPRO.");
            warningFallback.Should().Contain("#D97706");

            var rejectedBlock = NotificationFormattingHelper.BuildObservationsBlockHtml("Falta aval <b>firmado</b>\nAdjuntar PDF", "rejected");
            rejectedBlock.Should().Contain("#DC2626");
            rejectedBlock.Should().Contain("Falta aval &lt;b&gt;firmado&lt;/b&gt;<br />Adjuntar PDF");
        }

        [Fact]
        public async Task Etapa1_RecipientResolver_ResolvesPermissionWithRoleFallback_AndPracticeStageResponsibleAcrossPhases()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            context.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = PermissionCodes.PracticaAcademica.N4PAF0PA, StatusRegister = true },
                new Permission { Id = 2, Code = PermissionCodes.PracticaAcademica.N4PAF1PG, StatusRegister = true },
                new Permission { Id = 3, Code = PermissionCodes.PracticaAcademica.N4PAF2PG, StatusRegister = true });

            context.Set<Role>().Add(new Role { Id = 10, Code = "PA_COMMITTEE", Name = "Comité PA", StatusRegister = true });
            context.Set<RolePermission>().AddRange(
                new RolePermission { Id = 1, IdRole = 10, IdPermission = 1, StatusRegister = true },
                new RolePermission { Id = 2, IdRole = 10, IdPermission = 2, StatusRegister = true },
                new RolePermission { Id = 3, IdRole = 10, IdPermission = 3, StatusRegister = true });

            // User 1: tiene UserPermission directa activa para N4PAF0PA
            // User 2: NO tiene ninguna fila en UserPermission, pero tiene UserRole activo en PA_COMMITTEE -> entra por fallback
            // User 3: tiene UserRole en PA_COMMITTEE, pero tiene fila UserPermission revocada (StatusRegister=false) -> NO entra por fallback
            // User 4: docente confirmado asignado a la práctica
            context.Set<User>().AddRange(
                new User { Id = 1, Email = "direct.f0@unicesar.edu.co", StatusRegister = true },
                new User { Id = 2, Email = "fallback.role@unicesar.edu.co", StatusRegister = true },
                new User { Id = 3, Email = "revoked.perm@unicesar.edu.co", StatusRegister = true },
                new User { Id = 4, Email = "docente.pa@unicesar.edu.co", StatusRegister = true },
                new User { Id = 5, Email = "docente.noconf@unicesar.edu.co", StatusRegister = true });

            context.Set<UserPermission>().AddRange(
                new UserPermission { Id = 1, IdUser = 1, IdPermission = 1, StatusRegister = true },
                new UserPermission { Id = 2, IdUser = 3, IdPermission = 1, StatusRegister = false });

            context.Set<UserRole>().AddRange(
                new UserRole { Id = 1, IdUser = 2, IdRole = 10, StatusRegister = true },
                new UserRole { Id = 2, IdUser = 3, IdRole = 10, StatusRegister = true });

            context.Set<StageModality>().AddRange(
                new StageModality { Id = 100, Code = StageModalityCodes.PaFaseInscripcion, StatusRegister = true },
                new StageModality { Id = 101, Code = StageModalityCodes.PaFaseDesarrollo, StatusRegister = true },
                new StageModality { Id = 102, Code = StageModalityCodes.PaFaseEvaluacion, StatusRegister = true });

            context.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 500, IdStageModality = 100, StatusRegister = true },
                new InscriptionModality { Id = 501, IdStageModality = 101, StatusRegister = true },
                new InscriptionModality { Id = 502, IdStageModality = 102, StatusRegister = true });

            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment { Id = 1, IdInscriptionModality = 501, IdTeacher = 4, IsConfirmed = true, RevocationDate = null, StatusRegister = true },
                new TeachingAssignment { Id = 2, IdInscriptionModality = 501, IdTeacher = 5, IsConfirmed = false, RevocationDate = null, StatusRegister = true },
                new TeachingAssignment { Id = 3, IdInscriptionModality = 502, IdTeacher = 4, IsConfirmed = true, RevocationDate = null, StatusRegister = true });

            await context.SaveChangesAsync();

            var resolver = new EmailRecipientResolverService(uow, NullLogger<EmailRecipientResolverService>.Instance);
            var rule = new EmailRecipientRule
            {
                Id = 1,
                RecipientType = "TO",
                RuleType = "BY_ENTITY_RELATION",
                RuleValue = "PRACTICE_STAGE_RESPONSIBLE",
                StatusRegister = true
            };

            // Fase 0 (Inscripción): N4PAF0PA -> direct.f0 + fallback.role (excluyendo revoked.perm)
            var f0 = await resolver.ResolveRecipientsAsync([rule], new Dictionary<string, object> { ["InscriptionModalityId"] = 500 });
            f0.To.Should().BeEquivalentTo(["direct.f0@unicesar.edu.co", "fallback.role@unicesar.edu.co"]);

            // Fase 1 (Desarrollo): docente confirmado + N4PAF1PG (fallback.role)
            var f1 = await resolver.ResolveRecipientsAsync([rule], new Dictionary<string, object> { ["InscriptionModalityId"] = 501 });
            f1.To.Should().BeEquivalentTo(["docente.pa@unicesar.edu.co", "fallback.role@unicesar.edu.co"]);

            // Fase 2 (Evaluación): docente confirmado + N4PAF2PG (fallback.role)
            var f2 = await resolver.ResolveRecipientsAsync([rule], new Dictionary<string, object> { ["InscriptionModalityId"] = 502 });
            f2.To.Should().BeEquivalentTo(["docente.pa@unicesar.edu.co", "fallback.role@unicesar.edu.co"]);
        }

        [Fact]
        public async Task Etapa1_ChangeHandlers_FixUnassignedTeacher_RequiresApprovalFalse_PreliminaryEnEvaluacion_AndProposalAdjustments()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            var enqueued = new List<(string EventName, Dictionary<string, object> Data)>();
            var queueMock = new Mock<IEmailNotificationQueueService>();
            queueMock.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Callback<string, Dictionary<string, object>>((evt, data) => enqueued.Add((evt, data)))
                .Returns("job-ok");

            context.Set<Modality>().AddRange(
                new Modality { Id = 1, Code = ModalityCodes.ProyectoGrado, Name = "Proyecto de Grado", RequiresApproval = true, StatusRegister = true },
                new Modality { Id = 3, Code = ModalityCodes.CoTerminal, Name = "Co-Terminal", RequiresApproval = false, StatusRegister = true });
            context.Set<AcademicPeriod>().Add(new AcademicPeriod { Id = 1, Code = "2026-2", StatusRegister = true });
            context.Set<StageModality>().Add(new StageModality { Id = 4, Code = StageModalityCodes.PgFasePropuesta, Name = "Fase Propuesta", StatusRegister = true });
            context.Set<StateStage>().AddRange(
                new StateStage { Id = 5, Code = StateStageCodes.ApRadicadoPendAsigEval, Name = "Radicado", StatusRegister = true },
                new StateStage { Id = 37, Code = StateStageCodes.ApEnEvaluacion, Name = "En Evaluación", StatusRegister = true },
                new StateStage { Id = 31, Code = StateStageCodes.PropNoPertinente, Name = "No Pertinente", IsRejectionState = false, StatusRegister = true },
                new StateStage { Id = 99, Code = "PROP_RECHAZADA", Name = "Rechazada", IsRejectionState = true, StatusRegister = true });

            context.Set<User>().AddRange(
                new User { Id = 10, Email = "old.teacher@unicesar.edu.co", FirstName = "Carlos", LastName = "Docente", StatusRegister = true },
                new User { Id = 20, Email = "creator@unicesar.edu.co", FirstName = "Ana", LastName = "Lider", StatusRegister = true },
                new User { Id = 21, Email = "invited@unicesar.edu.co", FirstName = "Luis", LastName = "Invitado", StatusRegister = true });

            context.Set<TypeTeachingAssignment>().Add(new TypeTeachingAssignment { Id = 1, Code = "DIRECTOR", Name = "Director", StatusRegister = true });
            context.Set<InscriptionModality>().AddRange(
                new InscriptionModality { Id = 700, IdModality = 1, IdAcademicPeriod = 1, IdStageModality = 4, StatusRegister = true },
                new InscriptionModality { Id = 701, IdModality = 3, IdAcademicPeriod = 1, IdUserCreatedAt = 20, CreatedAt = DateTimeOffset.UtcNow, StatusRegister = true });
            context.Set<Proposal>().Add(new Proposal { Id = 700, Title = "Sistema Inteligente SIGEPRO", IdStateStage = 31, StatusRegister = true });
            context.Set<UserInscriptionModality>().AddRange(
                new UserInscriptionModality { Id = 1, IdInscriptionModality = 701, IdUser = 20, IsConfirmed = true, StatusRegister = true },
                new UserInscriptionModality { Id = 2, IdInscriptionModality = 701, IdUser = 21, IsConfirmed = false, StatusRegister = true });
            await context.SaveChangesAsync();

            // 1. TEACHER_UNASSIGNED cuando oldEntity.IdInscriptionModality == 0 toma fallback de newEntity.IdInscriptionModality
            var taBuilderMock = new Mock<ITeachingAssignmentEventDataBuilder>();
            taBuilderMock.Setup(b => b.BuildTeachingAssignmentEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { ["Dummy"] = 1 });

            var taHandler = new TeachingAssignmentChangeHandler(
                queueMock.Object,
                taBuilderMock.Object,
                uow,
                NullLogger<TeachingAssignmentChangeHandler>.Instance);

            await taHandler.HandleChangeAsync(
                new TeachingAssignment { Id = 55, IdInscriptionModality = 0, IdTeacher = 10, IdTypeTeachingAssignment = 1, StatusRegister = true },
                new TeachingAssignment { Id = 55, IdInscriptionModality = 700, IdTeacher = 10, IdTypeTeachingAssignment = 1, RevocationDate = DateTimeOffset.UtcNow, StatusRegister = false });

            var unassignedEvt = enqueued.Single(e => e.EventName == "TEACHER_UNASSIGNED");
            unassignedEvt.Data["InscriptionId"].Should().Be(700);
            unassignedEvt.Data["ProjectTitle"].Should().Be("Sistema Inteligente SIGEPRO");
            unassignedEvt.Data["ModalityName"].Should().Be("Proyecto de Grado");

            // 2. InscriptionChangeHandler: RequiresApproval == false NO encola INSCRIPTION_CREATED, pero SÍ INSCRIPTION_COSTUDENT_INVITED
            enqueued.Clear();
            var inscBuilderMock = new Mock<IInscriptionEventDataBuilder>();
            var inscChangeHandler = new InscriptionChangeHandler(
                queueMock.Object,
                inscBuilderMock.Object,
                uow,
                NullLogger<InscriptionChangeHandler>.Instance);

            var nonApprovalInsc = await context.Set<InscriptionModality>().FindAsync(701);
            await inscChangeHandler.HandleCreationAsync(nonApprovalInsc!);

            enqueued.Select(e => e.EventName).Should().Equal("INSCRIPTION_COSTUDENT_INVITED");
            inscBuilderMock.Verify(b => b.BuildBasicInscriptionDataAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<int>>()), Times.Never);

            // 3. PreliminaryProjectChangeHandler: solo AP_RADICADO_PEND_ASIG_EVAL dispara ANTEPROYECTO_SUBMITTED (NO AP_EN_EVALUACION)
            enqueued.Clear();
            var ppBuilderMock = new Mock<IPreliminaryProjectEventDataBuilder>();
            ppBuilderMock.Setup(b => b.BuildPreliminaryProjectEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { ["PreliminaryProjectId"] = 700 });

            var ppChangeHandler = new PreliminaryProjectChangeHandler(
                queueMock.Object,
                ppBuilderMock.Object,
                uow,
                NullLogger<PreliminaryProjectChangeHandler>.Instance);

            await ppChangeHandler.HandleChangeAsync(
                new PreliminaryProject { Id = 700, IdStateStage = 1 },
                new PreliminaryProject { Id = 700, IdStateStage = 5 }); // AP_RADICADO_PEND_ASIG_EVAL
            await ppChangeHandler.HandleChangeAsync(
                new PreliminaryProject { Id = 700, IdStateStage = 5 },
                new PreliminaryProject { Id = 700, IdStateStage = 37 }); // AP_EN_EVALUACION

            enqueued.Select(e => e.EventName).Should().Equal("ANTEPROYECTO_SUBMITTED");

            // 4. ProposalChangeHandler: PROP_NO_PERTINENTE -> PROPOSAL_ADJUSTMENTS, estado de rechazo -> PROPOSAL_REJECTED
            enqueued.Clear();
            var propBuilderMock = new Mock<IProposalEventDataBuilder>();
            propBuilderMock.Setup(b => b.BuildProposalEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { ["ProposalId"] = 700 });

            var propChangeHandler = new ProposalChangeHandler(
                queueMock.Object,
                propBuilderMock.Object,
                uow,
                NullLogger<ProposalChangeHandler>.Instance);

            await propChangeHandler.HandleChangeAsync(
                new Proposal { Id = 700, IdStateStage = 1 },
                new Proposal { Id = 700, IdStateStage = 31 });
            await propChangeHandler.HandleChangeAsync(
                new Proposal { Id = 700, IdStateStage = 31 },
                new Proposal { Id = 700, IdStateStage = 99 });

            enqueued.Select(e => e.EventName).Should().Equal("PROPOSAL_ADJUSTMENTS", "PROPOSAL_REJECTED");
        }

        [Fact]
        public async Task Etapa1_AdvanceAcademicPracticePhaseHandler_StampsExactDatesPerPhase_AndAcademicPracticeBuilderPopulatesFields()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            context.Set<Modality>().Add(new Modality { Id = 2, Code = ModalityCodes.PracticaAcademica, Name = "Práctica Académica", RequiresApproval = true, StatusRegister = true });
            context.Set<StageModality>().AddRange(
                new StageModality { Id = 1, IdModality = 2, Code = StageModalityCodes.PaFaseInscripcion, StageOrder = 1, StatusRegister = true },
                new StageModality { Id = 2, IdModality = 2, Code = StageModalityCodes.PaFaseDesarrollo, StageOrder = 2, StatusRegister = true },
                new StageModality { Id = 3, IdModality = 2, Code = StageModalityCodes.PaFaseEvaluacion, StageOrder = 3, StatusRegister = true });

            context.Set<StateStage>().AddRange(
                new StateStage { Id = 10, Code = StateStageCodes.PaInscripcionPendDoc, StatusRegister = true },
                new StateStage { Id = 11, Code = StateStageCodes.PaInscripcionAprobada, StatusRegister = true },
                new StateStage { Id = 12, Code = StateStageCodes.PaDesarrolloAprobada, StatusRegister = true },
                new StateStage { Id = 13, Code = StateStageCodes.PaInformeFinalEnRevision, StatusRegister = true },
                new StateStage { Id = 14, Code = StateStageCodes.PaAprobado, StatusRegister = true },
                new StateStage { Id = 15, Code = nameof(StateStageCodeEnum.PA_NO_APROBADO), Name = "No Aprobado", IsRejectionState = true, StatusRegister = true });

            context.Set<Permission>().AddRange(
                new Permission { Id = 1, Code = PermissionCodes.PracticaAcademica.N1PA, StatusRegister = true },
                new Permission { Id = 2, Code = PermissionCodes.PracticaAcademica.N2PAF1, ParentCode = PermissionCodes.PracticaAcademica.N1PA, StatusRegister = true },
                new Permission { Id = 3, Code = PermissionCodes.PracticaAcademica.N3PAF1R, ParentCode = PermissionCodes.PracticaAcademica.N2PAF1, StatusRegister = true },
                new Permission { Id = 4, Code = PermissionCodes.PracticaAcademica.N3PAF1C, ParentCode = PermissionCodes.PracticaAcademica.N2PAF1, StatusRegister = true },
                new Permission { Id = 5, Code = PermissionCodes.PracticaAcademica.N2PAF2, ParentCode = PermissionCodes.PracticaAcademica.N1PA, StatusRegister = true },
                new Permission { Id = 6, Code = PermissionCodes.PracticaAcademica.N3PAF2R, ParentCode = PermissionCodes.PracticaAcademica.N2PAF2, StatusRegister = true },
                new Permission { Id = 7, Code = PermissionCodes.PracticaAcademica.N3PAF2C, ParentCode = PermissionCodes.PracticaAcademica.N2PAF2, StatusRegister = true });

            context.Set<InscriptionModality>().Add(new InscriptionModality { Id = 800, IdModality = 2, IdStageModality = 1, StatusRegister = true });
            context.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = 800,
                Title = "Práctica en Gobernación",
                InstitutionName = null,
                IdStateStage = 10,
                Observations = "Observación comité",
                EvaluatorObservations = "Observación evaluador",
                StatusRegister = true
            });
            await context.SaveChangesAsync();

            var phaseHandler = new AdvanceAcademicPracticePhaseHandler(uow, NullLogger<AdvanceAcademicPracticePhaseHandler>.Instance);

            // 1. PaInscripcionAprobada: solo AvalApprovalDate (PlanApprovalDate permanece null)
            await phaseHandler.Handle(new AcademicPracticeStateChangedEvent(800, 11, 10, 1), CancellationToken.None);
            await context.SaveChangesAsync();
            var ap = (await context.Set<AcademicPractice>().FindAsync(800))!;
            ap.AvalApprovalDate.Should().NotBeNull();
            ap.PlanApprovalDate.Should().BeNull();
            ap.DevelopmentCompletionDate.Should().BeNull();
            ap.FinalReportApprovalDate.Should().BeNull();
            ap.FinalApprovalDate.Should().BeNull();

            // 2. PaDesarrolloAprobada: PlanApprovalDate y DevelopmentCompletionDate
            await phaseHandler.Handle(new AcademicPracticeStateChangedEvent(800, 12, 11, 1), CancellationToken.None);
            await context.SaveChangesAsync();
            ap = (await context.Set<AcademicPractice>().FindAsync(800))!;
            ap.PlanApprovalDate.Should().NotBeNull();
            ap.DevelopmentCompletionDate.Should().NotBeNull();
            ap.FinalReportApprovalDate.Should().BeNull();

            // 3. PaInformeFinalEnRevision: NO estampa FinalReportApprovalDate
            await phaseHandler.Handle(new AcademicPracticeStateChangedEvent(800, 13, 12, 1), CancellationToken.None);
            await context.SaveChangesAsync();
            ap = (await context.Set<AcademicPractice>().FindAsync(800))!;
            ap.FinalReportApprovalDate.Should().BeNull();
            ap.FinalApprovalDate.Should().BeNull();

            // 4. PaAprobado: estampa FinalReportApprovalDate y FinalApprovalDate
            await phaseHandler.Handle(new AcademicPracticeStateChangedEvent(800, 14, 13, 1), CancellationToken.None);
            await context.SaveChangesAsync();
            ap = (await context.Set<AcademicPractice>().FindAsync(800))!;
            ap.FinalReportApprovalDate.Should().NotBeNull();
            ap.FinalApprovalDate.Should().NotBeNull();

            // 5. AcademicPracticeEventDataBuilder en PA_NO_APROBADO: CompanyName fallback, ObservationsBlock combinado y PossibleCauses poblado
            ap.IdStateStage = 15;
            await context.SaveChangesAsync();

            var studentSvcMock = new Mock<IStudentDataService>();
            studentSvcMock.Setup(s => s.GetStudentDataByInscriptionAsync(800))
                .ReturnsAsync(("María Gómez", "maria@unicesar.edu.co", 1));

            var apBuilder = new AcademicPracticeEventDataBuilder(uow, studentSvcMock.Object, NullLogger<AcademicPracticeEventDataBuilder>.Instance);
            var apData = await apBuilder.BuildAcademicPracticeEventDataAsync(800, "PRACTICE_EVALUATED");

            apData["CompanyName"].Should().Be("Institución receptora por confirmar");
            apData["TopAccentColor"].Should().Be("#DC2626");
            System.Net.WebUtility.HtmlDecode(apData["ObservationsBlock"].ToString()!).Should().Contain("Observación comité");
            System.Net.WebUtility.HtmlDecode(apData["ObservationsBlock"].ToString()!).Should().Contain("Observación evaluador");
            apData["PossibleCauses"].ToString().Should().Contain("<li>");
        }

        [Fact]
        public async Task DocumentAccessGuard_SingleTableModality_AllowsNewlyUnlockedStageUpload_BlocksFutureStages_AndBlocksOverallFinalOrRejected()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            context.Set<Modality>().Add(new Modality
            {
                Id = 2,
                Code = ModalityCodes.PracticaAcademica,
                Name = "Práctica Académica",
                RequiresApproval = true,
                StatusRegister = true
            });

            context.Set<StateInscription>().Add(new StateInscription
            {
                Id = 2,
                Code = StateInscriptionCodes.Aprobado,
                Name = "Aprobado",
                IsFinalStateForStage = true,
                StatusRegister = true
            });

            context.Set<StageModality>().AddRange(
                new StageModality { Id = 5, IdModality = 2, Code = StageModalityCodes.PaFaseInscripcion, Name = "Fase 0 Inscripción", StageOrder = 1, StatusRegister = true },
                new StageModality { Id = 6, IdModality = 2, Code = StageModalityCodes.PaFaseDesarrollo, Name = "Fase 1 Desarrollo", StageOrder = 2, StatusRegister = true },
                new StageModality { Id = 7, IdModality = 2, Code = StageModalityCodes.PaFaseEvaluacion, Name = "Fase 2 Evaluación", StageOrder = 3, StatusRegister = true });

            context.Set<StateStage>().AddRange(
                new StateStage
                {
                    Id = 20,
                    IdStageModality = 5,
                    Code = StateStageCodes.PaInscripcionEnRevision,
                    Name = "Inscripción en Revisión",
                    IsFinalStateForStage = false,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false,
                    StatusRegister = true
                },
                new StateStage
                {
                    Id = 21,
                    IdStageModality = 5,
                    Code = StateStageCodes.PaInscripcionAprobada,
                    Name = "Inscripción Aprobada - Puede Iniciar Desarrollo",
                    IsFinalStateForStage = true,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false,
                    StatusRegister = true
                },
                new StateStage
                {
                    Id = 22,
                    IdStageModality = 5,
                    Code = StateStageCodes.PaInscripcionRechazada,
                    Name = "Inscripción Rechazada",
                    IsFinalStateForStage = true,
                    IsFinalStateForModalityOverall = true,
                    IsRejectionState = true,
                    StatusRegister = true
                },
                new StateStage
                {
                    Id = 23,
                    IdStageModality = 6,
                    Code = StateStageCodes.PaDesarrolloAprobada,
                    Name = "Desarrollo Aprobado",
                    IsFinalStateForStage = true,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false,
                    StatusRegister = true
                },
                new StateStage
                {
                    Id = 24,
                    IdStageModality = 7,
                    Code = StateStageCodes.PaAprobado,
                    Name = "Práctica Aprobada",
                    IsFinalStateForStage = true,
                    IsFinalStateForModalityOverall = true,
                    IsRejectionState = false,
                    StatusRegister = true
                });

            context.Set<DocumentClass>().Add(new DocumentClass
            {
                Id = 1,
                Code = DocumentClassCodes.DocEntregableEst,
                Name = "Entregable Estudiante",
                StatusRegister = true
            });

            context.Set<DocumentType>().AddRange(
                new DocumentType { Id = 100, IdDocumentClass = 1, IdStageModality = 5, Code = "PA_DOC_F0", Name = "Aval Fase 0", StatusRegister = true },
                new DocumentType { Id = 101, IdDocumentClass = 1, IdStageModality = 6, Code = "PA_DOC_F1", Name = "Plan/Seguimiento Fase 1", StatusRegister = true },
                new DocumentType { Id = 102, IdDocumentClass = 1, IdStageModality = 7, Code = "PA_DOC_F2", Name = "Informe Final Fase 2", StatusRegister = true });

            context.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 950,
                IdModality = 2,
                IdStateInscription = 2,
                IdStageModality = 6, // Fase 1 (Desarrollo, StageOrder = 2)
                StatusRegister = true
            });

            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                Id = 1,
                IdInscriptionModality = 950,
                IdUser = 301,
                IsConfirmed = true,
                StatusRegister = true
            });

            context.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = 950,
                Title = "Práctica E2E",
                IdStateStage = 21, // PA_INSCRIPCION_APROBADA (pertenece a Stage 5, StageOrder = 1)
                StatusRegister = true
            });

            await context.SaveChangesAsync();

            var guard = new DocumentAccessGuard(uow);

            // Caso 1: PA_INSCRIPCION_APROBADA + InscriptionModality en PA_FASE_DESARROLLO (StageOrder = 2)
            // -> SÍ permite subir entregable de Fase 1 (StageOrder = 2)
            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 101 },
                    currentUserId: 301))
                .Should().NotThrowAsync();

            // -> NO permite subir entregable de Fase 0 ya superada (StageOrder = 1) ni Fase 2 futura (StageOrder = 3)
            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 100 },
                    currentUserId: 301))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ya fue superada y cerrada*");

            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 102 },
                    currentUserId: 301))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*aún no se encuentra habilitada*");

            // Caso 1b: PA_DESARROLLO_APROBADA + InscriptionModality en PA_FASE_EVALUACION (StageOrder = 3)
            // -> SÍ permite subir entregable de Fase 2 (StageOrder = 3)
            var inscription = (await context.Set<InscriptionModality>().FindAsync(950))!;
            var practice = (await context.Set<AcademicPractice>().FindAsync(950))!;
            inscription.IdStageModality = 7;
            practice.IdStateStage = 23;
            await context.SaveChangesAsync();

            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 102 },
                    currentUserId: 301))
                .Should().NotThrowAsync();

            // Caso 2: PA_INSCRIPCION_EN_REVISION + InscriptionModality en PA_FASE_INSCRIPCION (StageOrder = 1)
            // -> NO permite subir documentos de Fase 1 (StageOrder = 2) ni Fase 2 (StageOrder = 3)
            inscription.IdStageModality = 5;
            practice.IdStateStage = 20;
            await context.SaveChangesAsync();

            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 101 },
                    currentUserId: 301))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*aún no se encuentra habilitada*");

            await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                    new Document { IdInscriptionModality = 950, IdDocumentType = 102 },
                    currentUserId: 301))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*aún no se encuentra habilitada*");

            // Caso 3a: PA_APROBADO (IsFinalStateForModalityOverall = true) -> NO permite subir ningún documento
            inscription.IdStageModality = 7;
            practice.IdStateStage = 24;
            await context.SaveChangesAsync();

            foreach (var docTypeId in new[] { 100, 101, 102 })
            {
                await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                        new Document { IdInscriptionModality = 950, IdDocumentType = docTypeId },
                        currentUserId: 301))
                    .Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*cerrada*");
            }

            // Caso 3b: PA_INSCRIPCION_RECHAZADA (IsRejectionState = true) -> NO permite subir ningún documento
            inscription.IdStageModality = 5;
            practice.IdStateStage = 22;
            await context.SaveChangesAsync();

            foreach (var docTypeId in new[] { 100, 101, 102 })
            {
                await FluentActions.Awaiting(() => guard.EnsureUserCanModifyAsync(
                        new Document { IdInscriptionModality = 950, IdDocumentType = docTypeId },
                        currentUserId: 301))
                    .Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*rechazo*");
            }
        }

        [Fact]
        public async Task HallazgoA_MinorModalityBuilders_WhenRequiresApprovalFalseAndInscriptionApprovalDateNull_PopulateColombiaApprovalDate()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            var studentDataMock = new Mock<IStudentDataService>();
            studentDataMock.Setup(s => s.GetStudentDataByInscriptionAsync(It.IsAny<int>()))
                .ReturnsAsync(("Valentina Ríos", "valentina@unicesar.edu.co", 1));

            context.Set<Modality>().AddRange(
                new Modality { Id = 6, Code = ModalityCodes.GradoPromedio, Name = "Grado por Promedio", RequiresApproval = false, StatusRegister = true },
                new Modality { Id = 7, Code = ModalityCodes.SaberPro, Name = "Saber Pro", RequiresApproval = false, StatusRegister = true });

            context.Set<StateInscription>().Add(new StateInscription
            {
                Id = 3,
                Code = StateInscriptionCodes.NoAplica,
                Name = "No Aplica",
                StatusRegister = true
            });

            context.Set<StateStage>().AddRange(
                new StateStage { Id = 80, Code = nameof(StateStageCodeEnum.SP_APROBADO), Name = "Saber Pro Aprobado", IsFinalStateForStage = true, IsFinalStateForModalityOverall = true, StatusRegister = true },
                new StateStage { Id = 81, Code = nameof(StateStageCodeEnum.GP_APROBADO), Name = "Grado por Promedio Aprobado", IsFinalStateForStage = true, IsFinalStateForModalityOverall = true, StatusRegister = true });

            context.Set<InscriptionModality>().AddRange(
                new InscriptionModality
                {
                    Id = 980,
                    IdModality = 7,
                    IdStateInscription = 3,
                    ApprovalDate = null, // Modalidad sin aprobación previa de inscripción
                    StatusRegister = true
                },
                new InscriptionModality
                {
                    Id = 981,
                    IdModality = 6,
                    IdStateInscription = 3,
                    ApprovalDate = null, // Modalidad sin aprobación previa de inscripción
                    StatusRegister = true
                });

            // 2026-11-28 02:15 UTC == 2026-11-27 09:15 PM en América/Bogotá (UTC-5) -> fecha Colombia "27/11/2026"
            var evaluationInstantUtc = new DateTimeOffset(2026, 11, 28, 2, 15, 0, TimeSpan.Zero);

            context.Set<SaberPro>().Add(new SaberPro
            {
                Id = 980,
                IdStateStage = 80,
                ResultQuintile = "Q5",
                ResultScore = 215,
                CreatedAt = evaluationInstantUtc.AddDays(-5),
                UpdatedAt = evaluationInstantUtc,
                StatusRegister = true
            });

            context.Set<AcademicAverage>().Add(new AcademicAverage
            {
                Id = 981,
                IdStateStage = 81,
                CertifiedAverage = 4.65m,
                HasFailedSubjects = false,
                CreatedAt = evaluationInstantUtc.AddDays(-3),
                UpdatedAt = evaluationInstantUtc,
                StatusRegister = true
            });

            await context.SaveChangesAsync();

            var saberProBuilder = new SaberProEventDataBuilder(
                uow,
                studentDataMock.Object,
                NullLogger<SaberProEventDataBuilder>.Instance);
            var academicAverageBuilder = new AcademicAverageEventDataBuilder(
                uow,
                studentDataMock.Object,
                NullLogger<AcademicAverageEventDataBuilder>.Instance);

            var spData = await saberProBuilder.BuildEventDataAsync(980, "SABER_PRO_APPROVED");
            var gpData = await academicAverageBuilder.BuildEventDataAsync(981, "ACADEMIC_AVERAGE_APPROVED");

            spData["ApprovalDate"].Should().Be("27/11/2026");
            gpData["ApprovalDate"].Should().Be("27/11/2026");
        }

        [Fact]
        public async Task ScientificArticle_WhenAutoAdvancedFromPhase1ToArtPubPendiente_EnqueuesArtFase1ApprovedWithApprovedMetadata()
        {
            using var context = CreateInMemoryContext();
            var uow = new UnitOfWork(context, Mock.Of<IMediator>(), NullLogger<UnitOfWork>.Instance);

            var studentDataMock = new Mock<IStudentDataService>();
            studentDataMock.Setup(s => s.GetStudentDataByInscriptionAsync(990))
                .ReturnsAsync(("Camila Torres", "camila@unicesar.edu.co", 1));

            context.Set<Modality>().Add(new Modality
            {
                Id = 5,
                Code = ModalityCodes.PublicacionArticulo,
                Name = "Publicación de Artículo",
                RequiresApproval = true,
                StatusRegister = true
            });

            context.Set<StateStage>().AddRange(
                new StateStage { Id = 85, Code = nameof(StateStageCodeEnum.ART_INS_RADICADO), Name = "Inscripción Radicada", StatusRegister = true },
                new StateStage { Id = 87, Code = nameof(StateStageCodeEnum.ART_INS_OBSERVACIONES), Name = "Inscripción con Observaciones", StatusRegister = true },
                new StateStage { Id = 88, Code = nameof(StateStageCodeEnum.ART_INS_APROBADO), Name = "Inscripción Aprobada", IsFinalStateForStage = true, IsFinalStateForModalityOverall = false, StatusRegister = true },
                new StateStage { Id = 92, Code = nameof(StateStageCodeEnum.ART_PUB_PENDIENTE), Name = "Publicación Pendiente", IsInitialState = true, StatusRegister = true });

            var approvalInstantUtc = new DateTimeOffset(2026, 11, 28, 3, 0, 0, TimeSpan.Zero); // 27/11/2026 10:00 PM COT

            context.Set<InscriptionModality>().Add(new InscriptionModality
            {
                Id = 990,
                IdModality = 5,
                ApprovalDate = approvalInstantUtc.AddDays(-10),
                StatusRegister = true
            });

            context.Set<ScientificArticle>().Add(new ScientificArticle
            {
                Id = 990,
                IdStateStage = 92, // Ya promovido automáticamente a ART_PUB_PENDIENTE por AdvanceScientificArticlePhaseHandler
                ArticleTitle = "Modelos de IA en Educación Superior",
                JournalName = "Revista Ingeniería UPC",
                ISSN = "1234-5678",
                JournalCategory = "A2",
                CreatedAt = approvalInstantUtc.AddDays(-2),
                UpdatedAt = approvalInstantUtc,
                StatusRegister = true
            });

            await context.SaveChangesAsync();

            var builder = new ScientificArticleEventDataBuilder(
                uow,
                studentDataMock.Object,
                NullLogger<ScientificArticleEventDataBuilder>.Instance);

            var enqueued = new List<(string EventName, Dictionary<string, object> Data)>();
            var queueMock = new Mock<IEmailNotificationQueueService>();
            queueMock.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Callback<string, Dictionary<string, object>>((evt, data) => enqueued.Add((evt, data)))
                .Returns("job-art-1");

            var handler = new ScientificArticleChangeHandler(
                queueMock.Object,
                builder,
                uow,
                NullLogger<ScientificArticleChangeHandler>.Instance);

            // a) Transición ART_INS_RADICADO -> ART_PUB_PENDIENTE (auto-avance tras aprobar Fase 1)
            await handler.HandleChangeAsync(
                new ScientificArticle { Id = 990, IdStateStage = 85 },
                new ScientificArticle { Id = 990, IdStateStage = 92 });

            // Y también desde ART_INS_OBSERVACIONES -> ART_PUB_PENDIENTE
            await handler.HandleChangeAsync(
                new ScientificArticle { Id = 990, IdStateStage = 87 },
                new ScientificArticle { Id = 990, IdStateStage = 92 });

            enqueued.Should().HaveCount(2);
            enqueued.Select(e => e.EventName).Should().OnlyContain(name => name == "ART_FASE1_APPROVED");

            // b) Metadatos construidos por ScientificArticleEventDataBuilder para ART_FASE1_APPROVED
            var builtData = enqueued[0].Data;
            builtData["Phase"].Should().Be("Artículo - Fase 1: Inscripción");
            builtData["CurrentState"].Should().Be("Inscripción Aprobada");
            builtData["CurrentStateCode"].Should().Be(nameof(StateStageCodeEnum.ART_INS_APROBADO));
            builtData["EvaluationResult"].Should().Be("APROBADO");
            builtData["TopAccentColor"].Should().Be(NotificationFormattingHelper.GetTopAccentColor("approved"));
            builtData["ApprovalDate"].Should().Be("27/11/2026");
        }
    }
}
