using Application.Shared.Commands;
using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Moq;
using Xunit;

namespace Tests.UnitTests.Application
{
    public class CreateEntityCommandHandlerTests
    {
        private readonly Mock<IRepository<TestEntity, int>> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<CreateEntityCommandHandler<TestEntity, int, TestDto>>> _mockLogger;
        private readonly CreateEntityCommandHandler<TestEntity, int, TestDto> _handler;

        public CreateEntityCommandHandlerTests()
        {
            _mockRepository = new Mock<IRepository<TestEntity, int>>();
            _mockMapper = new Mock<IMapper>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CreateEntityCommandHandler<TestEntity, int, TestDto>>>();

            _handler = new CreateEntityCommandHandler<TestEntity, int, TestDto>(
                _mockRepository.Object,
                _mockMapper.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                null);
        }

        [Fact]
        public async Task Handle_ShouldAddEntityToRepository()
        {
            // Arrange
            var dto = new TestDto { Id = 0, Name = "Test" };
            var entity = new TestEntity { Id = 0, Name = "Test" };
            var resultEntity = new TestEntity { Id = 1, Name = "Test" };
            var resultDto = new TestDto { Id = 1, Name = "Test" };
            var command = new CreateEntityCommand<TestEntity, int, TestDto>(dto);

            _mockMapper.Setup(m => m.Map<TestEntity>(dto)).Returns(entity);
            _mockMapper.Setup(m => m.Map<TestDto>(resultEntity)).Returns(resultDto);
            _mockRepository.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            // Y luego usar la misma entidad para el mapeo de regreso
            _mockMapper.Setup(m => m.Map<TestDto>(entity)).Returns(resultDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockRepository.Verify(r => r.AddAsync(entity), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(resultDto, result);
        }

        [Fact]
        public async Task Handle_ShouldCommitChanges()
        {
            // Arrange
            var dto = new TestDto { Id = 0, Name = "Test" };
            var entity = new TestEntity { Id = 0, Name = "Test" };
            var command = new CreateEntityCommand<TestEntity, int, TestDto>(dto);

            _mockMapper.Setup(m => m.Map<TestEntity>(dto)).Returns(entity);
            _mockMapper.Setup(m => m.Map<TestDto>(entity)).Returns(dto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnMappedDto()
        {
            // Arrange
            var dto = new TestDto { Id = 0, Name = "Test" };
            var entity = new TestEntity { Id = 0, Name = "Test" };
            var resultDto = new TestDto { Id = 1, Name = "Test" };
            var command = new CreateEntityCommand<TestEntity, int, TestDto>(dto);

            _mockMapper.Setup(m => m.Map<TestEntity>(dto)).Returns(entity);
            _mockRepository.Setup(r => r.AddAsync(entity)).Callback(() => {entity.Id = 1;}).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<TestDto>(entity)).Returns(resultDto);
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(resultDto, result);
        }

    }

    // Clases auxiliares para las pruebas
    public class TestEntity : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestDto : BaseDto<int>
    {
        public string Name { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            if (obj is TestDto other)
            {
                return Id == other.Id && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}
