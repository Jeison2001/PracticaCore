using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Documents;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Services.Storage;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        [Fact]
        public async Task ReadEndpoints_ShouldReturnForbidden_WhenUserIsUnrelatedAndLacksReadAllPermission()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, _) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999);

            const int unrelatedUserId = 5001;
            const string permissions = "NONE";

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", unrelatedUserId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", unrelatedUserId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", unrelatedUserId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            byModalityRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            storageMock.Verify(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnForbidden_WhenTeacherIsUnconfirmedWithReviewOnlyPermission()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            const int teacherUserId = 5002;
            var (inscriptionId, docId, _) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                teacherUserId: teacherUserId,
                teacherIsConfirmed: false);

            var permissions = PermissionCodes.ProyectoGrado.N4PGPAR;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", teacherUserId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", teacherUserId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", teacherUserId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            byModalityRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            storageMock.Verify(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnOk_WhenTeacherIsConfirmedWithReviewOnlyPermission()
        {
            var expectedBytes = new byte[] { 10, 20, 30, 40 };
            var storageMock = new Mock<IFileStorageService>();
            storageMock
                .Setup(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(expectedBytes));

            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            const int teacherUserId = 5003;
            var (inscriptionId, docId, storedFileName) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                teacherUserId: teacherUserId,
                teacherIsConfirmed: true);

            var permissions = PermissionCodes.ProyectoGrado.N4PGPAR;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", teacherUserId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", teacherUserId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", teacherUserId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var docPayload = await getByIdRes.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            docPayload!.Data!.Id.Should().Be(docId);

            downloadRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var downloadedBytes = await downloadRes.Content.ReadAsByteArrayAsync();
            downloadedBytes.Should().Equal(expectedBytes);
            storageMock.Verify(s => s.GetFileAsync(storedFileName, It.IsAny<CancellationToken>()), Times.Once);

            byModalityRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var listPayload = await byModalityRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            listPayload!.Data!.Items.Should().Contain(d => d.Id == docId);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnOk_WhenUnlinkedUserHasReadAllPermission()
        {
            var expectedBytes = new byte[] { 1, 3, 5, 7 };
            var storageMock = new Mock<IFileStorageService>();
            storageMock
                .Setup(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(expectedBytes));

            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, storedFileName) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999);

            const int coordinatorUserId = 5004;
            var permissions = PermissionCodes.ProyectoGrado.N4PGPPG;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", coordinatorUserId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", coordinatorUserId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", coordinatorUserId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.OK);
            (await downloadRes.Content.ReadAsByteArrayAsync()).Should().Equal(expectedBytes);
            storageMock.Verify(s => s.GetFileAsync(storedFileName, It.IsAny<CancellationToken>()), Times.Once);

            byModalityRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var listPayload = await byModalityRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            listPayload!.Data!.Items.Should().Contain(d => d.Id == docId);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnForbidden_WhenUserHasPermissionFromDifferentModality()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, _) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                modalityCode: ModalityCodes.ProyectoGrado,
                stageCode: StageModalityCodes.PgFasePropuesta,
                stageOrder: 1);

            const int seminarioUserId = 5005;
            var permissions = PermissionCodes.Seminario.N3SASGG;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", seminarioUserId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", seminarioUserId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", seminarioUserId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            byModalityRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            storageMock.Verify(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnForbidden_WhenUserHasPermissionFromDifferentStage()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, _) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                modalityCode: ModalityCodes.ProyectoGrado,
                stageCode: StageModalityCodes.PgFaseProyectoInforme,
                stageOrder: 3);

            const int propuestaCoordinatorId = 5006;
            var permissions = PermissionCodes.ProyectoGrado.N4PGPPG;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", propuestaCoordinatorId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", propuestaCoordinatorId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", propuestaCoordinatorId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            byModalityRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            storageMock.Verify(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnForbidden_WhenTeacherHasDefaultTeacherPermissionsButIsNotAssigned()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, _) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                modalityCode: ModalityCodes.ProyectoGrado,
                stageCode: StageModalityCodes.PgFaseAnteproyecto,
                stageOrder: 2);

            const int unlinkedTeacherId = 5007;
            var permissions = string.Join(",",
                PermissionCodes.ModalidadesGrado.N2MGP,
                PermissionCodes.ModalidadesGrado.N3MGPG,
                PermissionCodes.CoTerminal.N3CTSGG,
                PermissionCodes.ProyectoGrado.N4PGPAR);

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", unlinkedTeacherId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", unlinkedTeacherId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", unlinkedTeacherId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            byModalityRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            storageMock.Verify(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReadEndpoints_ShouldReturnOk_WhenUserHasMatchingModalityAndStagePermission()
        {
            var expectedBytes = new byte[] { 9, 8, 7, 6 };
            var storageMock = new Mock<IFileStorageService>();
            storageMock
                .Setup(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(expectedBytes));

            using var customFactory = CreateFactoryWithMockStorage(storageMock);
            using var client = customFactory.CreateClient();

            var (inscriptionId, docId, storedFileName) = await SeedReadScenarioAsync(
                customFactory.Services,
                studentUserId: 999,
                modalityCode: ModalityCodes.ProyectoGrado,
                stageCode: StageModalityCodes.PgFaseAnteproyecto,
                stageOrder: 2);

            const int anteproyectoCoordinatorId = 5008;
            var permissions = PermissionCodes.ProyectoGrado.N4PGPAG;

            using var getByIdReq = CreateReadRequest($"/api/Document/{docId}", anteproyectoCoordinatorId, permissions);
            using var downloadReq = CreateReadRequest($"/api/Document/File/{docId}", anteproyectoCoordinatorId, permissions);
            using var byModalityReq = CreateReadRequest($"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10", anteproyectoCoordinatorId, permissions);

            var getByIdRes = await client.SendAsync(getByIdReq);
            var downloadRes = await client.SendAsync(downloadReq);
            var byModalityRes = await client.SendAsync(byModalityReq);

            getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadRes.StatusCode.Should().Be(HttpStatusCode.OK);
            (await downloadRes.Content.ReadAsByteArrayAsync()).Should().Equal(expectedBytes);
            storageMock.Verify(s => s.GetFileAsync(storedFileName, It.IsAny<CancellationToken>()), Times.Once);

            byModalityRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var listPayload = await byModalityRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            listPayload!.Data!.Items.Should().Contain(d => d.Id == docId);
        }

        private WebApplicationFactory<Program> CreateFactoryWithMockStorage(Mock<IFileStorageService> storageMock)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_ => storageMock.Object);
                });
            });
        }

        private static HttpRequestMessage CreateReadRequest(string url, int userId, string permissions)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Test-UserId", userId.ToString());
            request.Headers.Add("X-Test-Permissions", permissions);
            return request;
        }

        private static async Task<(int InscriptionId, int DocumentId, string StoredFileName)> SeedReadScenarioAsync(
            IServiceProvider services,
            int studentUserId,
            int? teacherUserId = null,
            bool teacherIsConfirmed = true,
            string modalityCode = ModalityCodes.ProyectoGrado,
            string? stageCode = StageModalityCodes.PgFasePropuesta,
            int stageOrder = 1)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var modality = new Modality { Code = modalityCode, Name = "Proyecto de Grado", Description = "Proyecto", StatusRegister = true };
            var state = new StateInscription { Name = "Activo", Description = "Activo", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2026-1", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6), StatusRegister = true };
            var docClass = new DocumentClass { Name = "General", Description = "General", StatusRegister = true };

            context.Set<Modality>().Add(modality);
            context.Set<StateInscription>().Add(state);
            context.Set<AcademicPeriod>().Add(period);
            context.Set<DocumentClass>().Add(docClass);
            await context.SaveChangesAsync();

            StageModality? stage = null;
            if (!string.IsNullOrWhiteSpace(stageCode))
            {
                stage = new StageModality
                {
                    IdModality = modality.Id,
                    Code = stageCode,
                    Name = stageCode,
                    StageOrder = stageOrder,
                    StatusRegister = true
                };
                context.Set<StageModality>().Add(stage);
                await context.SaveChangesAsync();
            }

            var inscriptionModality = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStageModality = stage?.Id,
                IdStateInscription = state.Id,
                IdAcademicPeriod = period.Id,
                StatusRegister = true
            };
            context.Set<InscriptionModality>().Add(inscriptionModality);

            var docType = new DocumentType
            {
                Name = "Propuesta",
                Description = "Propuesta",
                IdDocumentClass = docClass.Id,
                IdStageModality = stage?.Id,
                StatusRegister = true
            };
            context.Set<DocumentType>().Add(docType);
            await context.SaveChangesAsync();

            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdUser = studentUserId,
                StatusRegister = true
            });

            if (teacherUserId.HasValue)
            {
                var assignmentType = new TypeTeachingAssignment
                {
                    Code = $"DIR_{Guid.NewGuid():N}"[..16],
                    Name = "Director",
                    StatusRegister = true
                };
                context.Set<TypeTeachingAssignment>().Add(assignmentType);
                await context.SaveChangesAsync();

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacherUserId.Value,
                    IdTypeTeachingAssignment = assignmentType.Id,
                    IsConfirmed = teacherIsConfirmed,
                    RevocationDate = null,
                    StatusRegister = true
                });
            }

            var storedFileName = $"stored_{Guid.NewGuid():N}.pdf";
            var document = new Document
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdDocumentType = docType.Id,
                Name = "Protected Doc",
                OriginalFileName = "protected.pdf",
                StoredFileName = storedFileName,
                StoragePath = "Uploads",
                MimeType = "application/pdf",
                FileSize = 1024,
                StatusRegister = true
            };
            context.Set<Document>().Add(document);
            await context.SaveChangesAsync();

            return (inscriptionModality.Id, document.Id, storedFileName);
        }
    }
}
