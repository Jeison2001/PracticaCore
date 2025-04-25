using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class RoleTests
    {
        [Fact]
        public void Role_Should_Have_Default_Values()
        {
            // Arrange & Act
            var role = new Role();

            // Assert
            Assert.Null(role.Code);
            Assert.Null(role.Name);
            Assert.Null(role.Description);
        }

        [Fact]
        public void Role_Should_Allow_Setting_Properties()
        {
            // Arrange
            var role = new Role();

            // Act
            role.Code = "ADMIN";
            role.Name = "Administrator";
            role.Description = "Full access to the system.";

            // Assert
            Assert.Equal("ADMIN", role.Code);
            Assert.Equal("Administrator", role.Name);
            Assert.Equal("Full access to the system.", role.Description);
        }
    }
}