using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;


namespace Tests.Integration
{
    /// <summary>
    /// Base class for integration tests of controllers inheriting from GenericController.
    /// All test methods are virtual, allowing derived classes to override them when
    /// the controller disables specific endpoints with [NonAction].
    /// 
    /// Example: If a controller has [NonAction] on GetById, override the test to skip it:
    /// <code>
    /// public override Task GetById_ReturnsOkAndEntity()
    /// {
    ///     return Task.CompletedTask; // Skip this test
    /// }
    /// </code>
    /// </summary>
    public abstract class GenericControllerIntegrationTests<TEntity, TDto> : IntegrationTestBase
        where TEntity : BaseEntity<int>
        where TDto : BaseDto<int>
    {
        protected abstract string BaseUrl { get; }

        protected GenericControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected abstract TDto CreateValidDto();
        protected abstract TEntity CreateValidEntity();
        
        protected virtual void SeedAdditionalData(TEntity entity) { }

        [Fact]
        public virtual async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<TEntity>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<TDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public virtual async Task GetById_ReturnsOkAndEntity()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<TEntity>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
        }

        [Fact]
        public virtual async Task Create_ReturnsCreated()
        {
            // Arrange
            var dto = CreateValidDto();

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public virtual async Task Update_ReturnsOk()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<TEntity>().Add(entity);
                await context.SaveChangesAsync();
            }

            var updateDto = CreateValidDto();
            updateDto.Id = entity.Id;

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{entity.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(entity.Id);
        }
    }
}
