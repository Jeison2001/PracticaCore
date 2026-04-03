using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Evaluations;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Evaluations
{
    public class EvaluationControllerTests : GenericControllerIntegrationTests<Evaluation, EvaluationDto>
    {
        public EvaluationControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        protected override string BaseUrl => "/api/Evaluation";

        private async Task<(EvaluationType Type, User Evaluator, IdentificationType IdType, AcademicProgram Program)> SeedDependenciesAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Tipo de Evaluación
            var type = new EvaluationType 
            { 
                Name = "Final Evaluation " + Guid.NewGuid(), 
                Code = "FINAL_" + Guid.NewGuid().ToString().Substring(0, 4), 
                StatusRegister = true 
            };
            context.Set<EvaluationType>().Add(type);

            // 2. Dependencias para User
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

            // 3. Usuario Evaluador
            var evaluator = new User 
            { 
                FirstName = "Evaluator", 
                LastName = "Teacher", 
                Email = "evaluator@test.com", 
                Identification = "999888777", 
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            context.Set<User>().Add(evaluator);

            await context.SaveChangesAsync();

            return (type, evaluator, idType, program);
        }

        protected override EvaluationDto CreateValidDto()
        {
            // Necesitamos ejecutar esto sincrónicamente en este contexto, o pre-seeder.
            // Como CreateValidDto es llamado por los tests de la clase base, no podemos usar await fácilmente aquí.
            // Estrategia: Usar seed "lazy" o seed en el método de test.
            // Sin embargo, la clase base llama a CreateValidDto().
            // Vamos a hacer override de los métodos de test en su lugar para asegurar que el seed ocurra.
            return new EvaluationDto(); // Placeholder
        }

        protected override Evaluation CreateValidEntity()
        {
            return new Evaluation(); // Placeholder
        }

        // Override de tests para manejar seed asíncrono

        [Fact]
        public override async Task Create_ReturnsCreated()
        {
            // Arrange
            var (type, evaluator, _, _) = await SeedDependenciesAsync();

            var dto = new EvaluationDto
            {
                EntityType = "PreliminaryProject",
                EntityId = 1, // ID simulado
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
                var evaluation = new Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Evaluation>().Add(evaluation);
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
                var evaluation = new Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Evaluation>().Add(evaluation);
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
                var evaluation = new Evaluation
                {
                    EntityType = "PreliminaryProject",
                    EntityId = 1,
                    IdEvaluationType = type.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Pending",
                    Observations = "Initial",
                    StatusRegister = true
                };
                context.Set<Evaluation>().Add(evaluation);
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
