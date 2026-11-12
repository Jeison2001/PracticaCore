using Application.Shared.Commands.InscriptionModalities;
using Application.Shared.Commands.InscriptionModalities.Handlers;
using Application.Shared.DTOs.InscriptionModalities;
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
    public class PatchInscriptionModalityCommandHandlerTests
    {
        private readonly Mock<IRepository<InscriptionModality, int>> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IJobEnqueuer> _jobEnqueuerMock;
        private readonly Mock<ILogger<PatchInscriptionModalityCommandHandler>> _loggerMock;
        private readonly Mock<IRepository<StateInscription, int>> _stateRepoMock;
        private readonly Mock<IRepository<EvaluationType, int>> _evalTypeRepoMock;
        private readonly Mock<IRepository<Evaluation, int>> _evalRepoMock;
        private readonly PatchInscriptionModalityCommandHandler _handler;

        public PatchInscriptionModalityCommandHandlerTests()
        {
            _repositoryMock = new Mock<IRepository<InscriptionModality, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _jobEnqueuerMock = new Mock<IJobEnqueuer>();
            _loggerMock = new Mock<ILogger<PatchInscriptionModalityCommandHandler>>();
            _stateRepoMock = new Mock<IRepository<StateInscription, int>>();
            _evalTypeRepoMock = new Mock<IRepository<EvaluationType, int>>();
            _evalRepoMock = new Mock<IRepository<Evaluation, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<StateInscription, int>())
                .Returns(_stateRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<EvaluationType, int>())
                .Returns(_evalTypeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Evaluation, int>())
                .Returns(_evalRepoMock.Object);

            _handler = new PatchInscriptionModalityCommandHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _jobEnqueuerMock.Object,
                _loggerMock.Object);
        }

        [Theory]
        [InlineData(StateInscriptionCodes.Rechazado, "Rechazado")]
        [InlineData(StateInscriptionCodes.Aprobado, "Aprobado")]
        public async Task Handle_WhenCurrentStateIsFinalState_ThrowsInvalidOperationException(string stateCode, string stateName)
        {
            // Arrange
            const int inscriptionId = 10;
            const int originalStateId = 1;
            const int targetStateId = 2;

            var existingEntity = new InscriptionModality
            {
                Id = inscriptionId,
                IdStateInscription = originalStateId,
                StatusRegister = true
            };

            var finalState = new StateInscription
            {
                Id = originalStateId,
                Code = stateCode,
                Name = stateName,
                IsFinalStateForStage = true
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(inscriptionId))
                .ReturnsAsync(existingEntity);

            _stateRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(finalState);

            var command = new PatchInscriptionModalityCommand(
                inscriptionId,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = targetStateId,
                    Observations = "Intento de cambio en estado final"
                },
                new CurrentUserInfo { UserId = 99 });

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"No se puede modificar el estado de una inscripción que ya se encuentra en estado '{stateName}'.");
        }

        [Fact]
        public async Task Handle_WhenCurrentStateIsNotFinal_AllowsStateModification()
        {
            // Arrange
            const int inscriptionId = 20;
            const int originalStateId = 1; // PENDIENTE
            const int targetStateId = 2;   // APROBADO

            var existingEntity = new InscriptionModality
            {
                Id = inscriptionId,
                IdStateInscription = originalStateId,
                StatusRegister = true
            };

            var pendingState = new StateInscription
            {
                Id = originalStateId,
                Code = StateInscriptionCodes.Pendiente,
                Name = "Pendiente",
                IsFinalStateForStage = false
            };

            var approvedState = new StateInscription
            {
                Id = targetStateId,
                Code = StateInscriptionCodes.Aprobado,
                Name = "Aprobado",
                IsFinalStateForStage = true
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(inscriptionId))
                .ReturnsAsync(existingEntity);

            _stateRepoMock.Setup(s => s.GetByIdAsync(originalStateId))
                .ReturnsAsync(pendingState);

            _stateRepoMock.Setup(s => s.GetByIdAsync(targetStateId))
                .ReturnsAsync(approvedState);

            var expectedDto = new InscriptionModalityDto
            {
                Id = inscriptionId,
                IdStateInscription = targetStateId
            };
            _mapperMock.Setup(m => m.Map<InscriptionModalityDto>(existingEntity))
                .Returns(expectedDto);

            var command = new PatchInscriptionModalityCommand(
                inscriptionId,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = targetStateId,
                    Observations = "Aprobado formalmente"
                },
                new CurrentUserInfo { UserId = 42 });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IdStateInscription.Should().Be(targetStateId);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
