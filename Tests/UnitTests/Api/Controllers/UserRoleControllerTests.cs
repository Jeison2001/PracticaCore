using Api.Controllers;
using Api.Responses;
using Application.Shared.DTOs.UserRole;
using Application.Shared.Queries.UserRole;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.UnitTests.Api.Controllers
{
    public class UserRoleControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly UserRoleController _controller;

        public UserRoleControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new UserRoleController(_mediatorMock.Object);
        }        [Fact]
        public async Task GetUserRolesByUserId_ReturnsOkResult_WithUserRolesList()
        {
            // Arrange
            var userId = 1;
            var userRoles = new List<UserRoleInfoDto>
            {
                new UserRoleInfoDto
                {
                    // Propiedades del RoleDto (heredadas)
                    Id = 1,
                    Code = "ADMIN",
                    Name = "Administrator",
                    Description = "Full system access",
                    
                    // Propiedades específicas de UserRoleInfoDto
                    UserRoleId = 1, // ID del registro UserRole
                    UserId = userId,
                    StatusRegister = true,
                    OperationRegister = "CREATE",
                    IdUserCreatedAt = 1
                },
                new UserRoleInfoDto
                {
                    // Propiedades del RoleDto (heredadas)
                    Id = 2,
                    Code = "USER",
                    Name = "Regular User",
                    Description = "Basic system access",
                    
                    // Propiedades específicas de UserRoleInfoDto
                    UserRoleId = 2, // ID del registro UserRole
                    UserId = userId,
                    StatusRegister = true,
                    OperationRegister = "CREATE",
                    IdUserCreatedAt = 1
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserRolesByUserIdQuery>(), default))
                         .ReturnsAsync(userRoles);

            // Act
            var result = await _controller.GetUserRolesByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<UserRoleInfoDto>>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(2, apiResponse.Data.Count);

            // Verificar que se devuelve el UserRoleId (necesario para desactivación)
            Assert.Equal(1, apiResponse.Data.First().UserRoleId);
            Assert.Equal(2, apiResponse.Data.Last().UserRoleId);

            // Verificar que se devuelve la información completa del rol
            Assert.Equal("ADMIN", apiResponse.Data.First().Code);
            Assert.Equal("Administrator", apiResponse.Data.First().Name);
            Assert.Equal("USER", apiResponse.Data.Last().Code);
            Assert.Equal("Regular User", apiResponse.Data.Last().Name);
        }

        [Fact]
        public async Task GetUserRolesByUserId_ReturnsBadRequest_WhenUserIdIsInvalid()
        {
            // Arrange
            var invalidUserId = 0;

            // Act
            var result = await _controller.GetUserRolesByUserId(invalidUserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("El ID de usuario debe ser válido.", badRequestResult.Value);
        }        [Fact]
        public async Task GetUserRolesByUserId_ReturnsNotFound_WhenNoUserRolesExist()
        {
            // Arrange
            var userId = 1;
            var emptyList = new List<UserRoleInfoDto>();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserRolesByUserIdQuery>(), default))
                         .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetUserRolesByUserId(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No se encontraron registros de UserRole para el usuario con ID {userId}.", notFoundResult.Value);
        }        [Fact]
        public async Task GetUserRolesByUserId_CallsMediatorWithCorrectQuery()
        {
            // Arrange
            var userId = 123;
            var userRoles = new List<UserRoleInfoDto>
            {
                new UserRoleInfoDto
                {
                    Id = 1,
                    Code = "TEST",
                    Name = "Test Role",
                    UserRoleId = 1,
                    UserId = userId
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserRolesByUserIdQuery>(), default))
                         .ReturnsAsync(userRoles);

            // Act
            await _controller.GetUserRolesByUserId(userId);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetUserRolesByUserIdQuery>(q => q.UserId == userId), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
