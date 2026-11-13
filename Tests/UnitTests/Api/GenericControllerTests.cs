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
        public async Task GetAll_ForwardsKeysetAndSkipTotalCountParameters()
        {
            // Arrange
            var cursorAt = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
            var request = new PaginatedRequest
            {
                PageNumber = 1,
                PageSize = 25,
                SortBy = "Code",
                IsDescending = true,
                CursorCreatedAt = cursorAt,
                CursorId = 42,
                SkipTotalCount = true
            };

            GetAllEntitiesQuery<BaseEntity<int>, int, BaseDto<int>>? capturedQuery = null;
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllEntitiesQuery<BaseEntity<int>, int, BaseDto<int>>>(), default))
                .Callback<IRequest<PaginatedResult<BaseDto<int>>>, CancellationToken>((q, _) =>
                    capturedQuery = (GetAllEntitiesQuery<BaseEntity<int>, int, BaseDto<int>>)q)
                .ReturnsAsync(new PaginatedResult<BaseDto<int>>());

            // Act
            await _controller.GetAll(request);

            // Assert
            Assert.NotNull(capturedQuery);
            Assert.Equal(cursorAt, capturedQuery!.CursorCreatedAt);
            Assert.Equal(42L, capturedQuery.CursorId);
            Assert.True(capturedQuery.SkipTotalCount);
            Assert.Equal(25, capturedQuery.PageSize);
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
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, objectResult.StatusCode);
            var apiResponse = Assert.IsType<ApiResponse<BaseDto<int>>>(objectResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
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
        public async Task UpdateStatus_ReturnsOkResult_WhenEntityStatusIsUpdated()
        {
            // Arrange
            var id = 1;
            var dto = new UpdateStatusRequestDto { StatusRegister = false, IdUserUpdateAt = 1, OperationRegister = "Test Operation" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateStatusEntityCommand<BaseEntity<int>, int>>(), default))
                         .ReturnsAsync(true);

            // Act
           
            var result = await _controller.UpdateStatus(id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Contains("Entity status updated successfully", apiResponse.Messages!);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNotFoundResult_WhenEntityStatusUpdateFails()
        {
            // Arrange
            var id = 1;
            var dto = new UpdateStatusRequestDto { StatusRegister = false, IdUserUpdateAt = 1, OperationRegister = "Test Operation" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateStatusEntityCommand<BaseEntity<int>, int>>(), default))
                         .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateStatus(id,dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Entity not found or update failed", apiResponse.Errors!);
        }
    }
}
