using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.Evaluations;
using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Domain.Common;
using Domain.Common.Dashboard;
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

namespace Tests.Integration.Authorization
{
    public class ActorDocumentsInscriptionsAndEvaluationMatrixTests : IntegrationTestBase
    {
        public ActorDocumentsInscriptionsAndEvaluationMatrixTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private sealed record MatrixSeedResult(
            int PgInscriptionId,
            int PaInscriptionId,
            int PropuestaStageId,
            int InformeStageId,
            int SustentacionStageId,
            int PaInscripcionStageId,
            int DocTypeInformeId,
            int Doc1PropuestaId,
            int Doc2InformeId,
            int Doc3PaId,
            byte[] Doc1Bytes,
            byte[] Doc2Bytes,
            byte[] Doc3Bytes);

        [Fact]
        public async Task DocumentController_StudentOwner_ReadsOwnDocsAndBytes_AndGetsForbiddenOnOtherModality()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            const int studentId = 101;
            const string permissions = PermissionCodes.ModalidadesGrado.N2MGC;

            using var doc1Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc1PropuestaId}", studentId, permissions);
            using var doc1FileReq = CreateRequest(HttpMethod.Get, $"/api/Document/File/{seed.Doc1PropuestaId}", studentId, permissions);
            using var pgListReq = CreateRequest(HttpMethod.Get, $"/api/Document/ByInscriptionModality/{seed.PgInscriptionId}?PageNumber=1&PageSize=10", studentId, permissions);
            using var doc3Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc3PaId}", studentId, permissions);
            using var doc3FileReq = CreateRequest(HttpMethod.Get, $"/api/Document/File/{seed.Doc3PaId}", studentId, permissions);
            using var paListReq = CreateRequest(HttpMethod.Get, $"/api/Document/ByInscriptionModality/{seed.PaInscriptionId}?PageNumber=1&PageSize=10", studentId, permissions);

            var doc1Res = await client.SendAsync(doc1Req);
            var doc1FileRes = await client.SendAsync(doc1FileReq);
            var pgListRes = await client.SendAsync(pgListReq);
            var doc3Res = await client.SendAsync(doc3Req);
            var doc3FileRes = await client.SendAsync(doc3FileReq);
            var paListRes = await client.SendAsync(paListReq);

            doc1Res.StatusCode.Should().Be(HttpStatusCode.OK);
            var doc1Payload = await doc1Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            doc1Payload.Should().NotBeNull();
            doc1Payload!.Success.Should().BeTrue();
            doc1Payload.Data!.Id.Should().Be(seed.Doc1PropuestaId);
            doc1Payload.Data.OriginalFileName.Should().Be("propuesta_pg.pdf");

            doc1FileRes.StatusCode.Should().Be(HttpStatusCode.OK);
            (await doc1FileRes.Content.ReadAsByteArrayAsync()).Should().Equal(seed.Doc1Bytes);

            pgListRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var pgListPayload = await pgListRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            pgListPayload!.Data!.Items.Should().Contain(d => d.Id == seed.Doc1PropuestaId && d.OriginalFileName == "propuesta_pg.pdf");
            pgListPayload.Data.Items.Should().Contain(d => d.Id == seed.Doc2InformeId && d.OriginalFileName == "informe_pg.pdf");

            doc3Res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            doc3FileRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            paListRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DocumentController_SustentacionCommittee_ReadsPreviousPgPhase_AndGetsForbiddenOnOtherModalityOrManipulatedStage()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            const int committeeUserId = 202;
            const string permissions = PermissionCodes.ProyectoGrado.N4PGPSG;

            using var doc2Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc2InformeId}", committeeUserId, permissions);
            using var byStageReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{seed.PgInscriptionId}?idStageModality={seed.InformeStageId}&PageNumber=1&PageSize=10",
                committeeUserId,
                permissions);
            using var byDocTypeReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{seed.PgInscriptionId}?Filters[idDocumentType]={seed.DocTypeInformeId}&PageNumber=1&PageSize=10",
                committeeUserId,
                permissions);
            using var doc3Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc3PaId}", committeeUserId, permissions);
            using var manipulatedStageReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{seed.PgInscriptionId}?idStageModality={seed.PaInscripcionStageId}&PageNumber=1&PageSize=10",
                committeeUserId,
                permissions);

            var doc2Res = await client.SendAsync(doc2Req);
            var byStageRes = await client.SendAsync(byStageReq);
            var byDocTypeRes = await client.SendAsync(byDocTypeReq);
            var doc3Res = await client.SendAsync(doc3Req);
            var manipulatedStageRes = await client.SendAsync(manipulatedStageReq);

            doc2Res.StatusCode.Should().Be(HttpStatusCode.OK);
            var doc2Payload = await doc2Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            doc2Payload!.Data!.Id.Should().Be(seed.Doc2InformeId);
            doc2Payload.Data.OriginalFileName.Should().Be("informe_pg.pdf");

            byStageRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var byStagePayload = await byStageRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            byStagePayload!.Data!.Items.Should().Contain(d => d.Id == seed.Doc2InformeId && d.OriginalFileName == "informe_pg.pdf");
            byStagePayload.Data.Items.Should().NotContain(d => d.Id == seed.Doc1PropuestaId);

            byDocTypeRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var byDocTypePayload = await byDocTypeRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<DocumentDto>>>();
            byDocTypePayload!.Data!.Items.Should().Contain(d => d.Id == seed.Doc2InformeId && d.OriginalFileName == "informe_pg.pdf");

            doc3Res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            manipulatedStageRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DocumentController_PropuestaCommittee_ReadsPropuestaDoc_AndGetsForbiddenOnInformeStage()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            const int committeeUserId = 203;
            const string permissions = PermissionCodes.ProyectoGrado.N4PGPPG;

            using var doc1Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc1PropuestaId}", committeeUserId, permissions);
            using var doc2Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc2InformeId}", committeeUserId, permissions);
            using var informeStageReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{seed.PgInscriptionId}?idStageModality={seed.InformeStageId}&PageNumber=1&PageSize=10",
                committeeUserId,
                permissions);

            var doc1Res = await client.SendAsync(doc1Req);
            var doc2Res = await client.SendAsync(doc2Req);
            var informeStageRes = await client.SendAsync(informeStageReq);

            doc1Res.StatusCode.Should().Be(HttpStatusCode.OK);
            var doc1Payload = await doc1Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
            doc1Payload!.Data!.Id.Should().Be(seed.Doc1PropuestaId);
            doc1Payload.Data.OriginalFileName.Should().Be("propuesta_pg.pdf");

            doc2Res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            informeStageRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData(PermissionCodes.Configuraciones.N3CFDOT)]
        [InlineData(PermissionCodes.Configuraciones.N2CFDO)]
        [InlineData(PermissionCodes.Configuraciones.N3CFSU)]
        public async Task DocumentController_GlobalAdminPermissions_ReadAllDocumentsAcrossModalitiesAndPhases(string adminPermission)
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            const int adminUserId = 301;

            using var doc1Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc1PropuestaId}", adminUserId, adminPermission);
            using var doc2Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc2InformeId}", adminUserId, adminPermission);
            using var doc3Req = CreateRequest(HttpMethod.Get, $"/api/Document/{seed.Doc3PaId}", adminUserId, adminPermission);

            var doc1Res = await client.SendAsync(doc1Req);
            var doc2Res = await client.SendAsync(doc2Req);
            var doc3Res = await client.SendAsync(doc3Req);

            doc1Res.StatusCode.Should().Be(HttpStatusCode.OK);
            (await doc1Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>())!.Data!.Id.Should().Be(seed.Doc1PropuestaId);

            doc2Res.StatusCode.Should().Be(HttpStatusCode.OK);
            (await doc2Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>())!.Data!.Id.Should().Be(seed.Doc2InformeId);

            doc3Res.StatusCode.Should().Be(HttpStatusCode.OK);
            (await doc3Res.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>())!.Data!.Id.Should().Be(seed.Doc3PaId);
        }

        [Fact]
        public async Task InscriptionAndInscriptionModalityControllers_StudentAndManagers_EnforceExpectedPermissionsAndData()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            const int studentId = 101;
            const string studentPerm = PermissionCodes.ModalidadesGrado.N2MGC;

            using var ownByUserReq = CreateRequest(HttpMethod.Get, $"/api/Inscription/ByUser/{studentId}", studentId, studentPerm);
            using var otherByUserReq = CreateRequest(HttpMethod.Get, "/api/Inscription/ByUser/999", studentId, studentPerm);
            using var getAllStudentReq = CreateRequest(HttpMethod.Get, "/api/Inscription?PageNumber=1&PageSize=10", studentId, studentPerm);
            using var patchStudentReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{seed.PgInscriptionId}",
                studentId,
                studentPerm,
                new InscriptionModalityPatchDto { Observations = "Intento no autorizado" });

            var ownByUserRes = await client.SendAsync(ownByUserReq);
            var otherByUserRes = await client.SendAsync(otherByUserReq);
            var getAllStudentRes = await client.SendAsync(getAllStudentReq);
            var patchStudentRes = await client.SendAsync(patchStudentReq);

            ownByUserRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var ownPayload = await ownByUserRes.Content.ReadFromJsonAsync<ApiResponse<List<InscriptionWithStudentsResponseDto>>>();
            ownPayload!.Success.Should().BeTrue();
            ownPayload.Data.Should().ContainSingle(i => i.InscriptionModality.Id == seed.PgInscriptionId);

            // GetByUserId solo exige [RequirePermission] sin filtro anti-IDOR por claim sub; para 999 sin registros retorna 200 con lista vacia.
            otherByUserRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var otherPayload = await otherByUserRes.Content.ReadFromJsonAsync<ApiResponse<List<InscriptionWithStudentsResponseDto>>>();
            otherPayload!.Data.Should().BeEmpty();

            getAllStudentRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            patchStudentRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            const int managerId = 401;
            using var getAllManagerReq = CreateRequest(
                HttpMethod.Get,
                "/api/Inscription?PageNumber=1&PageSize=10",
                managerId,
                PermissionCodes.ModalidadesGrado.N3MGPG);
            using var getAllReviewerReq = CreateRequest(
                HttpMethod.Get,
                "/api/Inscription?PageNumber=1&PageSize=10",
                managerId,
                PermissionCodes.ModalidadesGrado.N3MGPR);
            using var patchReviewerReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{seed.PgInscriptionId}",
                managerId,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto { Observations = "Observacion aprobada por revisor" });

            var getAllManagerRes = await client.SendAsync(getAllManagerReq);
            var getAllReviewerRes = await client.SendAsync(getAllReviewerReq);
            var patchReviewerRes = await client.SendAsync(patchReviewerReq);

            getAllManagerRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var managerList = await getAllManagerRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<InscriptionWithStudentsResponseDto>>>();
            managerList!.Data!.Items.Should().Contain(i => i.InscriptionModality.Id == seed.PgInscriptionId);
            managerList.Data.Items.Should().Contain(i => i.InscriptionModality.Id == seed.PaInscriptionId);

            getAllReviewerRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var reviewerList = await getAllReviewerRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<InscriptionWithStudentsResponseDto>>>();
            reviewerList!.Data!.Items.Should().Contain(i => i.InscriptionModality.Id == seed.PgInscriptionId);

            patchReviewerRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var patchPayload = await patchReviewerRes.Content.ReadFromJsonAsync<ApiResponse<InscriptionModalityDto>>();
            patchPayload!.Data!.Observations.Should().Be("Observacion aprobada por revisor");
        }

        [Fact]
        public async Task DashboardController_Filters_AllowsN2EDDWithValidStructure_AndForbidsStudentOrTeacher()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            using var adminReq = CreateRequest(HttpMethod.Get, "/api/Dashboard/filters", 501, PermissionCodes.Reportes.N2EDD);
            using var studentReq = CreateRequest(HttpMethod.Get, "/api/Dashboard/filters", 101, PermissionCodes.ModalidadesGrado.N2MGC);
            using var teacherReq = CreateRequest(HttpMethod.Get, "/api/Dashboard/filters", 601, PermissionCodes.ProyectoGrado.N4PGPAR);

            var adminRes = await client.SendAsync(adminReq);
            var studentRes = await client.SendAsync(studentReq);
            var teacherRes = await client.SendAsync(teacherReq);

            adminRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await adminRes.Content.ReadFromJsonAsync<ApiResponse<DashboardFilterOptions>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().NotBeNull();
            payload.Data!.Modalities.Should().NotBeEmpty();
            payload.Data.States.Should().NotBeEmpty();
            payload.Data.AcademicPeriods.Should().NotBeEmpty();

            studentRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            teacherRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task EvaluationController_HistoryAndObservationUpdate_EnforcesAuthenticationAndAuthorOwnership()
        {
            var storageMock = new Mock<IFileStorageService>();
            using var factory = CreateFactoryWithMockStorage(storageMock);
            using var client = factory.CreateClient();
            var seed = await SeedDocumentAndInscriptionMatrixAsync(factory.Services, storageMock);

            int evaluatorId;
            int evaluationId;

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var idType = context.Set<IdentificationType>().First();
                var program = context.Set<AcademicProgram>().First();

                var evaluator = new User
                {
                    FirstName = "Jurado",
                    LastName = "Principal",
                    Email = $"jurado_{Guid.NewGuid():N}@test.edu",
                    Identification = "888777666",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(evaluator);

                var evalType = new EvaluationType
                {
                    Name = "Sustentacion",
                    Code = $"EVAL_{Guid.NewGuid():N}"[..16],
                    StatusRegister = true
                };
                context.Set<EvaluationType>().Add(evalType);
                await context.SaveChangesAsync();

                var evaluation = new Evaluation
                {
                    EntityType = nameof(InscriptionModality),
                    EntityId = seed.PgInscriptionId,
                    IdEvaluationType = evalType.Id,
                    IdEvaluator = evaluator.Id,
                    Result = "Observations",
                    Observations = "Observacion inicial",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<Evaluation>().Add(evaluation);
                await context.SaveChangesAsync();

                evaluatorId = evaluator.Id;
                evaluationId = evaluation.Id;
            }

            using var anonHistoryReq = new HttpRequestMessage(HttpMethod.Get, $"/api/Evaluation/history/{nameof(InscriptionModality)}/{seed.PgInscriptionId}");
            anonHistoryReq.Headers.Add("X-Test-Anonymous", "true");

            using var authHistoryReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Evaluation/history/{nameof(InscriptionModality)}/{seed.PgInscriptionId}",
                evaluatorId,
                PermissionCodes.ProyectoGrado.N4PGPSG);

            using var otherUserPatchReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/Evaluation/{evaluationId}/observations",
                999,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new UpdateEvaluationObservationDto { Observations = "Edicion ajena" });

            using var authorPatchReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/Evaluation/{evaluationId}/observations",
                evaluatorId,
                PermissionCodes.ProyectoGrado.N4PGPSG,
                new UpdateEvaluationObservationDto { Observations = "Observacion corregida por autor" });

            var anonHistoryRes = await client.SendAsync(anonHistoryReq);
            var authHistoryRes = await client.SendAsync(authHistoryReq);
            var otherUserPatchRes = await client.SendAsync(otherUserPatchReq);
            var authorPatchRes = await client.SendAsync(authorPatchReq);

            anonHistoryRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            authHistoryRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var historyPayload = await authHistoryRes.Content.ReadFromJsonAsync<ApiResponse<List<ObservationHistoryItemDto>>>();
            historyPayload!.Data.Should().ContainSingle(e => e.Id == evaluationId && e.Observations == "Observacion inicial");

            otherUserPatchRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            authorPatchRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedPayload = await authorPatchRes.Content.ReadFromJsonAsync<ApiResponse<ObservationHistoryItemDto>>();
            updatedPayload!.Data!.Id.Should().Be(evaluationId);
            updatedPayload.Data.Observations.Should().Be("Observacion corregida por autor");
            updatedPayload.Data.EvaluatorName.Should().Be("Jurado Principal");
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

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            int userId,
            string permissions,
            object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-UserId", userId.ToString());
            request.Headers.Add("X-Test-Permissions", permissions);
            if (body != null)
                request.Content = JsonContent.Create(body);
            return request;
        }

        private static async Task<MatrixSeedResult> SeedDocumentAndInscriptionMatrixAsync(
            IServiceProvider services,
            Mock<IFileStorageService> storageMock)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var idType = new IdentificationType { Name = "CC", Code = "CC", Description = "Cedula", StatusRegister = true };
            var program = new AcademicProgram { Name = "Ingenieria de Sistemas", Code = "SYS", StatusRegister = true };
            var pgModality = new Modality { Code = ModalityCodes.ProyectoGrado, Name = "Proyecto de Grado", Description = "PG", StatusRegister = true };
            var paModality = new Modality { Code = ModalityCodes.PracticaAcademica, Name = "Practica Academica", Description = "PA", StatusRegister = true };
            var state = new StateInscription { Code = StateInscriptionCodes.Pendiente, Name = "Pendiente", Description = "Pendiente", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2026-1", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6), StatusRegister = true };
            var docClass = new DocumentClass { Name = "Academico", Description = "Academico", StatusRegister = true };

            context.Set<IdentificationType>().Add(idType);
            context.Set<AcademicProgram>().Add(program);
            context.Set<Modality>().AddRange(pgModality, paModality);
            context.Set<StateInscription>().Add(state);
            context.Set<AcademicPeriod>().Add(period);
            context.Set<DocumentClass>().Add(docClass);
            await context.SaveChangesAsync();

            var student101 = await context.Set<User>().FindAsync(101);
            if (student101 == null)
            {
                student101 = new User
                {
                    Id = 101,
                    FirstName = "Estudiante",
                    LastName = "CientoUno",
                    Email = "student101@test.edu",
                    Identification = "101101101",
                    IdIdentificationType = idType.Id,
                    IdAcademicProgram = program.Id,
                    StatusRegister = true
                };
                context.Set<User>().Add(student101);
            }

            var pgPropuestaStage = new StageModality
            {
                IdModality = pgModality.Id,
                Code = StageModalityCodes.PgFasePropuesta,
                Name = "Propuesta",
                StageOrder = 1,
                StatusRegister = true
            };
            var pgInformeStage = new StageModality
            {
                IdModality = pgModality.Id,
                Code = StageModalityCodes.PgFaseProyectoInforme,
                Name = "Informe Proyecto",
                StageOrder = 3,
                StatusRegister = true
            };
            var pgSustentacionStage = new StageModality
            {
                IdModality = pgModality.Id,
                Code = StageModalityCodes.PgFaseSustentacion,
                Name = "Sustentacion",
                StageOrder = 4,
                StatusRegister = true
            };
            var paInscripcionStage = new StageModality
            {
                IdModality = paModality.Id,
                Code = StageModalityCodes.PaFaseInscripcion,
                Name = "Inscripcion PA",
                StageOrder = 1,
                StatusRegister = true
            };

            context.Set<StageModality>().AddRange(pgPropuestaStage, pgInformeStage, pgSustentacionStage, paInscripcionStage);
            await context.SaveChangesAsync();

            var pgInscription = new InscriptionModality
            {
                IdModality = pgModality.Id,
                IdStageModality = pgSustentacionStage.Id,
                IdStateInscription = state.Id,
                IdAcademicPeriod = period.Id,
                StatusRegister = true
            };
            var paInscription = new InscriptionModality
            {
                IdModality = paModality.Id,
                IdStageModality = paInscripcionStage.Id,
                IdStateInscription = state.Id,
                IdAcademicPeriod = period.Id,
                StatusRegister = true
            };
            context.Set<InscriptionModality>().AddRange(pgInscription, paInscription);

            var docTypePropuesta = new DocumentType
            {
                Name = "Propuesta PG",
                Description = "Propuesta PG",
                IdDocumentClass = docClass.Id,
                IdStageModality = pgPropuestaStage.Id,
                StatusRegister = true
            };
            var docTypeInforme = new DocumentType
            {
                Name = "Informe Final PG",
                Description = "Informe Final PG",
                IdDocumentClass = docClass.Id,
                IdStageModality = pgInformeStage.Id,
                StatusRegister = true
            };
            var docTypePa = new DocumentType
            {
                Name = "Carta PA",
                Description = "Carta PA",
                IdDocumentClass = docClass.Id,
                IdStageModality = paInscripcionStage.Id,
                StatusRegister = true
            };
            context.Set<DocumentType>().AddRange(docTypePropuesta, docTypeInforme, docTypePa);
            await context.SaveChangesAsync();

            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdInscriptionModality = pgInscription.Id,
                IdUser = 101,
                StatusRegister = true
            });

            var doc1Stored = $"propuesta_{Guid.NewGuid():N}.pdf";
            var doc2Stored = $"informe_{Guid.NewGuid():N}.pdf";
            var doc3Stored = $"pa_{Guid.NewGuid():N}.pdf";

            var doc1 = new Document
            {
                IdInscriptionModality = pgInscription.Id,
                IdDocumentType = docTypePropuesta.Id,
                Name = "Doc 1 Propuesta",
                OriginalFileName = "propuesta_pg.pdf",
                StoredFileName = doc1Stored,
                StoragePath = "Uploads",
                MimeType = "application/pdf",
                FileSize = 4,
                StatusRegister = true
            };
            var doc2 = new Document
            {
                IdInscriptionModality = pgInscription.Id,
                IdDocumentType = docTypeInforme.Id,
                Name = "Doc 2 Informe",
                OriginalFileName = "informe_pg.pdf",
                StoredFileName = doc2Stored,
                StoragePath = "Uploads",
                MimeType = "application/pdf",
                FileSize = 4,
                StatusRegister = true
            };
            var doc3 = new Document
            {
                IdInscriptionModality = paInscription.Id,
                IdDocumentType = docTypePa.Id,
                Name = "Doc 3 PA",
                OriginalFileName = "carta_pa.pdf",
                StoredFileName = doc3Stored,
                StoragePath = "Uploads",
                MimeType = "application/pdf",
                FileSize = 4,
                StatusRegister = true
            };
            context.Set<Document>().AddRange(doc1, doc2, doc3);
            await context.SaveChangesAsync();

            var doc1Bytes = new byte[] { 10, 20, 30, 40 };
            var doc2Bytes = new byte[] { 50, 60, 70, 80 };
            var doc3Bytes = new byte[] { 90, 91, 92, 93 };

            storageMock.Setup(s => s.GetFileAsync(doc1Stored, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(doc1Bytes));
            storageMock.Setup(s => s.GetFileAsync(doc2Stored, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(doc2Bytes));
            storageMock.Setup(s => s.GetFileAsync(doc3Stored, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(doc3Bytes));

            return new MatrixSeedResult(
                PgInscriptionId: pgInscription.Id,
                PaInscriptionId: paInscription.Id,
                PropuestaStageId: pgPropuestaStage.Id,
                InformeStageId: pgInformeStage.Id,
                SustentacionStageId: pgSustentacionStage.Id,
                PaInscripcionStageId: paInscripcionStage.Id,
                DocTypeInformeId: docTypeInforme.Id,
                Doc1PropuestaId: doc1.Id,
                Doc2InformeId: doc2.Id,
                Doc3PaId: doc3.Id,
                Doc1Bytes: doc1Bytes,
                Doc2Bytes: doc2Bytes,
                Doc3Bytes: doc3Bytes);
        }
    }
}
