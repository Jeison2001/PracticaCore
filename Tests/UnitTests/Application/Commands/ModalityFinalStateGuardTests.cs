using Application.Common.Services;
using Application.Shared.Commands.AcademicPractices;
using Application.Shared.Commands.AcademicPractices.Handlers;
using Application.Shared.Commands.CoTerminals;
using Application.Shared.Commands.CoTerminals.Handlers;
using Application.Shared.Commands.Proposals;
using Application.Shared.Commands.Proposals.Handlers;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.CoTerminals;
using Application.Shared.DTOs.Proposals;
using AutoMapper;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
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
        private readonly Mock<IRepository<InscriptionModality, int>> _inscriptionRepoMock;

        public ModalityFinalStateGuardTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _stateStageRepoMock = new Mock<IRepository<StateStage, int>>();
            _inscriptionRepoMock = new Mock<IRepository<InscriptionModality, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<StateStage, int>())
                .Returns(_stateStageRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<InscriptionModality, int>())
                .Returns(_inscriptionRepoMock.Object);
        }

        [Fact]
        public async Task ModalityStateHelper_WhenCurrentStateIsFinalForStage_ThrowsInvalidOperationException()
        {
            // Arrange
            const int originalStateId = 10;
            const int newStateId = 20;

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    Code = "STAGE_FINAL",
                    Name = "Etapa Finalizada",
                    IsFinalStateForStage = true
                });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Etapa Finalizada'.");
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
                    Code = "MODALITY_FINAL",
                    Name = "Modalidad Culminada",
                    IsFinalStateForModalityOverall = true
                });

            // Act
            Func<Task> act = () => ModalityStateHelper.ValidateStateTransitionAndCheckRejectionAsync(
                _unitOfWorkMock.Object, 1, originalStateId, newStateId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Modalidad Culminada'.");
        }

        [Theory]
        [InlineData("PROP_NO_PERTINENTE", false, true, false)]
        [InlineData("CT_RECHAZADO", false, false, true)]
        [InlineData("PA_INSCRIPCION_RECHAZADA", true, false, false)]
        public async Task ModalityStateHelper_WhenNewStateIsRejection_MarksInscriptionModalityAsFinalRejection(
            string rejectionCode, bool isRejectionState, bool isFinalStage, bool isFinalOverall)
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
                    IsFinalStateForModalityOverall = false
                });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = newStateId,
                    Code = rejectionCode,
                    Name = "Rechazado",
                    IsRejectionState = isRejectionState,
                    IsFinalStateForStage = isFinalStage,
                    IsFinalStateForModalityOverall = isFinalOverall
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
                new CurrentUserInfo { UserId = 1 });

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
                new CurrentUserInfo { UserId = 1 });

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

            practiceRepoMock.Setup(r => r.GetByIdAsync(practiceId))
                .ReturnsAsync(new AcademicPractice { Id = practiceId, IdStateStage = originalStateId });

            _stateStageRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(new StateStage
                {
                    Id = originalStateId,
                    Code = StateStageCodes.PaInscripcionAprobada,
                    Name = "Inscripción Aprobada",
                    IsFinalStateForStage = true
                });

            var handler = new UpdateAcademicPracticePhaseCommandHandler(
                practiceRepoMock.Object,
                _unitOfWorkMock.Object,
                jobEnqueuerMock.Object);

            var command = new UpdateAcademicPracticePhaseCommand(
                new UpdatePhaseApprovalDto
                {
                    Id = practiceId,
                    NewStateStageId = 10
                },
                new CurrentUserInfo { UserId = 1 });

            // Act
            Func<Task> act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No se puede modificar el estado de un registro que ya se encuentra en estado 'Inscripción Aprobada'.");
        }
    }
}
