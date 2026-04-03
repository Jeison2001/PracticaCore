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
    /// Clase base para tests de integración de controladores que heredan de GenericController.
    /// Todos los métodos de test son virtuales, permitiendo que las clases derivadas hagan override
    /// cuando el controlador deshabilita endpoints específicos con [NonAction].
    ///
    /// Ejemplo: Si un controlador tiene [NonAction] en GetById, hacer override del test para omitirlo:
    /// <code>
    /// public override Task GetById_ReturnsOkAndEntity()
    /// {
    ///     return Task.CompletedTask; // Omitir este test
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

        /// <summary>
        /// Hacer override en clases derivadas si el controlador deshabilita PUT via [NonAction].
        /// </summary>
        protected virtual bool SupportsUpdate => true;

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
            // Omitir si el controlador ha deshabilitado PUT via [NonAction]
            if (!SupportsUpdate)
            {
                return;
            }

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

        [Fact]
        public virtual async Task UpdateStatus_ReturnsOk()
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

            var updateStatusDto = new UpdateStatusRequestDto 
            { 
                StatusRegister = false, 
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{entity.Id}/status", updateStatusDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }
    }
}
