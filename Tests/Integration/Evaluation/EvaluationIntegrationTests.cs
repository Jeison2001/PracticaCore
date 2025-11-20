using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Evaluation;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Evaluation
{
    public class EvaluationIntegrationTests : GenericControllerIntegrationTests<Domain.Entities.Evaluation, EvaluationDto>
    {
        public EvaluationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Evaluation";

        private async Task<(EvaluationType Type, Domain.Entities.User Evaluator, IdentificationType IdType, AcademicProgram Program)> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Evaluation Type
            var type = new EvaluationType 
            { 
                Name = "Final Evaluation " + Guid.NewGuid(), 
                Code = "FINAL_" + Guid.NewGuid().ToString().Substring(0, 4), 
                StatusRegister = true 
            };
            context.Set<EvaluationType>().Add(type);

            // 2. Dependencies for User
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

            // 3. Evaluator User
            var evaluator = new Domain.Entities.User 
            { 
                FirstName = "Evaluator", 
                LastName = "Teacher", 
                Email = "evaluator@test.com", 
                Identification = "999888777", 
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            context.Set<Domain.Entities.User>().Add(evaluator);

            await context.SaveChangesAsync();

            return (type, evaluator, idType, program);
        }

        protected override EvaluationDto CreateValidDto()
        {
            // We need to run this synchronously in this context, or pre-seed.
            // Since CreateValidDto is called by the base class tests, we can't easily await here.
            // Strategy: Use a "Lazy" seeding or seed in the test method.
            // However, the base class calls CreateValidDto().
            // We will override the Test methods instead to ensure seeding happens.
            return new EvaluationDto(); // Placeholder
        }

        protected override Domain.Entities.Evaluation CreateValidEntity()
        {
            return new Domain.Entities.Evaluation(); // Placeholder
        }

        // Override tests to handle async seeding

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();

            var dto = new EvaluationDto
            {
                EntityType = "PreliminaryProject",
                EntityId = 1, // Mock ID
                IdEvaluationType = type.Id,
                IdEvaluator = evaluator.Id,
                Result = "Approved",
                Observations = "Excellent work",
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvaluationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
            result.Data!.Result.Should().Be("Approved");
        }

        [Fact]
        public override async Task GetAll_ReturnsOkAndList()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var evaluation = new Domain.Entities.Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Evaluation>().Add(evaluation);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<EvaluationDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public override async Task GetById_ReturnsOkAndEntity()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();
            int evaluationId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var evaluation = new Domain.Entities.Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Evaluation>().Add(evaluation);
                await context.SaveChangesAsync();
                evaluationId = evaluation.Id;
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/{evaluationId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvaluationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Id.Should().Be(evaluationId);
        }

        [Fact]
        public override async Task Update_ReturnsOk()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();
            int evaluationId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var evaluation = new Domain.Entities.Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Domain.Entities.Evaluation>().Add(evaluation);
                await context.SaveChangesAsync();
                evaluationId = evaluation.Id;
            }

            var updateDto = new EvaluationDto
            {
                Id = evaluationId,
                EntityType = "PreliminaryProject",
                EntityId = 1,
                IdEvaluationType = type.Id,
                IdEvaluator = evaluator.Id,
                Result = "Rejected",
                Observations = "Needs improvement",
                StatusRegister = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{evaluationId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<EvaluationDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Result.Should().Be("Rejected");
        }
    }
}
