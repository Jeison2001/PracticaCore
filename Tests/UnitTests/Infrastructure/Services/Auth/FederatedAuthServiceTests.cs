using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class FederatedAuthServiceTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IUserInfoRepository> _userInfoRepositoryMock;
        private readonly Mock<ITokenValidator> _tokenValidatorMock;
        private readonly FederatedAuthService _service;

        public FederatedAuthServiceTests()
        {
            _jwtServiceMock = new Mock<IJwtService>();
            _userInfoRepositoryMock = new Mock<IUserInfoRepository>();
            _tokenValidatorMock = new Mock<ITokenValidator>();
            _tokenValidatorMock.SetupGet(x => x.Provider).Returns("google");

            _service = new FederatedAuthService(
                _jwtServiceMock.Object,
                _userInfoRepositoryMock.Object,
                new[] { _tokenValidatorMock.Object }
            );
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_ValidToken_ReturnsResult()
        {
            var token = "valid_token";
            var email = "test@unicesar.edu.co";
            var payload = new TokenPayload 
            { 
                Email = email, 
                GivenName = "Test", 
                FamilyName = "User" 
            };
            
            var user = new User 
            { 
                Id = 1, 
                Email = email, 
                FirstName = "Test", 
                LastName = "User" 
            };

            var loginData = new UserLoginData
            {
                Roles = new List<RoleInfoResult> { new RoleInfoResult { Name = "Admin", Code = "ADMIN" } },
                Permissions = new List<PermissionInfo> { new PermissionInfo { Code = "PERM_1" } }
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ReturnsAsync(payload);

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(email))
                .ReturnsAsync(user);

            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(user.Id))
                .ReturnsAsync(loginData);

            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .Returns("jwt_token");

            var result = await _service.AuthenticateWithTokenAsync(token, "google");

            Assert.NotNull(result);
            Assert.Equal("jwt_token", result.Token);
            Assert.Equal(email, result.User.Email);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_InvalidToken_ThrowsUnauthorizedAccessException()
        {
            var token = "invalid_token";
            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ThrowsAsync(new Exception("Invalid token sensitive-detail"));

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync(token, "google"));

            Assert.Equal("Error al autenticar con el proveedor de identidad.", ex.Message);
            Assert.DoesNotContain("sensitive-detail", ex.Message);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_UnregisteredUser_ThrowsUsuarioNoRegistrado()
        {
            var token = "valid_token";
            var email = "new@unicesar.edu.co";
            var payload = new TokenPayload 
            { 
                Email = email, 
                GivenName = "New", 
                FamilyName = "User" 
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ReturnsAsync(payload);

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(email))
                .ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync(token, "google"));

            Assert.Equal(
                $"USUARIO_NO_REGISTRADO: El correo '{email}' no se encuentra registrado. Complete primero su registro institucional.",
                ex.Message);
            _userInfoRepositoryMock.Verify(x => x.GetUserLoginDataAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_UnregisteredProvider_ThrowsUnauthorizedAccessException()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync("any_token", "azure"));
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_NormalizesEmailBeforeLookupAndUnregisteredError()
        {
            var token = "valid_token";
            var normalizedEmail = "mixed.user@unicesar.edu.co";
            var payload = new TokenPayload
            {
                Email = "  Mixed.User@UNICESAR.EDU.CO  ",
                GivenName = "Mixed",
                FamilyName = "User"
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token)).ReturnsAsync(payload);
            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(normalizedEmail)).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync(token, "google"));

            Assert.Equal(
                $"USUARIO_NO_REGISTRADO: El correo '{normalizedEmail}' no se encuentra registrado. Complete primero su registro institucional.",
                ex.Message);
            _userInfoRepositoryMock.Verify(x => x.FindUserByEmailAsync(normalizedEmail), Times.Once);

            var existingUser = new User
            {
                Id = 3,
                Email = normalizedEmail,
                FirstName = "Mixed",
                LastName = "User",
                StatusRegister = true
            };
            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(normalizedEmail)).ReturnsAsync(existingUser);
            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(existingUser.Id)).ReturnsAsync(new UserLoginData());
            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns("jwt_token");

            var result = await _service.AuthenticateWithTokenAsync(token, "google");

            Assert.Equal(normalizedEmail, result.User.Email);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_WhenNoDomainsConfigured_FallsBackToRegisterDomainsOrInstitutionalDefault()
        {
            // 1. Sin configuración alguna (_configuration == null en _service): fail-closed a unicesar.edu.co
            _tokenValidatorMock.Setup(x => x.ValidateAsync("ext_token"))
                .ReturnsAsync(new TokenPayload { Email = "external@gmail.com", GivenName = "Ext", FamilyName = "User" });

            var defaultEx = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync("ext_token", "google"));
            Assert.Equal("El dominio de correo no está autorizado para acceder al sistema.", defaultEx.Message);

            // 2. Fallback a Authentication:Register:AllowedDomains cuando ni proveedor ni Authentication:AllowedDomains existen
            var registerConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Register:AllowedDomains:0"] = "aliado.edu.co"
                })
                .Build();

            var registerFallbackService = new FederatedAuthService(
                _jwtServiceMock.Object,
                _userInfoRepositoryMock.Object,
                new[] { _tokenValidatorMock.Object },
                registerConfig);

            _tokenValidatorMock.Setup(x => x.ValidateAsync("ally_token"))
                .ReturnsAsync(new TokenPayload { Email = "docente@aliado.edu.co", GivenName = "Doc", FamilyName = "Ally" });
            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync("docente@aliado.edu.co"))
                .ReturnsAsync(new User { Id = 22, Email = "docente@aliado.edu.co", FirstName = "Doc", LastName = "Ally", StatusRegister = true });
            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(22)).ReturnsAsync(new UserLoginData());
            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns("ally_jwt");

            var allyResult = await registerFallbackService.AuthenticateWithTokenAsync("ally_token", "google");
            Assert.Equal("ally_jwt", allyResult.Token);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_WithAllowedDomainsConfigured_RejectsUnmatchedDomain()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:AllowedDomains:0"] = "unicesar.edu.co"
                })
                .Build();

            var service = new FederatedAuthService(
                _jwtServiceMock.Object,
                _userInfoRepositoryMock.Object,
                new[] { _tokenValidatorMock.Object },
                config);

            _tokenValidatorMock.Setup(x => x.ValidateAsync("token"))
                .ReturnsAsync(new TokenPayload { Email = "outsider@gmail.com", GivenName = "Out", FamilyName = "Sider" });

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AuthenticateWithTokenAsync("token", "google"));

            Assert.Equal("El dominio de correo no está autorizado para acceder al sistema.", ex.Message);
            _userInfoRepositoryMock.Verify(x => x.FindUserByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_WithAllowedDomainsConfigured_AllowsMatchingDomainCaseInsensitive()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:AllowedDomains:0"] = "UniCesar.EDU.CO"
                })
                .Build();

            var service = new FederatedAuthService(
                _jwtServiceMock.Object,
                _userInfoRepositoryMock.Object,
                new[] { _tokenValidatorMock.Object },
                config);

            var normalizedEmail = "student@unicesar.edu.co";
            _tokenValidatorMock.Setup(x => x.ValidateAsync("token"))
                .ReturnsAsync(new TokenPayload { Email = "  Student@UNICESAR.EDU.CO  ", GivenName = "Stu", FamilyName = "Dent" });

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(normalizedEmail))
                .ReturnsAsync(new User { Id = 10, Email = normalizedEmail, FirstName = "Stu", LastName = "Dent", StatusRegister = true });
            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(10)).ReturnsAsync(new UserLoginData());
            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).Returns("jwt_token");

            var result = await service.AuthenticateWithTokenAsync("token", "google");

            Assert.Equal("jwt_token", result.Token);
        }
    }
}