using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class FacultyTests
    {
        [Fact]
        public void Faculty_Should_Have_Default_Values()
        {
            // Arrange & Act
            var faculty = new Faculty();

            // Assert
            Assert.Null(faculty.Name);
            Assert.Empty(faculty.AcademicPrograms);
        }

        [Fact]
        public void Faculty_Should_Allow_Setting_Properties()
        {
            // Arrange
            var faculty = new Faculty();

            // Act
            faculty.Name = "Engineering";

            // Assert
            Assert.Equal("Engineering", faculty.Name);
        }
    }
}