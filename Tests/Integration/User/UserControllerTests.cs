using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.User;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Users
{
    public class UserControllerTests : GenericControllerIntegrationTests<Domain.Entities.User, UserDto>
    {
        public UserControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/User";

        private async Task<(IdentificationType IdType, AcademicProgram Program)> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula" };
            if (!context.Set<IdentificationType>().Any(x => x.Code == "CC"))
                context.Set<IdentificationType>().Add(idType);
            else
                idType = context.Set<IdentificationType>().First(x => x.Code == "CC");

            var program = new AcademicProgram { Name = "Systems Engineering", Code = "SYS" };
            if (!context.Set<AcademicProgram>().Any(x => x.Code == "SYS"))
                context.Set<AcademicProgram>().Add(program);
            else
                program = context.Set<AcademicProgram>().First(x => x.Code == "SYS");

            await context.SaveChangesAsync();

            return (idType, program);
        }

        protected override UserDto CreateValidDto()
        {
            return new UserDto(); // Placeholder
        }

        protected override Domain.Entities.User CreateValidEntity()
        {
            return new Domain.Entities.User(); // Placeholder
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();

            var dto = new UserDto
            {
                FirstName = "New",
                LastName = "User",
                Email = "new.user@test.com",
                Identification = "1234567890",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
            result.Data!.Email.Should().Be("new.user@test.com");
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = new Domain.Entities.User
                {
                    FirstName = "List",
                    LastName = "User",
                    Email = "list.user@test.com",
                    Identification = "111222333",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<UserDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            int userId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = new Domain.Entities.User
                {
                    FirstName = "Detail",
                    LastName = "User",
                    Email = "detail.user@test.com",
                    Identification = "444555666",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Id.Should().Be(userId);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            int userId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = new Domain.Entities.User
                {
                    FirstName = "Update",
                    LastName = "User",
                    Email = "update.user@test.com",
                    Identification = "777888999",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;
            }

            var updateDto = new UserDto
            {
                Id = userId,
                FirstName = "Updated",
                LastName = "User",
                Email = "update.user@test.com",
                Identification = "777888999",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{userId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.FirstName.Should().Be("Updated");
        }

        [Fact]
        public override async Task UpdateStatus_ReturnsOk()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            int userId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = new Domain.Entities.User
                {
                    FirstName = "Status",
                    LastName = "User",
                    Email = "status.user@test.com",
                    Identification = "999000111",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<Domain.Entities.User>().Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;
            }

            var updateStatusDto = new UpdateStatusRequestDto 
            { 
                StatusRegister = false, 
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{userId}/status", updateStatusDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }
    }
}
