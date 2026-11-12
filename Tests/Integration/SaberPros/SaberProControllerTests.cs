using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.SaberPros;
using Domain.Entities;
using Domain.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Infrastructure.Data;

namespace Tests.Integration.SaberPros
{
    public class SaberProControllerTests : GenericControllerIntegrationTests<SaberPro, SaberProDto>
    {
        protected override string BaseUrl => "/api/SaberPro";

        public SaberProControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        // Override GetById because it is [NonAction]
        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                context.Set<SaberPro>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<SaberProDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }


        protected override SaberProDto CreateValidDto()
        {
            return new SaberProDto
            {
                ResultScore = 250,
                ExamDate = DateTime.Now,
                ResultQuintile = "Q1",
                Observations = "Test Observations"
            };
        }

        protected override SaberPro CreateValidEntity()
        {
            return new SaberPro
            {
                ResultScore = 250,
                ExamDate = DateTime.Now,
                ResultQuintile = "Q1",
                Observations = "Test Observations"
            };
        }

        protected override void SeedAdditionalData(SaberPro entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed InscriptionModality Chain
                var modality = new Modality { Name = "Test Modality SP " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State SP " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-SP" + Guid.NewGuid().ToString().Substring(0, 4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                context.SaveChanges();

                // 2. Seed StateStage
                var stateStage = new StateStage { Name = "Stage SP " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                context.SaveChanges();

                // Set Entity ID and FKs
                entity.Id = inscription.Id; // 1:1 with Inscription
                entity.IdStateStage = stateStage.Id;
            }
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            var response = await _client.PostAsJsonAsync(BaseUrl, CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/1", CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public override async Task UpdateStatus_ReturnsOk()
        {
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/status", new Application.Shared.DTOs.UpdateStatusRequestDto
            {
                StatusRegister = false,
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetWithDetails_ReturnsOkAndDetails()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<SaberPro>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaberProWithDetailsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SaberPro.Should().NotBeNull();
            result.Data.SaberPro!.Id.Should().Be(entity.Id);
        }
    }
}
