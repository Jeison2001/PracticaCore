using Application.Common.Services.Notifications.Handlers;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Integration.AcademicAverages
{
    /// <summary>
    /// Tests para AcademicAverageChangeHandler - el handler que procesa cambios de estado
    /// de la modalidad Grado por Promedio (GRADO_PROMEDIO) y encola notificaciones.
    /// Espejo de InscriptionChangeHandlerTests, adaptado al patrón de modalidades menores
    /// (mapeo StateStage.Code -> eventName).
    /// </summary>
    public class AcademicAverageChangeHandlerTests
    {
        private readonly Mock<IEmailNotificationQueueService> _mockQueueService;
        private readonly Mock<IAcademicAverageEventDataBuilder> _mockEventDataBuilder;
        private readonly Mock<ILogger<AcademicAverageChangeHandler>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<StateStage, int>> _mockStateStageRepo;

        public AcademicAverageChangeHandlerTests()
        {
            _mockQueueService = new Mock<IEmailNotificationQueueService>();
            _mockEventDataBuilder = new Mock<IAcademicAverageEventDataBuilder>();
            _mockLogger = new Mock<ILogger<AcademicAverageChangeHandler>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStateStageRepo = new Mock<IRepository<StateStage, int>>();

            _mockUnitOfWork.Setup(u => u.GetRepository<StateStage, int>())
                .Returns(_mockStateStageRepo.Object);
        }

        [Theory]
        [InlineData("GP_RADICADO", "GP_SUBMITTED")]
        [InlineData("GP_APROBADO", "GP_APPROVED")]
        [InlineData("GP_CON_OBSERVACIONES", "GP_OBSERVATIONS")]
        [InlineData("GP_RECHAZADO", "GP_REJECTED")]
        public async Task HandleChangeAsync_StateChangedToKnownState_EnqueuesCorrectEvent(
            string stateCode, string expectedEventName)
        {
            // Arrange
            var handler = CreateHandler();

            const int oldStateId = 38; // GP_PENDIENTE_DOCUMENTACION (inicial)
            const int newStateId = 99;

            var oldEntity = new AcademicAverage { Id = 1, IdStateStage = oldStateId };
            var newEntity = new AcademicAverage { Id = 1, IdStateStage = newStateId };

            _mockStateStageRepo.Setup(r => r.GetByIdAsync(newStateId))
                .ReturnsAsync(new StateStage { Id = newStateId, Code = stateCode, Name = stateCode });

            _mockEventDataBuilder.Setup(b => b.BuildEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { { "Test", "Data" } });

            _mockQueueService.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns("test-job-id");

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
            var entity = new AcademicAverage { Id = 1, IdStateStage = 38 };

            // Act - same state, no change
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
            var oldEntity = new AcademicAverage { Id = 1, IdStateStage = 38 };
            var newEntity = new AcademicAverage { Id = 1, IdStateStage = 99 };

            _mockStateStageRepo.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync(new StateStage { Id = 99, Code = "GP_PENDIENTE_DOCUMENTACION", Name = "Pendiente" });

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert - the initial state does not map to any event
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task HandleChangeAsync_StateNotFoundInDatabase_DoesNotEnqueueNotification()
        {
            // Arrange
            var handler = CreateHandler();
            var oldEntity = new AcademicAverage { Id = 1, IdStateStage = 38 };
            var newEntity = new AcademicAverage { Id = 1, IdStateStage = 999 };

            _mockStateStageRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((StateStage?)null);

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task HandleChangeAsync_EmptyEventData_DoesNotEnqueueNotification()
        {
            // Arrange
            var handler = CreateHandler();
            var oldEntity = new AcademicAverage { Id = 1, IdStateStage = 38 };
            var newEntity = new AcademicAverage { Id = 1, IdStateStage = 41 };

            _mockStateStageRepo.Setup(r => r.GetByIdAsync(41))
                .ReturnsAsync(new StateStage { Id = 41, Code = "GP_APROBADO", Name = "Aprobado" });

            _mockEventDataBuilder.Setup(b => b.BuildEventDataAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object>());

            // Act
            await handler.HandleChangeAsync(oldEntity, newEntity);

            // Assert - builder returned empty data, so nothing is enqueued
            _mockQueueService.Verify(q => q.EnqueueEventNotification(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        private AcademicAverageChangeHandler CreateHandler()
        {
            return new AcademicAverageChangeHandler(
                _mockQueueService.Object,
                _mockEventDataBuilder.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }
    }
}
