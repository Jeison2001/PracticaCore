using Application.Features.AcademicPractices.EventHandlers;
using Domain.Constants;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Integration;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.EventHandlers
{
    public class CreateAcademicPracticeOnApprovalHandlerTests : IntegrationTestBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateAcademicPracticeOnApprovalHandler _handler;

        public CreateAcademicPracticeOnApprovalHandlerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            SeedingUtilities.SeedCatalogs(_context);

            _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logger = new Mock<ILogger<CreateAcademicPracticeOnApprovalHandler>>().Object;
            
            _handler = new CreateAcademicPracticeOnApprovalHandler(_unitOfWork, logger);
        }

        [Fact]
        public async Task Handle_Should_Create_AcademicPractice_And_Initial_Phase_When_Inscription_Is_Approved()
        {
            // Arrange
            var user = new User { Id = 1, Email = "testuser@example.com", StatusRegister = true, OperationRegister = "Test" };
            _context.Set<User>().Add(user);

            var inscription = new InscriptionModality
            {
                Id = 10,
                IdModality = 2, // Práctica Académica
                IdStateInscription = 2, // Aprobado
                IdAcademicPeriod = 1,
                IdUserCreatedAt = 1,
                StatusRegister = true,
                OperationRegister = "Test"
            };
            _context.Set<InscriptionModality>().Add(inscription);

            var userInscription = new UserInscriptionModality
            {
                Id = 1,
                IdInscriptionModality = 10,
                IdUser = 1,
                StatusRegister = true,
                OperationRegister = "Test",
                IdUserCreatedAt = 1
            };
            _context.Set<UserInscriptionModality>().Add(userInscription);

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var domainEvent = new InscriptionStateChangedEvent(
                InscriptionModalityId: 10,
                ModalityId: 2,
                NewStateInscriptionId: 2, // Aprobado
                TriggeredByUserId: 1
            );

            // Act
            await _handler.Handle(domainEvent, CancellationToken.None);
            await _context.SaveChangesAsync();

            // Assert
            var practice = await _context.Set<AcademicPractice>().FirstOrDefaultAsync(x => x.Id == 10);
            practice.Should().NotBeNull();
            practice!.IdStateStage.Should().Be(10); // PaInscripcionPendDoc
            practice.OperationRegister.Should().Contain("Creada automáticamente");

            var updatedInscription = await _context.Set<InscriptionModality>().FirstOrDefaultAsync(x => x.Id == 10);
            updatedInscription!.IdStageModality.Should().Be(1); // Fase_Inscripcion
        }
    }
}
