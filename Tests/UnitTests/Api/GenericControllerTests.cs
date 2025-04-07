using Api.Controllers;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.UnitTests.Api
{
    public class GenericControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly GenericController<BaseEntity<int>, int, BaseDto<int>> _controller;

        public GenericControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new GenericController<BaseEntity<int>, int, BaseDto<int>>(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithEntity()
        {
            // Arrange
            var id = 1;
            var dto = new BaseDto<int> { Id = id };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetEntityByIdQuery<BaseEntity<int>, int, BaseDto<int>>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithEntities()
        {
            // Arrange
            var dtos = new List<BaseDto<int>> { new() { Id = 1 }, new() { Id = 2 } };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllEntitiesQuery<BaseEntity<int>, int, BaseDto<int>>>(), default))
                         .ReturnsAsync(dtos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dtos, okResult.Value);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithEntity()
        {
            // Arrange
            var dto = new BaseDto<int> { Id = 1 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateEntityCommand<BaseEntity<int>, int, BaseDto<int>>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(dto, createdAtActionResult.Value);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(dto.Id, createdAtActionResult.RouteValues!["id"]);
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WithEntity()
        {
            // Arrange
            var id = 1;
            var dto = new BaseDto<int> { Id = id };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateEntityCommand<BaseEntity<int>, int, BaseDto<int>>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContentResult_WhenEntityIsDeleted()
        {
            // Arrange
            var id = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<BaseEntity<int>, int>>(), default))
                         .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFoundResult_WhenEntityIsNotDeleted()
        {
            // Arrange
            var id = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<BaseEntity<int>, int>>(), default))
                         .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
