using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Documents;
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

        /// <summary>
        /// Siembra los datos base (modalidad, estado, periodo, inscripcion, clase y tipo de documento).
        /// Por defecto vincula al usuario <paramref name="linkUserId"/> con la inscripcion creando un
        /// <see cref="UserInscriptionModality"/>, de modo que ese usuario quede AUTORIZADO a modificar
        /// los documentos de la inscripcion (camino real de un usuario vinculado). El default 1 coincide
        /// con el UserId que inyecta TestAuthHandler (claim "sub"=1). Pasar un id distinto al autenticado
        /// (p.ej. 999) permite probar el caso DENEGADO.
        /// </summary>
        private async Task<(InscriptionModality InscriptionModality, DocumentType DocumentType)> SeedDataAsync(int linkUserId = 1)
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
            await context.SaveChangesAsync();

            // Vincular al usuario con la inscripcion para que el guard de acceso lo autorice.
            var userInscription = new UserInscriptionModality
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdUser = linkUserId,
                StatusRegister = true
            };
            context.Set<UserInscriptionModality>().Add(userInscription);

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

            // Verificar en BD
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedDoc = await context.Set<Document>().FindAsync(docId);
                updatedDoc.Should().NotBeNull();
                updatedDoc!.StatusRegister.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Update_ShouldReturnForbidden_WhenUserNotLinkedToInscription()
        {
            // Vinculamos la inscripcion a OTRO usuario (999), no al autenticado por TestAuthHandler (1).
            var (inscriptionModality, docType) = await SeedDataAsync(linkUserId: 999);
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
            content.Add(new StringContent("Hacked Name"), "Name");
            content.Add(new StringContent("UpdateOp"), "OperationRegister");

            var fileContent = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "File", "hacked.pdf");

            // Act
            var response = await _client.PutAsync($"/api/Document/{docId}", content);

            // Assert: el usuario no vinculado recibe 403 Forbidden.
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Y el documento NO debe haber cambiado en BD (la autorizacion corta ANTES de persistir).
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unchanged = await context.Set<Document>().FindAsync(docId);
                unchanged.Should().NotBeNull();
                unchanged!.Name.Should().Be("Original Name");
                unchanged.OriginalFileName.Should().Be("original.pdf");
            }
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnForbidden_WhenUserNotLinkedToInscription()
        {
            // Vinculamos la inscripcion a OTRO usuario (999), no al autenticado por TestAuthHandler (1).
            var (inscriptionModality, docType) = await SeedDataAsync(linkUserId: 999);
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

            // Assert: el usuario no vinculado recibe 403 Forbidden.
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Y el StatusRegister NO debe haber cambiado en BD.
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var unchanged = await context.Set<Document>().FindAsync(docId);
                unchanged.Should().NotBeNull();
                unchanged!.StatusRegister.Should().BeTrue();
            }
        }
    }
}
