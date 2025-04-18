using Application.Shared.DTOs.AcademicProgram;
using Application.Validations.SpecificValidators.AcademicProgram;
using Xunit;

namespace Tests.UnitTests.Application.Validations
{
    public class AcademicProgramValidatorTests
    {
        private readonly AcademicProgramValidator _validator;

        public AcademicProgramValidatorTests()
        {
            _validator = new AcademicProgramValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            // Arrange
            var model = new AcademicProgramDto { Name = "", Code = "PROG001", IdFaculty = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_MaxLength()
        {
            // Arrange
            var longName = new string('A', 101); // 101 characters, exceeding the 100 limit
            var model = new AcademicProgramDto { Name = longName, Code = "PROG001", IdFaculty = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public void Should_Have_Error_When_Code_Is_Empty()
        {
            // Arrange
            var model = new AcademicProgramDto { Name = "Computer Science", Code = "", IdFaculty = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Code");
        }

        [Fact]
        public void Should_Have_Error_When_Code_Exceeds_MaxLength()
        {
            // Arrange
            var longCode = new string('A', 21); // 21 characters, exceeding the 20 limit
            var model = new AcademicProgramDto { Name = "Computer Science", Code = longCode, IdFaculty = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Code");
        }

        [Fact]
        public void Should_Have_Error_When_IdFaculty_Is_Zero()
        {
            // Arrange
            var model = new AcademicProgramDto { Name = "Computer Science", Code = "PROG001", IdFaculty = 0 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "IdFaculty");
        }

        [Fact]
        public void Should_Not_Have_Error_When_All_Properties_Are_Valid()
        {
            // Arrange
            var model = new AcademicProgramDto { Name = "Computer Science", Code = "PROG001", IdFaculty = 1 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}