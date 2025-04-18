using Application.Shared.DTOs.RolePermission;
using Application.Validations.SpecificValidators.RolePermission;
using Xunit;

namespace Tests.UnitTests.Application.Validations
{
    public class RolePermissionValidatorTests
    {
        private readonly RolePermissionValidator _validator;

        public RolePermissionValidatorTests()
        {
            _validator = new RolePermissionValidator();
        }

        [Fact]
        public void Should_Have_Error_When_IdRole_Is_Empty()
        {
            // Arrange
            var model = new RolePermissionDto { IdRole = 0, IdPermission = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "IdRole");
        }

        [Fact]
        public void Should_Have_Error_When_IdPermission_Is_Empty()
        {
            // Arrange
            var model = new RolePermissionDto { IdRole = 1, IdPermission = 0 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "IdPermission");
        }

        [Fact]
        public void Should_Not_Have_Error_When_All_Properties_Are_Valid()
        {
            // Arrange
            var model = new RolePermissionDto { IdRole = 1, IdPermission = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}