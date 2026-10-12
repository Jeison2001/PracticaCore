using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Users;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Users
{
    public class UserControllerTests : GenericControllerIntegrationTests<User, UserDto>
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

        protected override User CreateValidEntity()
        {
            return new User(); // Placeholder
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
                var user = new User
                {
                    FirstName = "List",
                    LastName = "User",
                    Email = "list.user@test.com",
                    Identification = "111222333",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);
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
                var user = new User
                {
                    FirstName = "Detail",
                    LastName = "User",
                    Email = "detail.user@test.com",
                    Identification = "444555666",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);
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
                var user = new User
                {
                    FirstName = "Update",
                    LastName = "User",
                    Email = "update.user@test.com",
                    Identification = "777888999",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);
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
                var user = new User
                {
                    FirstName = "Status",
                    LastName = "User",
                    Email = "status.user@test.com",
                    Identification = "999000111",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);
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

        [Fact]
        public async Task GetAll_ReturnsUsers_WithPopulatedRoles()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            int userId;
            Role role;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                role = new Role { Code = "TEST_ROLE_" + Guid.NewGuid().ToString("N").Substring(0, 5), Name = "Test Role", StatusRegister = true };
                context.Set<Role>().Add(role);
                await context.SaveChangesAsync();

                var user = new User
                {
                    FirstName = "WithRole",
                    LastName = "User",
                    Email = $"role_{Guid.NewGuid():N}@test.com",
                    Identification = Guid.NewGuid().ToString("N").Substring(0, 10),
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(user);
                await context.SaveChangesAsync();
                userId = user.Id;

                var userRole = new UserRole
                {
                    IdUser = userId,
                    IdRole = role.Id,
                    StatusRegister = true
                };
                context.Set<UserRole>().Add(userRole);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=50");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<UserDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            var targetUser = result.Data!.Items.FirstOrDefault(u => u.Id == userId);
            targetUser.Should().NotBeNull();
            targetUser!.Roles.Should().NotBeNull();
            targetUser.Roles.Should().Contain(r => r.Id == role.Id && r.Code == role.Code && r.Name == role.Name);
        }

        [Fact]
        public async Task GetAll_WithKeysetPagination_ReturnsNextPageAndHasMoreRows()
        {
            // Arrange
            var (idType, program) = await SeedDependenciesAsync();
            var now = DateTimeOffset.UtcNow;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                for (int i = 1; i <= 5; i++)
                {
                    context.Set<User>().Add(new User
                    {
                        FirstName = $"KeysetUser_{i}",
                        LastName = "Test",
                        Email = $"keyset_{i}_{Guid.NewGuid():N}@test.com",
                        Identification = Guid.NewGuid().ToString("N").Substring(0, 10),
                        IdIdentificationType = idType.Id,
                        IdAcademicProgram = program.Id,
                        CreatedAt = now.AddMinutes(i),
                        StatusRegister = true
                    });
                }
                await context.SaveChangesAsync();
            }

            // Act 1: Get first page of size 2
            var response1 = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=2");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<UserDto>>>();
            result1.Should().NotBeNull();
            result1!.Data.Should().NotBeNull();
            var page1Items = result1.Data!.Items.ToList();
            page1Items.Should().HaveCount(2);
            result1.Data.CursorCreatedAt.Should().NotBeNull();
            result1.Data.CursorId.Should().NotBeNull();

            // Act 2: Request second page using keyset with SkipTotalCount=true
            var cursorCreatedAt = result1.Data.CursorCreatedAt!.Value.ToString("O");
            var cursorId = result1.Data.CursorId!.Value;
            var response2 = await _client.GetAsync(
                $"{BaseUrl}?PageNumber=2&PageSize=2&CursorId={cursorId}&CursorCreatedAt={Uri.EscapeDataString(cursorCreatedAt)}&SkipTotalCount=true");

            // Assert 2
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<UserDto>>>();
            result2.Should().NotBeNull();
            result2!.Data.Should().NotBeNull();
            result2.Data!.TotalRecords.Should().Be(-1); // COUNT skipped
            result2.Data.HasMoreRows.Should().BeTrue(); // There are more than 2 items left
            var page2Items = result2.Data.Items.ToList();
            page2Items.Should().HaveCount(2);

            // Ensure no overlap between page 1 and page 2
            page1Items.Select(x => x.Id).Should().NotIntersectWith(page2Items.Select(x => x.Id));

            // Verify order is descending
            page2Items[0].CreatedAt.Should().BeOnOrBefore(page1Items[1].CreatedAt);
        }
    }
}
