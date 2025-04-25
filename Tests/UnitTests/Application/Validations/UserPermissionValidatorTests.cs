using Application.Shared.DTOs.UserPermission;
using Application.Validations.SpecificValidators.UserPermission;
using Xunit;

namespace Tests.UnitTests.Application.Validations
{
    public class UserPermissionValidatorTests
    {
        private readonly UserPermissionValidator _validator;

        public UserPermissionValidatorTests()
        {
            _validator = new UserPermissionValidator();
        }

        [Fact]
        public void Should_Have_Error_When_IdUser_Is_Empty()
        {
            // Arrange
            var model = new UserPermissionDto { IdUser = 0, IdPermission = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "IdUser");
        }

        [Fact]
        public void Should_Have_Error_When_IdPermission_Is_Empty()
        {
            // Arrange
            var model = new UserPermissionDto { IdUser = 1, IdPermission = 0 };

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
            var model = new UserPermissionDto { IdUser = 1, IdPermission = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}