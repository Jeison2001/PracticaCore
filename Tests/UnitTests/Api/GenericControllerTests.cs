using Api.Controllers;
using Api.Responses;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.Queries;
using Domain.Common;
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
            var apiResponse = Assert.IsType<ApiResponse<BaseDto<int>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithPaginatedEntities()
        {
            // Arrange
            var dtos = new List<BaseDto<int>> { new() { Id = 1 }, new() { Id = 2 } };
            var paginatedResult = new PaginatedResult<BaseDto<int>>
            {
                Items = dtos,
                TotalRecords = dtos.Count,
                PageNumber = 1,
                PageSize = 10
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllEntitiesQuery<BaseEntity<int>, int, BaseDto<int>>>(), default))
                         .ReturnsAsync(paginatedResult);

            // Act
            var result = await _controller.GetAll(new PaginatedRequest());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<PaginatedResult<BaseDto<int>>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(paginatedResult, apiResponse.Data);
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
            var apiResponse = Assert.IsType<ApiResponse<BaseDto<int>>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
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
            var apiResponse = Assert.IsType<ApiResponse<BaseDto<int>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenEntityIsDeleted()
        {
            // Arrange
            var id = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<BaseEntity<int>, int>>(), default))
                         .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Contains("Entity deleted successfully", apiResponse.Messages!);
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
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Entity not found", apiResponse.Errors!);
        }
    }
}
