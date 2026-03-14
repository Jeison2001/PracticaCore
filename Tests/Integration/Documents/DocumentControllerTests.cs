using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.RequiredDocumentsByState;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Documents
{
    public class DocumentControllerTests : IntegrationTestBase
    {
        public DocumentControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(InscriptionModality InscriptionModality, DocumentType DocumentType)> SeedDataAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var modality = new Modality { Name = "Pasantia", Description = "Pasantia", StatusRegister = true };
            var state = new StateInscription { Name = "Activo", Description = "Activo", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2024-1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
            
            context.Set<Modality>().Add(modality);
            context.Set<StateInscription>().Add(state);
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();

            var inscriptionModality = new InscriptionModality 
            { 
                IdModality = modality.Id,
                IdStateInscription = state.Id,
                IdAcademicPeriod = period.Id,
                StatusRegister = true
            };
            
            context.Set<InscriptionModality>().Add(inscriptionModality);

            var docClass = new DocumentClass { Name = "General", Description = "General", StatusRegister = true };
            context.Set<DocumentClass>().Add(docClass);
            await context.SaveChangesAsync();

            var docType = new DocumentType 
            { 
                Name = "Carta", 
                Description = "Carta de presentacion", 
                IdDocumentClass = docClass.Id,
                StatusRegister = true 
            };
            context.Set<DocumentType>().Add(docType);
            await context.SaveChangesAsync();

            return (inscriptionModality, docType);
        }

        [Fact]
        public async Task Upload_ShouldReturnCreated_WhenFileIsValid()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(inscriptionModality.Id.ToString()), "IdInscriptionModality");
            content.Add(new StringContent(docType.Id.ToString()), "IdDocumentType");
            content.Add(new StringContent("Test Document"), "Name");
            content.Add(new StringContent("1"), "IdUserCreatedAt");
            content.Add(new StringContent("Test"), "OperationRegister");

            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "File", "test.pdf");

            // Act
            var response = await _client.PostAsync("/api/Document", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.OriginalFileName.Should().Be("test.pdf");
        }

        [Fact]
        public async Task GetById_ShouldReturnDocument_WhenExists()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();
            int docId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var doc = new Document
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdDocumentType = docType.Id,
                    Name = "Test Doc",
                    OriginalFileName = "test.pdf",
                    StoredFileName = "test_stored.pdf",
                    StoragePath = "Uploads",
                    MimeType = "application/pdf",
                    FileSize = 1024,
                    StatusRegister = true
                };
                context.Set<Document>().Add(doc);
                await context.SaveChangesAsync();
                docId = doc.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/Document/{docId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(docId);
        }

        [Fact]
        public async Task GetByInscriptionModality_ShouldReturnList_WhenExists()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();
            
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var doc = new Document
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdDocumentType = docType.Id,
                    Name = "Test Doc",
                    OriginalFileName = "test.pdf",
                    StoredFileName = "test_stored.pdf",
                    StoragePath = "Uploads",
                    MimeType = "application/pdf",
                    FileSize = 1024,
                    StatusRegister = true
                };
                context.Set<Document>().Add(doc);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/Document/ByInscriptionModality/{inscriptionModality.Id}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenDataIsValid()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();
            int docId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var doc = new Document
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdDocumentType = docType.Id,
                    Name = "Original Name",
                    OriginalFileName = "original.pdf",
                    StoredFileName = "original_stored.pdf",
                    StoragePath = "Uploads",
                    MimeType = "application/pdf",
                    FileSize = 1024,
                    StatusRegister = true
                };
                context.Set<Document>().Add(doc);
                await context.SaveChangesAsync();
                docId = doc.Id;
            }

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(inscriptionModality.Id.ToString()), "IdInscriptionModality");
            content.Add(new StringContent(docType.Id.ToString()), "IdDocumentType");
            content.Add(new StringContent("Updated Name"), "Name");
            content.Add(new StringContent("1"), "IdUserUpdatedAt");
            content.Add(new StringContent("UpdateOp"), "OperationRegister");

            var fileContent = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "File", "updated.pdf");

            // Act
            var response = await _client.PutAsync($"/api/Document/{docId}", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Updated Name");
            result.Data!.OriginalFileName.Should().Be("updated.pdf");
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnOk_WhenDocumentExists()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();
            int docId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var doc = new Document
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdDocumentType = docType.Id,
                    Name = "Test Doc",
                    OriginalFileName = "test.pdf",
                    StoredFileName = "test_stored.pdf",
                    StoragePath = "Uploads",
                    MimeType = "application/pdf",
                    FileSize = 1024,
                    StatusRegister = true
                };
                context.Set<Document>().Add(doc);
                await context.SaveChangesAsync();
                docId = doc.Id;
            }

            var updateStatusDto = new UpdateStatusRequestDto
            {
                StatusRegister = false,
                IdUserUpdateAt = 1,
                OperationRegister = "Deactivate"
            };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/Document/{docId}/status", updateStatusDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().BeTrue();

            // Verify in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedDoc = await context.Set<Document>().FindAsync(docId);
                updatedDoc.Should().NotBeNull();
                updatedDoc!.StatusRegister.Should().BeFalse();
            }
        }

        [Fact]
        public async Task GetRequiredDocumentsByCurrentState_ShouldReturnList()
        {
            // Arrange
            var (inscriptionModality, docType) = await SeedDataAsync();
            int academicPracticeId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Create StageModality
                var stageModality = new StageModality
                {
                    IdModality = inscriptionModality.IdModality,
                    Name = "Stage 1",
                    Code = "S1",
                    StageOrder = 1,
                    StatusRegister = true
                };
                context.Set<StageModality>().Add(stageModality);
                await context.SaveChangesAsync();

                // Create StateStage
                var stateStage = new StateStage
                {
                    IdStageModality = stageModality.Id,
                    Name = "In Progress",
                    Code = "IN_PROGRESS",
                    StatusRegister = true
                };
                context.Set<StateStage>().Add(stateStage);
                await context.SaveChangesAsync();

                // Create AcademicPractice
                // Note: The repository queries AcademicPractice by Id using the passed inscriptionModalityId.
                // We will create an AcademicPractice and use its Id in the call.
                var academicPractice = new AcademicPractice
                {
                    IdStateStage = stateStage.Id,
                    StatusRegister = true,
                    Title = "Test Practice", // Added Title
                    // Assuming InscriptionModality is required, but we can try to set it or rely on EF
                    // Since we are in InMemory, we can just set the navigation property if needed, 
                    // but let's try to set the shadow FK if possible or just add it.
                    // However, AcademicPractice has a required InscriptionModality property.
                    // We need to link it to an InscriptionModality.
                    // We can use the one from SeedDataAsync.
                    // But wait, InscriptionModality might already be tracked.
                    // We should attach it or fetch it.
                };
                
                // To avoid tracking issues, let's just set the ID if we knew the FK column name, 
                // but better to fetch the existing InscriptionModality and assign it.
                var existingModality = await context.Set<InscriptionModality>().FindAsync(inscriptionModality.Id);
                academicPractice.InscriptionModality = existingModality!;
                
                context.Set<AcademicPractice>().Add(academicPractice);
                await context.SaveChangesAsync();
                academicPracticeId = academicPractice.Id;

                // Create RequiredDocumentsByState
                var requiredDoc = new RequiredDocumentsByState
                {
                    IdStateStage = stateStage.Id,
                    IdDocumentType = docType.Id,
                    IsRequired = true,
                    StatusRegister = true
                };
                context.Set<RequiredDocumentsByState>().Add(requiredDoc);
                await context.SaveChangesAsync();
            }

            // Act
            // We pass academicPracticeId because the repository queries AcademicPractice.Id
            var response = await _client.GetAsync($"/api/Document/RequiredByCurrentState/{academicPracticeId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<RequiredDocumentsByStateDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().Contain(x => x.DocumentTypeId == docType.Id);
        }
    }
}
