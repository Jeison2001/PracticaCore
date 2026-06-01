using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Services.Auth;
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
                .ThrowsAsync(new Exception("Invalid token"));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.AuthenticateWithTokenAsync(token, "google"));
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
    }
}