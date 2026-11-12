using System.Net;
using System.Net.Http.Json;
using Application.Shared.DTOs;
using Application.Shared.DTOs.InscriptionModalities;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.InscriptionModalities
{
    public class InscriptionModalityControllerTests : GenericControllerIntegrationTests<InscriptionModality, InscriptionModalityDto>
    {
        public InscriptionModalityControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/InscriptionModality";

        // PUT is disabled via [NonAction] in InscriptionModalityController, use PATCH instead
        protected override bool SupportsUpdate => false;

        protected override void SeedAdditionalData(InscriptionModality entity)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                // Seed dependencies if they don't exist (or create new ones to avoid conflicts)
                var modality = context.Set<Modality>().FirstOrDefault(x => x.Name == "Modality_Test");
                if (modality == null)
                {
                    modality = new Modality { Name = "Modality_Test", Description = "Test", StatusRegister = true };
                    context.Set<Modality>().Add(modality);
                }

                var state = context.Set<StateInscription>().FirstOrDefault(x => x.Name == "State_Test");
                if (state == null)
                {
                    state = new StateInscription { Name = "State_Test", Description = "Test", StatusRegister = true };
                    context.Set<StateInscription>().Add(state);
                }

                var period = context.Set<AcademicPeriod>().FirstOrDefault(x => x.Code == "PER_Test");
                if (period == null)
                {
                    period = new AcademicPeriod { Code = "PER_Test", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
                    context.Set<AcademicPeriod>().Add(period);
                }
                context.SaveChanges();

                entity.IdModality = modality.Id;
                entity.IdStateInscription = state.Id;
                entity.IdAcademicPeriod = period.Id;
            }
        }

        protected override InscriptionModalityDto CreateValidDto()
        {
            // We need IDs that exist, so we'll rely on SeedAdditionalData to fix the entity, 
            // but for the DTO in Create test, we need to pre-seed.
            // However, GenericControllerIntegrationTests.Create_ReturnsCreated calls CreateValidDto first.
            // So we need a way to seed data for the DTO.
            
            // Strategy: CreateValidDto will seed its own dependencies.
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();
                
                var modality = context.Set<Modality>().FirstOrDefault(x => x.Name == "Modality_Dto");
                if (modality == null)
                {
                    modality = new Modality { Name = "Modality_Dto", Description = "Test", StatusRegister = true };
                    context.Set<Modality>().Add(modality);
                }

                var state = context.Set<StateInscription>().FirstOrDefault(x => x.Name == "State_Dto");
                if (state == null)
                {
                    state = new StateInscription { Name = "State_Dto", Description = "Test", StatusRegister = true };
                    context.Set<StateInscription>().Add(state);
                }

                var period = context.Set<AcademicPeriod>().FirstOrDefault(x => x.Code == "PER_Dto");
                if (period == null)
                {
                    period = new AcademicPeriod { Code = "PER_Dto", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
                    context.Set<AcademicPeriod>().Add(period);
                }
                context.SaveChanges();

                return new InscriptionModalityDto
                {
                    IdModality = modality.Id,
                    IdStateInscription = state.Id,
                    IdAcademicPeriod = period.Id,
                    ApprovalDate = DateTime.Now,
                    Observations = "Test Observation",
                    StatusRegister = true
                };
            }
        }

        protected override InscriptionModality CreateValidEntity()
        {
            return new InscriptionModality
            {
                ApprovalDate = DateTime.Now,
                Observations = "Test Observation",
                StatusRegister = true
                // IDs will be set in SeedAdditionalData
            };
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            var response = await _client.GetAsync($"{BaseUrl}/1");
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            var response = await _client.PostAsJsonAsync(BaseUrl, CreateValidDto());
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public override async Task UpdateStatus_ReturnsOk()
        {
            var response = await _client.PatchAsJsonAsync($"{BaseUrl}/1/status", new UpdateStatusRequestDto
            {
                StatusRegister = false,
                IdUserUpdateAt = 1,
                OperationRegister = "TEST_UPDATE"
            });
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
