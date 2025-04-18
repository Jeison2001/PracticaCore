using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class UserPermissionTests
    {
        [Fact]
        public void UserPermission_Should_Have_Default_Values()
        {
            // Arrange & Act
            var userPermission = new UserPermission();

            // Assert
            Assert.Equal(0, userPermission.IdUser);
            Assert.Equal(0, userPermission.IdPermission);
            Assert.Null(userPermission.IdUserCreatedAt);
        }

        [Fact]
        public void UserPermission_Should_Allow_Setting_Properties()
        {
            // Arrange
            var userPermission = new UserPermission();

            // Act
            userPermission.IdUser = 1;
            userPermission.IdPermission = 2;
            userPermission.IdUserCreatedAt = 3;

            // Assert
            Assert.Equal(1, userPermission.IdUser);
            Assert.Equal(2, userPermission.IdPermission);
            Assert.Equal(3, userPermission.IdUserCreatedAt);
        }

        [Fact]
        public void UserPermission_Should_Initialize_Navigation_Properties()
        {
            // Arrange & Act
            var userPermission = new UserPermission();

            // Assert
            Assert.Null(userPermission.User);
            Assert.Null(userPermission.Permission);
        }
    }
}