using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class RolePermissionTests
    {
        [Fact]
        public void RolePermission_Should_Have_Default_Values()
        {
            // Arrange & Act
            var rolePermission = new RolePermission();

            // Assert
            Assert.Equal(0, rolePermission.IdRole);
            Assert.Equal(0, rolePermission.IdPermission);
            Assert.Null(rolePermission.IdUserCreatedAt);
        }

        [Fact]
        public void RolePermission_Should_Allow_Setting_Properties()
        {
            // Arrange
            var rolePermission = new RolePermission();

            // Act
            rolePermission.IdRole = 1;
            rolePermission.IdPermission = 2;
            rolePermission.IdUserCreatedAt = 3;

            // Assert
            Assert.Equal(1, rolePermission.IdRole);
            Assert.Equal(2, rolePermission.IdPermission);
            Assert.Equal(3, rolePermission.IdUserCreatedAt);
        }

        [Fact]
        public void RolePermission_Should_Initialize_Navigation_Properties()
        {
            // Arrange & Act
            var rolePermission = new RolePermission();

            // Assert
            Assert.Null(rolePermission.Role);
            Assert.Null(rolePermission.Permission);
        }
    }
}