using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Auth;
using Infrastructure.Services.Auth;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class GoogleAuthServiceTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IUserInfoRepository> _userInfoRepositoryMock;
        private readonly Mock<IRepository<Role, int>> _roleRepositoryMock;
        private readonly Mock<ITokenValidator> _tokenValidatorMock;
        private readonly GoogleAuthService _service;

        public GoogleAuthServiceTests()
        {
            _jwtServiceMock = new Mock<IJwtService>();
            _userInfoRepositoryMock = new Mock<IUserInfoRepository>();
            _roleRepositoryMock = new Mock<IRepository<Role, int>>();
            _tokenValidatorMock = new Mock<ITokenValidator>();

            _service = new GoogleAuthService(
                _jwtServiceMock.Object,
                _userInfoRepositoryMock.Object,
                _roleRepositoryMock.Object,
                _tokenValidatorMock.Object
            );
        }

        [Fact]
        public async Task AuthenticateWithGoogleAsync_ValidToken_ReturnsResult()
        {
            // Arrange
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

            var roles = new List<string> { "Admin" };
            var roleEntities = new List<Role> { new Role { Name = "Admin", Code = "ADMIN" } };
            var permissions = new List<Permission> { new Permission { Code = "PERM_1" } };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ReturnsAsync(payload);

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(email))
                .ReturnsAsync(user);

            _userInfoRepositoryMock.Setup(x => x.GetUserRolesAsync(user.Id))
                .ReturnsAsync(roles);

            _roleRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(roleEntities);

            _userInfoRepositoryMock.Setup(x => x.GetUserPermissionsFullInfoAsync(user.Id))
                .ReturnsAsync(permissions);

            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .Returns("jwt_token");

            // Act
            var result = await _service.AuthenticateWithGoogleAsync(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("jwt_token", result.Token);
            Assert.Equal(email, result.User.Email);
        }

        [Fact]
        public async Task AuthenticateWithGoogleAsync_InvalidToken_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var token = "invalid_token";
            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ThrowsAsync(new Exception("Invalid token"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.AuthenticateWithGoogleAsync(token));
        }

        [Fact]
        public async Task AuthenticateWithGoogleAsync_NewUser_CreatesUser()
        {
            // Arrange
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

            _tokenValidatorMock.Setup(x => x.ValidateAsync(token))
                .ReturnsAsync(payload);

            _userInfoRepositoryMock.Setup(x => x.FindUserByEmailAsync(email))
                .ReturnsAsync((User?)null);

            _userInfoRepositoryMock.Setup(x => x.CreateUserIfNotExistsAsync(email, "New", "User"))
                .ReturnsAsync(newUser);

            _userInfoRepositoryMock.Setup(x => x.GetUserRolesAsync(newUser.Id))
                .ReturnsAsync(new List<string>());

            _roleRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<Role, bool>>>(), It.IsAny<Func<IQueryable<Role>, IOrderedQueryable<Role>>>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Role>());

            _userInfoRepositoryMock.Setup(x => x.GetUserPermissionsFullInfoAsync(newUser.Id))
                .ReturnsAsync(new List<Permission>());

            _jwtServiceMock.Setup(x => x.GenerateTokenWithClaims(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<List<string>>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>()))
                .Returns("jwt_token");

            // Act
            var result = await _service.AuthenticateWithGoogleAsync(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newUser.Email, result.User.Email);
            _userInfoRepositoryMock.Verify(x => x.CreateUserIfNotExistsAsync(email, "New", "User"), Times.Once);
        }
    }
}
