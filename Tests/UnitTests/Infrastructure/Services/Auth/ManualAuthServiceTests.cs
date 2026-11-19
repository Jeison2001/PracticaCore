using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using FluentAssertions;
using Infrastructure.Services.Auth;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class ManualAuthServiceTests
    {
        [Fact]
        public async Task AuthenticateManualAsync_NormalizesEmailBeforeLookup()
        {
            var jwtMock = new Mock<IJwtService>();
            var userRepoMock = new Mock<IUserInfoRepository>();
            var loggerMock = new Mock<ILogger<ManualAuthService>>();

            const string normalizedEmail = "manual.user@unicesar.edu.co";
            var user = new User
            {
                Id = 7,
                Email = normalizedEmail,
                FirstName = "Manual",
                LastName = "User",
                Identification = "999999",
                IdIdentificationType = 1,
                StatusRegister = true
            };

            userRepoMock.Setup(r => r.FindUserByEmailAsync(normalizedEmail)).ReturnsAsync(user);
            userRepoMock.Setup(r => r.GetUserLoginDataAsync(user.Id)).ReturnsAsync(new UserLoginData());
            jwtMock.Setup(j => j.GenerateTokenWithClaims(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns("manual-jwt");

            var service = new ManualAuthService(jwtMock.Object, userRepoMock.Object, loggerMock.Object);

            var result = await service.AuthenticateManualAsync("  Manual.User@UNICESAR.EDU.CO  ", null);

            result.Token.Should().Be("manual-jwt");
            userRepoMock.Verify(r => r.FindUserByEmailAsync(normalizedEmail), Times.Once);
        }

        [Fact]
        public async Task AuthenticateManualAsync_WhenUnexpectedExceptionOccurs_DoesNotLeakInternalMessage()
        {
            var jwtMock = new Mock<IJwtService>();
            var userRepoMock = new Mock<IUserInfoRepository>();
            var loggerMock = new Mock<ILogger<ManualAuthService>>();

            userRepoMock.Setup(r => r.FindUserByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("internal-db-secret-detail"));

            var service = new ManualAuthService(jwtMock.Object, userRepoMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AuthenticateManualAsync("user@unicesar.edu.co", null));

            ex.Message.Should().Be("Error al autenticar el usuario.");
            ex.Message.Should().NotContain("internal-db-secret-detail");
        }
    }
}
