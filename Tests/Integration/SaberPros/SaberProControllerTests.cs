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
        public override Task GetById_ReturnsOkAndEntity()
        {
            return Task.CompletedTask;
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
            // Arrange
            int inscriptionId;
            int stateStageId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed dependencies
                var modality = new Modality { Name = "Test Modality SP Create " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State SP Create " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-SPC" + Guid.NewGuid().ToString().Substring(0, 4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                
                var stateStage = new StateStage { Name = "Stage SP Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                
                context.SaveChanges();
                
                inscriptionId = inscription.Id;
                stateStageId = stateStage.Id;
            }

            var dto = new SaberProDto
            {
                Id = inscriptionId,
                IdStateStage = stateStageId,
                ResultScore = 250,
                ExamDate = DateTime.Now,
                ResultQuintile = "Q1",
                Observations = "Test Observations"
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SaberProDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(inscriptionId);
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
