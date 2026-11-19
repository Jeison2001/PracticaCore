using Application.Common.Services;
using Application.Shared.Commands.AcademicPractices;
using Application.Shared.Commands.AcademicPractices.Handlers;
using Application.Shared.Commands.CoTerminals;
using Application.Shared.Commands.CoTerminals.Handlers;
using Application.Shared.Commands.PreliminaryProjects;
using Application.Shared.Commands.PreliminaryProjects.Handlers;
using Application.Shared.Commands.ProjectFinals;
using Application.Shared.Commands.ProjectFinals.Handlers;
using Application.Shared.Commands.ProjectPresentations;
using Application.Shared.Commands.ProjectPresentations.Handlers;
using Application.Shared.Commands.Proposals;
using Application.Shared.Commands.Proposals.Handlers;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.CoTerminals;
using Application.Shared.DTOs.PreliminaryProjects;
using Application.Shared.DTOs.ProjectFinals;
using Application.Shared.DTOs.ProjectPresentations;
using Application.Shared.DTOs.Proposals;
using AutoMapper;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application.Commands
{
    public class ModalityFinalStateGuardTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRepository<StateStage, int>> _stateStageRepoMock;
        private readonly Mock<IRepository<StageModality, int>> _stageModalityRepoMock;
        private readonly Mock<IRepository<InscriptionModality, int>> _inscriptionRepoMock;

        public ModalityFinalStateGuardTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _stateStageRepoMock = new Mock<IRepository<StateStage, int>>();
            _stageModalityRepoMock = new Mock<IRepository<StageModality, int>>();
            _inscriptionRepoMock = new Mock<IRepository<InscriptionModality, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<StateStage, int>())
                .Returns(_stateStageRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StageModality, int>())
                .Returns(_stageModalityRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<InscriptionModality, int>())
                .Returns(_inscriptionRepoMock.Object);
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCurrentStateIsFinalForSameStage_ThrowsInvalidOperationException()
        {
            // Arrange
            const int originalStateId = 10;
            const int newStateId = 20;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    IdStageModality = 1,
                    Code = "STAGE_FINAL",
                    Name = "Etapa Finalizada",
                    IsFinalStateForStage = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    IdStageModality = 1,
                    Code = "STAGE_OTHER",
                    Name = "Misma Etapa Otro Estado",
                    IsFinalStateForStage = false
                });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Etapa Finalizada'.");
        }

        [Fact]
        public async Task ModalityStateHelper_WhenAdvancingToNextStage_AllowsTransitionEvenIfCurrentStateIsFinalForStage()
        {
            // Arrange
            const int originalStateId = 10;
            const int newStateId = 20;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    IdStageModality = 1,
                    Code = "STAGE1_FINAL",
                    Name = "Fase 1 Aprobada",
                    IsFinalStateForStage = true,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    IdStageModality = 2,
                    Code = "STAGE2_IN_PROGRESS",
                    Name = "Fase 2 En Curso",
                    IsFinalStateForStage = false,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false
                });

            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new StageModality { Id = 1, IdModality = 1, Code = "S1", Name = "Etapa 1", StatusRegister = true });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(new StageModality { Id = 2, IdModality = 1, Code = "S2", Name = "Etapa 2", StatusRegister = true });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId, allowCrossStageWithinSameModality: true);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCrossStageWithoutOptIn_ThrowsInvalidOperationException()
        {
            // Arrange
            const int originalStateId = 11;
            const int newStateId = 21;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage { Id = originalStateId, IdStageModality = 1, Code = "S1_OPEN", Name = "Etapa 1 En Curso" });
            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage { Id = newStateId, IdStageModality = 2, Code = "S2_OPEN", Name = "Etapa 2 En Curso" });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new StageModality { Id = 1, IdModality = 1, Code = "S1", Name = "Etapa 1", StatusRegister = true });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(2))
                .ReturnsAsync(new StageModality { Id = 2, IdModality = 1, Code = "S2", Name = "Etapa 2", StatusRegister = true });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId, allowCrossStageWithinSameModality: false);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede transicionar directamente a un estado perteneciente a otra etapa.");
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCrossModality_ThrowsInvalidOperationException()
        {
            // Arrange
            const int originalStateId = 13;
            const int newStateId = 23;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage { Id = originalStateId, IdStageModality = 1, Code = "M1_OPEN", Name = "Modalidad 1" });
            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage { Id = newStateId, IdStageModality = 3, Code = "M2_OPEN", Name = "Modalidad 2" });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new StageModality { Id = 1, IdModality = 1, Code = "S1", Name = "Etapa M1", StatusRegister = true });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(3))
                .ReturnsAsync(new StageModality { Id = 3, IdModality = 2, Code = "S3", Name = "Etapa M2", StatusRegister = true });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId, allowCrossStageWithinSameModality: true);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede transicionar hacia un estado perteneciente a otra modalidad.");
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCurrentStateIsRejectionState_ThrowsInvalidOperationExceptionEvenIfNextStage()
        {
            // Arrange
            const int originalStateId = 12;
            const int newStateId = 22;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    IdStageModality = 1,
                    Code = "STAGE1_RECHAZADO",
                    Name = "Fase 1 Rechazada",
                    IsRejectionState = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    IdStageModality = 2,
                    Code = "STAGE2_INICIAL",
                    Name = "Fase 2 Inicial"
                });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Fase 1 Rechazada'.");
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCurrentStateIsFinalForModalityOverall_ThrowsInvalidOperationException()
        {
            // Arrange
            const int originalStateId = 15;
            const int newStateId = 25;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    IdStageModality = 1,
                    Code = "MODALITY_FINAL",
                    Name = "Modalidad Culminada",
                    IsFinalStateForModalityOverall = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    IdStageModality = 2,
                    Code = "MODALITY_NEXT",
                    Name = "Otra Fase"
                });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Modalidad Culminada'.");
        }

        [Theory]
        [InlineData("CT_RECHAZADO")]
        [InlineData("PA_INSCRIPCION_RECHAZADA")]
        public async Task ModalityStateHelper_WhenNewStateIsRejectionState_MarksInscriptionModalityAsFinalRejection(
            string rejectionCode)
        {
            // Arrange
            const int originalStateId = 1;
            const int newStateId = 2;
            const int inscriptionId = 100;

            var inscription = new InscriptionModality
            {
                Id = inscriptionId,
                IsFinalRejection = false
            };

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    Code = "INICIAL",
                    Name = "Inicial",
                    IsFinalStateForStage = false,
                    IsFinalStateForModalityOverall = false,
                    IsRejectionState = false
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    Code = rejectionCode,
                    Name = "Rechazado",
                    IsRejectionState = true
                });

            _inscriptionRepoMock.Setup(i => i.GetByIdAsync(inscriptionId))
                .ReturnsAsync(inscription);

            // Act
            await ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, inscriptionId, originalStateId, newStateId);

            // Assert
            inscription.IsFinalRejection.Should().BeTrue();
            _inscriptionRepoMock.Verify(i => i.UpdatePartialAsync(inscription, It.IsAny<System.Linq.Expressions.Expression<Func<InscriptionModality, object?>>[]>()), Times.Once);
        }

        [Fact]
        public async Task PatchProposal_WhenCurrentStateIsFinal_ThrowsInvalidOperationException()
        {
            // Arrange
            var propRepoMock = new Mock<IRepository<Proposal, int>>();
            var mapperMock = new Mock<IMapper>();
            var jobEnqueuerMock = new Mock<IJobEnqueuer>();
            var loggerMock = new Mock<ILogger<PatchProposalCommandHandler>>();

            const int proposalId = 5;
            const int originalStateId = 30; // Final

            propRepoMock.Setup(r => r.GetByIdAsync(proposalId))
                .ReturnsAsync(new Proposal { Id = proposalId, IdStateStage = originalStateId });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    Code = "PROP_PERTINENTE",
                    Name = "Pertinente",
                    IsFinalStateForStage = true
                });

            var handler = new PatchProposalCommandHandler(
                propRepoMock.Object,
                _unitOfWorkMock.Object,
                mapperMock.Object,
                jobEnqueuerMock.Object,
                loggerMock.Object);

            var command = new PatchProposalCommand(
                proposalId,
                new ProposalPatchDto { IdStateStage = 40 },
                new CurrentUserInfo
                {
                    UserId = 1,
                    Permissions = new HashSet<string> { PermissionCodes.ProyectoGrado.N4PGPPR }
                });

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Pertinente'.");
        }

        [Fact]
        public async Task PatchCoTerminal_WhenCurrentStateIsFinal_ThrowsInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<IRepository<CoTerminal, int>>();
            var mapperMock = new Mock<IMapper>();
            var jobEnqueuerMock = new Mock<IJobEnqueuer>();
            var loggerMock = new Mock<ILogger<PatchCoTerminalCommandHandler>>();

            const int id = 7;
            const int originalStateId = 32;

            repoMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(new CoTerminal { Id = id, IdStateStage = originalStateId });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    Code = "CT_RECHAZADO",
                    Name = "Rechazado",
                    IsFinalStateForModalityOverall = true
                });

            var handler = new PatchCoTerminalCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                mapperMock.Object,
                jobEnqueuerMock.Object,
                loggerMock.Object);

            var command = new PatchCoTerminalCommand(
                id,
                new CoTerminalPatchDto { IdStateStage = 9 },
                new CurrentUserInfo
                {
                    UserId = 1,
                    Permissions = new HashSet<string> { PermissionCodes.CoTerminal.N3CTSGR }
                });

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Rechazado'.");
        }

        [Fact]
        public async Task UpdateAcademicPracticePhase_WhenCurrentStateIsFinal_ThrowsInvalidOperationException()
        {
            // Arrange
            var practiceRepoMock = new Mock<IAcademicPracticeRepository>();
            var jobEnqueuerMock = new Mock<IJobEnqueuer>();

            const int practiceId = 12;
            const int originalStateId = 1;
            const int targetStateId = 10;
            const int stageId = 1;

            practiceRepoMock.Setup(r => r.GetByIdAsync(practiceId))
                .ReturnsAsync(new AcademicPractice { Id = practiceId, IdStateStage = originalStateId });

            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(stageId))
                .ReturnsAsync(new StageModality
                {
                    Id = stageId,
                    IdModality = 2,
                    Code = StageModalityCodes.PaFaseInscripcion,
                    Name = "PA Fase Inscripcion",
                    StatusRegister = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    IdStageModality = stageId,
                    Code = StateStageCodes.PaInscripcionAprobada,
                    Name = "Inscripción Aprobada",
                    IsFinalStateForStage = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(targetStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = targetStateId,
                    IdStageModality = stageId,
                    Code = StateStageCodes.PaInscripcionPendDoc,
                    Name = "Pendiente Documentos",
                    IsFinalStateForStage = false
                });

            var handler = new UpdateAcademicPracticePhaseCommandHandler(
                practiceRepoMock.Object,
                _unitOfWorkMock.Object,
                jobEnqueuerMock.Object);

            var command = new UpdateAcademicPracticePhaseCommand(
                new UpdatePhaseApprovalDto
                {
                    Id = practiceId,
                    NewStateStageId = targetStateId
                },
                new CurrentUserInfo
                {
                    UserId = 1,
                    Permissions = new HashSet<string> { PermissionCodes.PracticaAcademica.N4PAF0PG }
                });

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Inscripción Aprobada'.");
        }

        [Fact]
        public async Task UpdateAcademicPracticePhase_WhenAdvancingFromApprovedPreviousStage_AllowsConfirmedStudentAndBlocksUnapprovedPreviousStage()
        {
            // Arrange
            const int practiceId = 158;
            const int f0StageId = 20;
            const int f1StageId = 21;
            const int f0ApprovedStateId = 200;
            const int f0PendingStateId = 201;
            const int f1ReviewStateId = 202;

            var practice = new AcademicPractice { Id = practiceId, IdStateStage = f0ApprovedStateId };
            var practiceRepoMock = new Mock<IAcademicPracticeRepository>();
            practiceRepoMock.Setup(r => r.GetByIdAsync(practiceId)).ReturnsAsync(practice);
            practiceRepoMock.Setup(r => r.UpdateAcademicPracticeStateAsync(
                    practiceId, f1ReviewStateId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var uimRepoMock = new Mock<IRepository<UserInscriptionModality, int>>();
            uimRepoMock.Setup(r => r.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserInscriptionModality, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.GetRepository<UserInscriptionModality, int>())
                .Returns(uimRepoMock.Object);

            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(f0StageId))
                .ReturnsAsync(new StageModality
                {
                    Id = f0StageId,
                    IdModality = 2,
                    Code = StageModalityCodes.PaFaseInscripcion,
                    Name = "PA Fase Inscripcion",
                    StageOrder = 1,
                    StatusRegister = true
                });
            _stageModalityRepoMock.Setup(s => s.GetByIdAsync(f1StageId))
                .ReturnsAsync(new StageModality
                {
                    Id = f1StageId,
                    IdModality = 2,
                    Code = StageModalityCodes.PaFaseDesarrollo,
                    Name = "PA Fase Desarrollo",
                    StageOrder = 2,
                    StatusRegister = true
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(f0ApprovedStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = f0ApprovedStateId,
                    IdStageModality = f0StageId,
                    Code = StateStageCodes.PaInscripcionAprobada,
                    Name = "Inscripción Aprobada",
                    IsFinalStateForStage = true
                });
            _stateStageRepoMock.Setup(s => s.GetByIdAsync(f0PendingStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = f0PendingStateId,
                    IdStageModality = f0StageId,
                    Code = StateStageCodes.PaInscripcionEnRevision,
                    Name = "Inscripción En Revisión",
                    IsFinalStateForStage = false
                });
            _stateStageRepoMock.Setup(s => s.GetByIdAsync(f1ReviewStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = f1ReviewStateId,
                    IdStageModality = f1StageId,
                    Code = "PA_DESARROLLO_EN_REVISION",
                    Name = "Desarrollo En Revisión",
                    IsFinalStateForStage = false
                });

            var handler = new UpdateAcademicPracticePhaseCommandHandler(
                practiceRepoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IJobEnqueuer>());

            var studentUser = new CurrentUserInfo
            {
                UserId = 3,
                Permissions = new HashSet<string> { PermissionCodes.PracticaAcademica.N3PAF1R }
            };

            // Act 1: Fase 0 aprobada -> estudiante confirmado con N3PAF1R avanza a Fase 1
            var allowed = await handler.Handle(
                new UpdateAcademicPracticePhaseCommand(
                    new UpdatePhaseApprovalDto { Id = practiceId, NewStateStageId = f1ReviewStateId },
                    studentUser),
                CancellationToken.None);

            // Assert 1
            allowed.Should().BeTrue();
            practiceRepoMock.Verify(r => r.UpdateAcademicPracticeStateAsync(
                practiceId, f1ReviewStateId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);

            // Act 2: Fase 0 NO aprobada aún -> debe bloquear con 403 ForbiddenAccessException
            practice.IdStateStage = f0PendingStateId;
            Func<Task> blockedAct = () => handler.Handle(
                new UpdateAcademicPracticePhaseCommand(
                    new UpdatePhaseApprovalDto { Id = practiceId, NewStateStageId = f1ReviewStateId },
                    studentUser),
                CancellationToken.None);

            // Assert 2
            await blockedAct.Should().ThrowAsync<ForbiddenAccessException>();
        }

        public static IEnumerable<object[]> InvalidCurrentUserCases()
        {
            yield return [new CurrentUserInfo { UserId = null, Permissions = new HashSet<string> { PermissionCodes.ProyectoGrado.N4PGPPR } }];
            yield return [new CurrentUserInfo { UserId = 0, Permissions = new HashSet<string> { PermissionCodes.ProyectoGrado.N4PGPPR } }];
            yield return [new CurrentUserInfo { UserId = 1, Permissions = new HashSet<string>() }];
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task PatchProposal_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var repoMock = new Mock<IRepository<Proposal, int>>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Proposal { Id = 1, IdStateStage = 36 });

            var handler = new PatchProposalCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<ILogger<PatchProposalCommandHandler>>());

            Func<Task> act = () => handler.Handle(
                new PatchProposalCommand(1, new ProposalPatchDto { Observation = "Obs" }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task PatchCoTerminal_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var repoMock = new Mock<IRepository<CoTerminal, int>>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CoTerminal { Id = 1, IdStateStage = 9 });

            var handler = new PatchCoTerminalCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<ILogger<PatchCoTerminalCommandHandler>>());

            Func<Task> act = () => handler.Handle(
                new PatchCoTerminalCommand(1, new CoTerminalPatchDto { Observations = "Obs" }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task UpdateAcademicPracticePhase_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var practiceRepoMock = new Mock<IAcademicPracticeRepository>();
            practiceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new AcademicPractice { Id = 1, IdStateStage = 10 });

            var handler = new UpdateAcademicPracticePhaseCommandHandler(
                practiceRepoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IJobEnqueuer>());

            Func<Task> act = () => handler.Handle(
                new UpdateAcademicPracticePhaseCommand(new UpdatePhaseApprovalDto { Id = 1, NewStateStageId = 10 }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task PatchPreliminaryProject_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var repoMock = new Mock<IRepository<PreliminaryProject, int>>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new PreliminaryProject { Id = 1, IdStateStage = 5 });

            var handler = new PatchPreliminaryProjectCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<ILogger<PatchPreliminaryProjectCommandHandler>>());

            Func<Task> act = () => handler.Handle(
                new PatchPreliminaryProjectCommand(1, new PreliminaryProjectPatchDto { Observations = "Obs" }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task PatchProjectFinal_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var repoMock = new Mock<IRepository<ProjectFinal, int>>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ProjectFinal { Id = 1, IdStateStage = 8 });

            var handler = new PatchProjectFinalCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<ILogger<PatchProjectFinalCommandHandler>>());

            Func<Task> act = () => handler.Handle(
                new PatchProjectFinalCommand(1, new ProjectFinalPatchDto { Observations = "Obs" }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCurrentUserCases))]
        public async Task PatchProjectPresentation_WhenCurrentUserHasNoValidActorOrPermissions_ThrowsForbiddenAccessException(CurrentUserInfo currentUser)
        {
            var repoMock = new Mock<IRepository<ProjectPresentation, int>>();
            repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ProjectPresentation { Id = 1, IdStateStage = 13 });

            var handler = new PatchProjectPresentationCommandHandler(
                repoMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IJobEnqueuer>(),
                Mock.Of<ILogger<PatchProjectPresentationCommandHandler>>());

            Func<Task> act = () => handler.Handle(
                new PatchProjectPresentationCommand(1, new ProjectPresentationPatchDto { LocationOrLink = "Sala 1" }, currentUser),
                CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }
    }
}
