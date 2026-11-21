using Domain.Common.Notifications;
using Domain.Interfaces.Services.Notifications;
using FluentAssertions;
using Infrastructure.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Notifications
{
    public class EmailNotificationQueueServiceTests
    {
        private readonly Mock<IEmailNotificationEventService> _eventServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ILogger<EmailNotificationQueueService>> _loggerMock = new();

        [Fact]
        public async Task SendDirectEmailAsync_WhenSendEmailReturnsFalse_ThrowsInvalidOperationExceptionForHangfireRetry()
        {
            _notificationServiceMock
                .Setup(s => s.SendEmailAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = new EmailNotificationQueueService(
                _eventServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object);

            var act = () => sut.SendDirectEmailAsync(
                "destino@unicesar.edu.co",
                "Asunto de prueba",
                "<p>Cuerpo</p>");

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("No se pudo enviar el email directo a 'destino@unicesar.edu.co'.");
        }

        [Fact]
        public async Task SendDirectEmailAsync_WhenSendEmailReturnsTrue_CompletesWithoutThrowing()
        {
            EmailNotification? captured = null;
            _notificationServiceMock
                .Setup(s => s.SendEmailAsync(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
                .Callback<EmailNotification, CancellationToken>((n, _) => captured = n)
                .ReturnsAsync(true);

            var sut = new EmailNotificationQueueService(
                _eventServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object);

            var act = () => sut.SendDirectEmailAsync(
                "destino@unicesar.edu.co",
                "Asunto OK",
                "<p>Hola</p>",
                isHtml: true,
                cc: new[] { "copia@unicesar.edu.co" },
                bcc: new[] { "oculta@unicesar.edu.co" });

            await act.Should().NotThrowAsync();
            captured.Should().NotBeNull();
            captured!.To.Should().Be("destino@unicesar.edu.co");
            captured.Subject.Should().Be("Asunto OK");
            captured.Cc.Should().ContainSingle().Which.Should().Be("copia@unicesar.edu.co");
            captured.Bcc.Should().ContainSingle().Which.Should().Be("oculta@unicesar.edu.co");
        }
    }
}
