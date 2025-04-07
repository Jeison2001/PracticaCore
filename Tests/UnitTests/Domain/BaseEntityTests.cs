using Domain.Entities;
using Moq;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class BaseEntityTests
    {
        [Fact]
        public void BaseEntity_DefaultValues_ShouldBeInitializedCorrectly()
        {
            // Arrange
            var mockEntity = new Mock<BaseEntity<int>>();
            var entity = mockEntity.Object;

            // Assert
            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.IdUserCreatedAt);
            Assert.True((DateTime.UtcNow - entity.CreatedAt).TotalSeconds < 5);
            Assert.Null(entity.IdUserUpdatedAt);
            Assert.Null(entity.UpdatedAt);
            Assert.Equal(string.Empty, entity.OperationRegister);
            Assert.True(entity.StatusRegister);
        }

        [Fact]
        public void BaseEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var mockEntity = new Mock<BaseEntity<Guid>>();
            var entity = mockEntity.Object;
            var guid = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Act
            entity.Id = guid;
            entity.IdUserCreatedAt = 123;
            entity.CreatedAt = now;
            entity.IdUserUpdatedAt = 456;
            entity.UpdatedAt = now.AddDays(1);
            entity.OperationRegister = "TEST";
            entity.StatusRegister = false;

            // Assert
            Assert.Equal(guid, entity.Id);
            Assert.Equal(123, entity.IdUserCreatedAt);
            Assert.Equal(now, entity.CreatedAt);
            Assert.Equal(456, entity.IdUserUpdatedAt);
            Assert.Equal(now.AddDays(1), entity.UpdatedAt);
            Assert.Equal("TEST", entity.OperationRegister);
            Assert.False(entity.StatusRegister);
        }
    }
}
