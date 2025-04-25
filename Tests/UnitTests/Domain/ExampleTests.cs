using Domain.Entities;
using System;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class ExampleTests
    {
        [Fact]
        public void Example_DefaultValues_ShouldBeInitializedCorrectly()
        {
            // Arrange & Act
            var example = new Example();

            // Assert
            Assert.Equal(0, example.Id);
            Assert.Equal(string.Empty, example.Name);
            Assert.Equal(string.Empty, example.Code);
            Assert.Null(example.IdUserCreatedAt);
            Assert.True((DateTime.UtcNow - example.CreatedAt).TotalSeconds < 5);
        }

        [Fact]
        public void Example_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var example = new Example();
            var now = DateTime.UtcNow;

            // Act
            example.Id = 1;
            example.Name = "Test Example";
            example.Code = "TST-001";
            example.IdUserCreatedAt = 123;
            example.CreatedAt = now;
            example.IdUserUpdatedAt = 456;
            example.UpdatedAt = now.AddDays(1);
            example.OperationRegister = "TEST";
            example.StatusRegister = false;

            // Assert
            Assert.Equal(1, example.Id);
            Assert.Equal("Test Example", example.Name);
            Assert.Equal("TST-001", example.Code);
            Assert.Equal(123, example.IdUserCreatedAt);
            Assert.Equal(now, example.CreatedAt);
            Assert.Equal(456, example.IdUserUpdatedAt);
            Assert.Equal(now.AddDays(1), example.UpdatedAt);
            Assert.Equal("TEST", example.OperationRegister);
            Assert.False(example.StatusRegister);
        }
    }
}