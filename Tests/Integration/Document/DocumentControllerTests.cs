using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Document;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration;
using Xunit;

namespace Tests.Integration.Document
{
    public class DocumentControllerTests : IntegrationTestBase
    {
        public DocumentControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(InscriptionModality InscriptionModality, Domain.Entities.DocumentType DocumentType)> SeedDataAsync()
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

            var docType = new Domain.Entities.DocumentType 
            { 
                Name = "Carta", 
                Description = "Carta de presentacion", 
                IdDocumentClass = docClass.Id,
                StatusRegister = true 
            };
            context.Set<Domain.Entities.DocumentType>().Add(docType);
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
                var doc = new Domain.Entities.Document
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
                context.Set<Domain.Entities.Document>().Add(doc);
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
                var doc = new Domain.Entities.Document
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
                context.Set<Domain.Entities.Document>().Add(doc);
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
    }
}
