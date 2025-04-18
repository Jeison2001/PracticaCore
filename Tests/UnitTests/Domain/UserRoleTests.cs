using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class UserRoleTests
    {
        [Fact]
        public void UserRole_Should_Have_Default_Values()
        {
            // Arrange & Act
            var userRole = new UserRole();

            // Assert
            Assert.Equal(0, userRole.IdUser);
            Assert.Equal(0, userRole.IdRole);
        }

        [Fact]
        public void UserRole_Should_Allow_Setting_Properties()
        {
            // Arrange
            var userRole = new UserRole();

            // Act
            userRole.IdUser = 1;
            userRole.IdRole = 2;

            // Assert
            Assert.Equal(1, userRole.IdUser);
            Assert.Equal(2, userRole.IdRole);
        }
    }
}