using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class UserTests
    {
        [Fact]
        public void User_Should_Have_Default_Values()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            Assert.Equal(0, user.IdIdentificationType);
            Assert.Null(user.Identification);
            Assert.Null(user.Email);
            Assert.Null(user.FirstName);
            Assert.Null(user.LastName);
            Assert.Equal(0, user.IdAcademicProgram);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.CurrentAcademicPeriod);
            Assert.Null(user.CumulativeAverage);
            Assert.Null(user.ApprovedCredits);
            Assert.Null(user.TotalAcademicCredits);
            Assert.Null(user.Observation);
        }

        [Fact]
        public void User_Should_Allow_Setting_Properties()
        {
            // Arrange
            var user = new User();

            // Act
            user.IdIdentificationType = 1;
            user.Identification = "123456789";
            user.Email = "user@example.com";
            user.FirstName = "John";
            user.LastName = "Doe";
            user.IdAcademicProgram = 2;
            user.PhoneNumber = "555-1234";
            user.CurrentAcademicPeriod = "2025-1";
            user.CumulativeAverage = 4.5;
            user.ApprovedCredits = 30;
            user.TotalAcademicCredits = 120;
            user.Observation = "Excellent student";

            // Assert
            Assert.Equal(1, user.IdIdentificationType);
            Assert.Equal("123456789", user.Identification);
            Assert.Equal("user@example.com", user.Email);
            Assert.Equal("John", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.Equal(2, user.IdAcademicProgram);
            Assert.Equal("555-1234", user.PhoneNumber);
            Assert.Equal("2025-1", user.CurrentAcademicPeriod);
            Assert.Equal(4.5, user.CumulativeAverage);
            Assert.Equal(30, user.ApprovedCredits);
            Assert.Equal(120, user.TotalAcademicCredits);
            Assert.Equal("Excellent student", user.Observation);
        }
    }
}