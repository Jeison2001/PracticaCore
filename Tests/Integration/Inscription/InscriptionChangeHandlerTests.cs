using Application.Common.Services.Notifications.Handlers;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.Inscription
{
    /// <summary>
    /// Tests para InscriptionChangeHandler - el handler que procesa cambios de estado
    /// de inscripciones y encola notificaciones.
    /// </summary>
    public class InscriptionChangeHandlerTests
    {
        private readonly Mock<IEmailNotificationQueueService> _mockQueueService;
        private readonly Mock<IInscriptionEventDataBuilder> _mockEventDataBuilder;
        private readonly Mock<ILogger<InscriptionChangeHandler>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<StateInscription, int>> _mockStateInscriptionRepo;

        public InscriptionChangeHandlerTests()
        {
            _mockQueueService = new Mock<IEmailNotificationQueueService>();
            _mockEventDataBuilder = new Mock<IInscriptionEventDataBuilder>();
            _mockLogger = new Mock<ILogger<InscriptionChangeHandler>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStateInscriptionRepo = new Mock<IRepository<StateInscription, int>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<StateInscription, int>())
                .Returns(_mockStateInscriptionRepo.Object);
        }

        [Theory]
        [InlineData(StateInscriptionCodes.Pendiente, "INSCRIPTION_CREATED")]
        [InlineData(StateInscriptionCodes.Aprobado, "INSCRIPTION_APPROVED")]
        [InlineData(StateInscriptionCodes.Rechazado, "INSCRIPTION_REJECTED")]
        public async Task HandleChangeAsync_StateChangedToKnownState_EnqueuesCorrectEvent(
            string stateCode, string expectedEventName)
        {
            // Arrange
            var handler = CreateHandler();

            // IDs de estado según SeedingUtilities:
            // 1 = PENDIENTE, 2 = APROBADO, 3 = RECHAZADO
            int GetStateId(string code) => code switch
            {
                StateInscriptionCodes.Pendiente => 1,
                StateInscriptionCodes.Aprobado => 2,
                StateInscriptionCodes.Rechazado => 3,
                _ => throw new ArgumentException($"Unknown state code: {code}")
            };

            int newStateId = GetStateId(stateCode);
            int oldStateId = newStateId == 1 ? 2 : 1; // Para PENDIENTE usamos old=2, para otros usamos old=1

            var oldEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = oldStateId
            };

            var newEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = newStateId
            };

            SetupStateRepository(stateCode);

            _mockQueueService.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns("test-job-id");

            _mockEventDataBuilder.Setup(b => b.BuildInscriptionEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { { "Test", "Data" } });

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                expectedEventName,
                It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task HandleChangeAsync_StateNotChanged_DoesNotEnqueueNotification()
        {
            // Arrange
            var handler = CreateHandler();

            var entity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 1 // PENDIENTE
            };

            // Act - same entity passed as old and new (no state change)
            await handler.HandleChangeAsync(entity, entity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task HandleChangeAsync_UnknownStateCode_DoesNotEnqueueNotification()
        {
            // Arrange
            var handler = CreateHandler();

            var oldEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 1
            };

            var newEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 99 // Unknown state
            };

            _mockStateInscriptionRepo.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync(new StateInscription { Id = 99, Code = "UNKNOWN_STATE", Name = "Unknown" });

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task HandleChangeAsync_StateNotFoundInDatabase_DoesNotEnqueueNotification()
        {
            // Arrange
            var handler = CreateHandler();

            var oldEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 1
            };

            var newEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 999
            };

            _mockStateInscriptionRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((StateInscription?)null);

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task HandleChangeAsync_ValidTransition_ReturnsCorrectJobId()
        {
            // Arrange
            var expectedJobId = "job-12345";
            var handler = CreateHandler();

            var oldEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 1 // PENDIENTE
            };

            var newEntity = new InscriptionModality
            {
                Id = 1,
                IdStateInscription = 2 // APROBADO
            };

            SetupStateRepository(StateInscriptionCodes.Aprobado);

            _mockQueueService.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns(expectedJobId);

            _mockEventDataBuilder.Setup(b => b.BuildInscriptionEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object>());

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                "INSCRIPTION_APPROVED",
                It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        private InscriptionChangeHandler CreateHandler()
        {
            return new InscriptionChangeHandler(
                _mockQueueService.Object,
                _mockEventDataBuilder.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        private void SetupStateRepository(string stateCode)
        {
            int stateId = stateCode switch
            {
                StateInscriptionCodes.Pendiente => 1,
                StateInscriptionCodes.Aprobado => 2,
                StateInscriptionCodes.Rechazado => 3,
                _ => 99
            };

            var state = new StateInscription
            {
                Id = stateId,
                Code = stateCode,
                Name = stateCode
            };

            _mockStateInscriptionRepo.Setup(r => r.GetByIdAsync(stateId))
                .ReturnsAsync(state);
        }
    }
}
