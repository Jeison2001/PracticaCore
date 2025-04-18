using Application.Shared.DTOs;
using Api.Controllers;
using Api.Responses;
using Application.Shared.Commands;
using Application.Shared.DTOs.UserPermission;
using Application.Shared.Queries;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.UnitTests.Api.Controllers
{
    public class UserPermissionControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly UserPermissionController _controller;

        public UserPermissionControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new UserPermissionController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithUserPermission()
        {
            // Arrange
            var id = 1L;
            var dto = new UserPermissionDto { Id = id, IdUser = 1, IdPermission = 2 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetEntityByIdQuery<UserPermission, long, UserPermissionDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<UserPermissionDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithPaginatedUserPermissions()
        {
            // Arrange
            var dtos = new List<UserPermissionDto> { 
                new() { Id = 1, IdUser = 1, IdPermission = 2 }, 
                new() { Id = 2, IdUser = 1, IdPermission = 3 } 
            };
            var paginatedResult = new PaginatedResult<UserPermissionDto>
            {
                Items = dtos,
                TotalRecords = dtos.Count,
                PageNumber = 1,
                PageSize = 10
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllEntitiesQuery<UserPermission, long, UserPermissionDto>>(), default))
                         .ReturnsAsync(paginatedResult);

            // Act
            var result = await _controller.GetAll(new PaginatedRequest());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<PaginatedResult<UserPermissionDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(paginatedResult, apiResponse.Data);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithUserPermission()
        {
            // Arrange
            var dto = new UserPermissionDto { Id = 1, IdUser = 1, IdPermission = 2 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateEntityCommand<UserPermission, long, UserPermissionDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<UserPermissionDto>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(dto.Id, createdAtActionResult.RouteValues!["id"]);
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WithUserPermission()
        {
            // Arrange
            var id = 1L;
            var dto = new UserPermissionDto { Id = id, IdUser = 1, IdPermission = 2 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateEntityCommand<UserPermission, long, UserPermissionDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<UserPermissionDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenUserPermissionIsDeleted()
        {
            // Arrange
            var id = 1L;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<UserPermission, long>>(), default))
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
        public async Task Delete_ReturnsNotFoundResult_WhenUserPermissionIsNotDeleted()
        {
            // Arrange
            var id = 1L;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<UserPermission, long>>(), default))
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