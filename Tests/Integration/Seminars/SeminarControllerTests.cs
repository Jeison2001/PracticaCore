using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Seminars;
using Domain.Entities;
using Domain.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Infrastructure.Data;

namespace Tests.Integration.Seminars
{
    public class SeminarControllerTests : GenericControllerIntegrationTests<Seminar, SeminarDto>
    {
        protected override string BaseUrl => "/api/Seminar";

        public SeminarControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        // Override GetAll because it is [NonAction]
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var entity = CreateValidEntity();
            SeedAdditionalData(entity);
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<Seminar>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<SeminarWithDetailsDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        // Override GetById because it is [NonAction]
        public override Task GetById_ReturnsOkAndEntity()
        {
            return Task.CompletedTask;
        }

        protected override SeminarDto CreateValidDto()
        {
            return new SeminarDto
            {
                SeminarName = "Test Seminar",
                AttendancePercentage = 90,
                FinalGrade = 4.5m,
                Observations = "Test Observations"
            };
        }

        protected override Seminar CreateValidEntity()
        {
            return new Seminar
            {
                SeminarName = "Test Seminar",
                AttendancePercentage = 90,
                FinalGrade = 4.5m,
                Observations = "Test Observations"
            };
        }

        protected override void SeedAdditionalData(Seminar entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Seed InscriptionModality Chain
                var modality = new Modality { Name = "Test Modality SM " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State SM " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-SM" + Guid.NewGuid().ToString().Substring(0, 4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
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
                var stateStage = new StateStage { Name = "Stage SM " + Guid.NewGuid() };
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
                var modality = new Modality { Name = "Test Modality SM Create " + Guid.NewGuid(), Description = "Test" };
                context.Set<Modality>().Add(modality);
                var stateInscription = new StateInscription { Name = "Test State SM Create " + Guid.NewGuid() };
                context.Set<StateInscription>().Add(stateInscription);
                var academicPeriod = new AcademicPeriod { Code = "2025-SMC" + Guid.NewGuid().ToString().Substring(0, 4), StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6) };
                context.Set<AcademicPeriod>().Add(academicPeriod);
                context.SaveChanges();

                var inscription = new InscriptionModality
                {
                    IdModality = modality.Id,
                    IdStateInscription = stateInscription.Id,
                    IdAcademicPeriod = academicPeriod.Id
                };
                context.Set<InscriptionModality>().Add(inscription);
                
                var stateStage = new StateStage { Name = "Stage SM Create " + Guid.NewGuid() };
                context.Set<StateStage>().Add(stateStage);
                
                context.SaveChanges();
                
                inscriptionId = inscription.Id;
                stateStageId = stateStage.Id;
            }

            var dto = new SeminarDto
            {
                Id = inscriptionId,
                IdStateStage = stateStageId,
                SeminarName = "Test Seminar",
                AttendancePercentage = 90,
                FinalGrade = 4.5m,
                Observations = "Test Observations"
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeminarDto>>();
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
                context.Set<Seminar>().Add(entity);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{entity.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeminarWithDetailsDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Seminar.Should().NotBeNull();
            result.Data.Seminar!.Id.Should().Be(entity.Id);
        }
    }
}
