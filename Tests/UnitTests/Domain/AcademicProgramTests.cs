using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class AcademicProgramTests
    {
        [Fact]
        public void AcademicProgram_Should_Have_Default_Values()
        {
            // Arrange & Act
            var program = new AcademicProgram();

            // Assert
            Assert.Null(program.Name);
            Assert.Null(program.Code);
            Assert.Equal(0, program.IdFaculty);
        }

        [Fact]
        public void AcademicProgram_Should_Allow_Setting_Properties()
        {
            // Arrange
            var program = new AcademicProgram();

            // Act
            program.Name = "Computer Science";
            program.Code = "CS101";
            program.IdFaculty = 1;

            // Assert
            Assert.Equal("Computer Science", program.Name);
            Assert.Equal("CS101", program.Code);
            Assert.Equal(1, program.IdFaculty);
        }
    }
}