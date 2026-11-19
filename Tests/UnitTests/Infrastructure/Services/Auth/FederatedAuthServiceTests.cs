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
        public async Task AuthenticateWithTokenAsync_NewUser_CreatesUser()
        {
            var token = "valid_token";
            var email = "new@unicesar.edu.co";
            var payload = new TokenPayload 
            { 
                Email = email, 
                GivenName = "New", 
                FamilyName = "User" 
            };
            
            var newUser = new User 
            { 
                Id = 2, 
                Email = email, 
                FirstName = "New", 
                LastName = "User" 
            };

            var loginData = new UserLoginData
            {
                Roles = new List<RoleInfoResult>(),
                Permissions = new List<PermissionInfo>()
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ReturnsAsync(payload);

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(email))
                .ReturnsAsync((User?)null);

            _userInfoRepositoryMock.Setup(x => x.CreateUserIfNotExistsAsync(email, "New", "User"))
                .ReturnsAsync(newUser);

            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(newUser.Id))
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
            Assert.Equal(newUser.Email, result.User.Email);
            _userInfoRepositoryMock.Verify(x => x.CreateUserIfNotExistsAsync(email, "New", "User"), Times.Once);
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_UnregisteredProvider_ThrowsUnauthorizedAccessException()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AuthenticateWithTokenAsync("any_token", "azure"));
        }

        [Fact]
        public async Task AuthenticateWithTokenAsync_NormalizesEmailBeforeLookupAndCreation()
        {
            var token = "valid_token";
            var normalizedEmail = "mixed.user@unicesar.edu.co";
            var payload = new TokenPayload
            {
                Email = "  Mixed.User@UNICESAR.EDU.CO  ",
                GivenName = "Mixed",
                FamilyName = "User"
            };

            var createdUser = new User
            {
                Id = 3,
                Email = normalizedEmail,
                FirstName = "Mixed",
                LastName = "User",
                StatusRegister = true
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token)).ReturnsAsync(payload);
            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(normalizedEmail)).ReturnsAsync((User?)null);
            _userInfoRepositoryMock.Setup(x => x.CreateUserIfNotExistsAsync(normalizedEmail, "Mixed", "User")).ReturnsAsync(createdUser);
            _userInfoRepositoryMock.Setup(x => x.GetUserLoginDataAsync(createdUser.Id)).ReturnsAsync(new UserLoginData());
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
            _userInfoRepositoryMock.Verify(x => x.FindUserByEmailAsync(normalizedEmail), Times.Once);
            _userInfoRepositoryMock.Verify(x => x.CreateUserIfNotExistsAsync(normalizedEmail, "Mixed", "User"), Times.Once);
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