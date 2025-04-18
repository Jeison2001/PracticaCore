using Api.Controllers;
using Api.Responses;
using Application.Shared.Commands;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Role;
using Application.Shared.Queries;
using Domain.Common;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.UnitTests.Api.Controllers
{
    public class RoleControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new RoleController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithRole()
        {
            // Arrange
            var id = 1;
            var dto = new RoleDto { Id = id, Name = "Admin" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetEntityByIdQuery<Role, int, RoleDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<RoleDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithPaginatedRoles()
        {
            // Arrange
            var dtos = new List<RoleDto> { 
                new() { Id = 1, Name = "Admin" }, 
                new() { Id = 2, Name = "User" } 
            };
            var paginatedResult = new PaginatedResult<RoleDto>
            {
                Items = dtos,
                TotalRecords = dtos.Count,
                PageNumber = 1,
                PageSize = 10
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllEntitiesQuery<Role, int, RoleDto>>(), default))
                         .ReturnsAsync(paginatedResult);

            // Act
            var result = await _controller.GetAll(new PaginatedRequest());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<PaginatedResult<RoleDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(paginatedResult, apiResponse.Data);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithRole()
        {
            // Arrange
            var dto = new RoleDto { Id = 1, Name = "Admin" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateEntityCommand<Role, int, RoleDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<RoleDto>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(dto.Id, createdAtActionResult.RouteValues!["id"]);
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WithRole()
        {
            // Arrange
            var id = 1;
            var dto = new RoleDto { Id = id, Name = "Admin" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateEntityCommand<Role, int, RoleDto>>(), default))
                         .ReturnsAsync(dto);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<RoleDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(dto, apiResponse.Data);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenRoleIsDeleted()
        {
            // Arrange
            var id = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<Role, int>>(), default))
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
        public async Task Delete_ReturnsNotFoundResult_WhenRoleIsNotDeleted()
        {
            // Arrange
            var id = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteEntityCommand<Role, int>>(), default))
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