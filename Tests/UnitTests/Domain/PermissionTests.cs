using Domain.Entities;
using Xunit;
using System.Collections.Generic;

namespace Tests.UnitTests.Domain
{
    public class PermissionTests
    {
        [Fact]
        public void Permission_Should_Have_Default_Values()
        {
            // Arrange & Act
            var permission = new Permission();

            // Assert
            Assert.Equal(string.Empty, permission.Code);
            Assert.Null(permission.ParentCode);
            Assert.Equal(string.Empty, permission.Description);
            Assert.Null(permission.IdUserCreatedAt);
        }

        [Fact]
        public void Permission_Should_Allow_Setting_Properties()
        {
            // Arrange
            var permission = new Permission();

            // Act
            permission.Code = "PERM001";
            permission.ParentCode = "ROOT";
            permission.Description = "Test Permission";
            permission.IdUserCreatedAt = 1;

            // Assert
            Assert.Equal("PERM001", permission.Code);
            Assert.Equal("ROOT", permission.ParentCode);
            Assert.Equal("Test Permission", permission.Description);
            Assert.Equal(1, permission.IdUserCreatedAt);
        }

        [Fact]
        public void Permission_Should_Initialize_Collections()
        {
            // Arrange & Act
            var permission = new Permission();

            // Assert
            Assert.NotNull(permission.RolePermissions);
            Assert.NotNull(permission.UserPermissions);
            Assert.IsType<List<RolePermission>>(permission.RolePermissions);
            Assert.IsType<List<UserPermission>>(permission.UserPermissions);
        }
    }
}