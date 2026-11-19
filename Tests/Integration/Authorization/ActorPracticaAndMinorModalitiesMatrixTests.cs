using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicAverages;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.CoTerminals;
using Application.Shared.DTOs.SaberPros;
using Application.Shared.DTOs.ScientificArticles;
using Application.Shared.DTOs.Seminars;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.Authorization
{
    public class ActorPracticaAndMinorModalitiesMatrixTests : IntegrationTestBase
    {
        public ActorPracticaAndMinorModalitiesMatrixTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Theory]
        [InlineData("/api/AcademicPractice/GetAll")]
        [InlineData("/api/AcademicPractice/ByUser/301")]
        [InlineData("/api/CoTerminal/GetAll")]
        [InlineData("/api/CoTerminal/ByUser/301")]
        [InlineData("/api/Seminar/GetAll")]
        [InlineData("/api/Seminar/ByUser/301")]
        [InlineData("/api/ScientificArticle/GetAll")]
        [InlineData("/api/ScientificArticle/ByUser/301")]
        [InlineData("/api/AcademicAverage/GetAll")]
        [InlineData("/api/AcademicAverage/ByUser/301")]
        [InlineData("/api/SaberPro/GetAll")]
        [InlineData("/api/SaberPro/ByUser/301")]
        public async Task AnonymousActor_GetsUnauthorized_OnPracticaAndMinorModalitiesEndpoints(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Test-Anonymous", "true");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AcademicPractice_Student301_CanReadOwnPractice_AndGetsForbiddenOnGetAllAndPhaseApproval()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);

            var studentPerms = string.Join(",",
                PermissionCodes.PracticaAcademica.N3PAF0R,
                PermissionCodes.PracticaAcademica.N3PAF0C,
                PermissionCodes.PracticaAcademica.N3PAF1C,
                PermissionCodes.PracticaAcademica.N3PAF2C);

            using var byUserReq = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByUser/301", studentPerms, userId: "301");
            var byUserResp = await _client.SendAsync(byUserReq);

            byUserResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var byUserBody = await byUserResp.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPracticeWithDetailsResponseDto>>>();
            byUserBody.Should().NotBeNull();
            byUserBody!.Success.Should().BeTrue();
            byUserBody.Data.Should().ContainSingle();

            var practice = byUserBody.Data![0];
            practice.InscriptionModalityId.Should().Be(seed.Practice301Id);
            practice.AcademicPractice.Should().NotBeNull();
            practice.AcademicPractice!.Title.Should().Be("Practica Backend 301");
            practice.AcademicPractice.InstitutionName.Should().Be("Empresa Alpha S.A.S.");
            practice.AcademicPractice.StateStageCode.Should().Be(StateStageCodes.PaInscripcionPendDoc);
            practice.Students.Should().ContainSingle(s => s.Id == 301);

            using var getAllReq = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/GetAll?PageNumber=1&PageSize=10", studentPerms, userId: "301");
            var getAllResp = await _client.SendAsync(getAllReq);
            getAllResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var approvalReq = CreateRequest(HttpMethod.Put, $"/api/AcademicPractice/UpdatePhaseApproval/{seed.Practice301Id}", studentPerms, userId: "301");
            approvalReq.Content = JsonContent.Create(new UpdatePhaseApprovalDto
            {
                Id = seed.Practice301Id,
                NewStateStageId = 1,
                Observations = "Intento no autorizado"
            });
            var approvalResp = await _client.SendAsync(approvalReq);
            approvalResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AcademicPractice_Teachers401And402_ReturnAssignedPractices_AndConfirmedFlagIsPreserved()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);

            // Docente confirmado 401 consulta su propio ByTeacher/401
            using var req401 = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByTeacher/401?PageNumber=1&PageSize=10",
                PermissionCodes.PracticaAcademica.N4PAF0PR, userId: "401");
            var resp401 = await _client.SendAsync(req401);

            resp401.StatusCode.Should().Be(HttpStatusCode.OK);
            var body401 = await resp401.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            body401.Should().NotBeNull();
            body401!.Success.Should().BeTrue();
            body401.Data!.Items.Should().ContainSingle(i => i.InscriptionModalityId == seed.Practice301Id);
            var item401 = body401.Data.Items.First(i => i.InscriptionModalityId == seed.Practice301Id);
            item401.Students.Should().ContainSingle(s => s.Id == 301);
            item401.Teachers.Should().ContainSingle(t => t.IdTeacher == 401 && t.IsConfirmed);

            // Docente no confirmado 402 consulta ByTeacher/402:
            // AcademicPracticeRepository.GetAcademicPracticesByTeacherWithDetailsPaginatedAsync filtra IsConfirmed == true,
            // por lo que la asignación sugerida sin ratificar NO aparece en la bandeja del docente.
            using var req402 = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByTeacher/402?PageNumber=1&PageSize=10",
                PermissionCodes.PracticaAcademica.N4PAF0PR, userId: "402");
            var resp402 = await _client.SendAsync(req402);

            resp402.StatusCode.Should().Be(HttpStatusCode.OK);
            var body402 = await resp402.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            body402.Should().NotBeNull();
            body402!.Success.Should().BeTrue();
            body402.Data!.Items.Should().BeEmpty();

            // Actor sin permiso docente (ej. estudiante con N3PAF0C) intentando /ByTeacher/402 recibe 403 Forbidden
            using var forbiddenTeacherReq = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByTeacher/402?PageNumber=1&PageSize=10",
                PermissionCodes.PracticaAcademica.N3PAF0C, userId: "301");
            var forbiddenTeacherResp = await _client.SendAsync(forbiddenTeacherReq);
            forbiddenTeacherResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AcademicPractice_Coordinator_CanGetAllSeededPractices()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);

            var coordPerms = string.Join(",",
                PermissionCodes.PracticaAcademica.N4PAF0PG,
                PermissionCodes.PracticaAcademica.N4PAF1PG,
                PermissionCodes.PracticaAcademica.N4PAF2PG);

            using var req = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/GetAll?PageNumber=1&PageSize=10", coordPerms, userId: "501");
            var resp = await _client.SendAsync(req);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
            body.Data.Items.Should().Contain(i =>
                i.InscriptionModalityId == seed.Practice301Id
                && i.AcademicPractice != null
                && i.AcademicPractice.Title == "Practica Backend 301");
            body.Data.Items.Should().Contain(i =>
                i.InscriptionModalityId == seed.Practice302Id
                && i.AcademicPractice != null
                && i.AcademicPractice.Title == "Practica Datos 302");
        }

        [Fact]
        public async Task MinorModalities_Student301_CanReadOwnByUser_AndGetsForbiddenOnGetAll()
        {
            using var context = GetFreshDbContext();
            var ids = SeedMinorModalitiesForStudent301(context);

            // 1. CoTerminal
            using (var req = CreateRequest(HttpMethod.Get, "/api/CoTerminal/ByUser/301", PermissionCodes.CoTerminal.N2CTC, "301"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<CoTerminalWithDetailsDto>>>();
                body!.Data.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.CoTerminalId
                    && x.CoTerminal != null
                    && x.CoTerminal.PostgraduateProgramName == "Maestria en Ingenieria");
            }
            using (var req = CreateRequest(HttpMethod.Get, "/api/CoTerminal/GetAll?PageNumber=1&PageSize=10", PermissionCodes.CoTerminal.N2CTC, "301"))
            {
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 2. Seminar
            using (var req = CreateRequest(HttpMethod.Get, "/api/Seminar/ByUser/301", PermissionCodes.Seminario.N2SAC, "301"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<SeminarWithDetailsDto>>>();
                body!.Data.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.SeminarId
                    && x.Seminar != null
                    && x.Seminar.SeminarName == "Seminario Arquitectura Cloud");
            }
            using (var req = CreateRequest(HttpMethod.Get, "/api/Seminar/GetAll?PageNumber=1&PageSize=10", PermissionCodes.Seminario.N2SAC, "301"))
            {
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 3. ScientificArticle
            using (var req = CreateRequest(HttpMethod.Get, "/api/ScientificArticle/ByUser/301", PermissionCodes.PublicacionArticulo.N2PCC, "301"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ScientificArticleWithDetailsDto>>>();
                body!.Data.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.ScientificArticleId
                    && x.ScientificArticle != null
                    && x.ScientificArticle.ArticleTitle == "Optimizacion de Consultas Distribuidas");
            }
            using (var req = CreateRequest(HttpMethod.Get, "/api/ScientificArticle/GetAll?PageNumber=1&PageSize=10", PermissionCodes.PublicacionArticulo.N2PCC, "301"))
            {
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 4. AcademicAverage
            using (var req = CreateRequest(HttpMethod.Get, "/api/AcademicAverage/ByUser/301", PermissionCodes.GradoPromedio.N2GPES, "301"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<AcademicAverageWithDetailsDto>>>();
                body!.Data.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.AcademicAverageId
                    && x.AcademicAverage != null
                    && x.AcademicAverage.CertifiedAverage == 4.65m);
            }
            using (var req = CreateRequest(HttpMethod.Get, "/api/AcademicAverage/GetAll?PageNumber=1&PageSize=10", PermissionCodes.GradoPromedio.N2GPES, "301"))
            {
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 5. SaberPro
            using (var req = CreateRequest(HttpMethod.Get, "/api/SaberPro/ByUser/301", PermissionCodes.SaberPro.N2SPC, "301"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<SaberProWithDetailsDto>>>();
                body!.Data.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.SaberProId
                    && x.SaberPro != null
                    && x.SaberPro.ResultScore == 215);
            }
            using (var req = CreateRequest(HttpMethod.Get, "/api/SaberPro/GetAll?PageNumber=1&PageSize=10", PermissionCodes.SaberPro.N2SPC, "301"))
            {
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task MinorModalities_Managers_CanGetAllOwnModality_AndAreForbiddenAcrossOtherModalities()
        {
            using var context = GetFreshDbContext();
            var ids = SeedMinorModalitiesForStudent301(context);

            // CoTerminal manager (N3CTSGG) -> 200 on CoTerminal/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/CoTerminal/GetAll?PageNumber=1&PageSize=10", PermissionCodes.CoTerminal.N3CTSGG, "601"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<CoTerminalWithDetailsDto>>>();
                body!.Data!.Items.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.CoTerminalId
                    && x.CoTerminal!.PostgraduateProgramName == "Maestria en Ingenieria");
            }

            // Seminar manager (N3SASGG) -> 200 on Seminar/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/Seminar/GetAll?PageNumber=1&PageSize=10", PermissionCodes.Seminario.N3SASGG, "602"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<SeminarWithDetailsDto>>>();
                body!.Data!.Items.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.SeminarId
                    && x.Seminar!.SeminarName == "Seminario Arquitectura Cloud");
            }

            // ScientificArticle reviewer (N3PCSGR) -> 200 on ScientificArticle/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/ScientificArticle/GetAll?PageNumber=1&PageSize=10", PermissionCodes.PublicacionArticulo.N3PCSGR, "603"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ScientificArticleWithDetailsDto>>>();
                body!.Data!.Items.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.ScientificArticleId
                    && x.ScientificArticle!.ArticleTitle == "Optimizacion de Consultas Distribuidas");
            }

            // AcademicAverage manager (N3GPSGG) -> 200 on AcademicAverage/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/AcademicAverage/GetAll?PageNumber=1&PageSize=10", PermissionCodes.GradoPromedio.N3GPSGG, "604"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicAverageWithDetailsDto>>>();
                body!.Data!.Items.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.AcademicAverageId
                    && x.AcademicAverage!.CertifiedAverage == 4.65m);
            }

            // SaberPro manager (N3SPSGG) -> 200 on SaberPro/GetAll
            using (var req = CreateRequest(HttpMethod.Get, "/api/SaberPro/GetAll?PageNumber=1&PageSize=10", PermissionCodes.SaberPro.N3SPSGG, "605"))
            {
                var resp = await _client.SendAsync(req);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<SaberProWithDetailsDto>>>();
                body!.Data!.Items.Should().ContainSingle(x =>
                    x.InscriptionModalityId == ids.SaberProId
                    && x.SaberPro!.ResultScore == 215);
            }

            // Verificación cruzada entre modalidades: N3CTSGG (solo CoTerminal) recibe 403 en las otras 4 modalidades menores
            var crossTargets = new[]
            {
                "/api/Seminar/GetAll?PageNumber=1&PageSize=10",
                "/api/ScientificArticle/GetAll?PageNumber=1&PageSize=10",
                "/api/AcademicAverage/GetAll?PageNumber=1&PageSize=10",
                "/api/SaberPro/GetAll?PageNumber=1&PageSize=10"
            };
            foreach (var url in crossTargets)
            {
                using var crossReq = CreateRequest(HttpMethod.Get, url, PermissionCodes.CoTerminal.N3CTSGG, "601");
                var crossResp = await _client.SendAsync(crossReq);
                crossResp.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"N3CTSGG no debe acceder a {url}");
            }

            // Verificación cruzada inversa: N3SASGG (solo Seminario) recibe 403 en CoTerminal/GetAll
            using var reverseCrossReq = CreateRequest(HttpMethod.Get, "/api/CoTerminal/GetAll?PageNumber=1&PageSize=10",
                PermissionCodes.Seminario.N3SASGG, "602");
            var reverseCrossResp = await _client.SendAsync(reverseCrossReq);
            reverseCrossResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string permissions, string userId)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-Permissions", permissions);
            request.Headers.Add("X-Test-UserId", userId);
            return request;
        }

        private static (int Practice301Id, int Practice302Id) SeedAcademicPracticeScenario(AppDbContext context)
        {
            SeedingUtilities.SeedCatalogs(context);
            var periodId = EnsureBaseUsersAndPeriod(context, 301, 302, 401, 402);

            var cargo = new TypeTeachingAssignment
            {
                Code = "TUTOR_PA",
                Name = "Tutor Practica",
                Description = "Tutor Practica",
                StatusRegister = true,
                OperationRegister = "Seed"
            };
            context.Set<TypeTeachingAssignment>().Add(cargo);
            context.SaveChanges();

            // Inscripción 1: Estudiante 301 + Docente confirmado 401
            var insc301 = new InscriptionModality
            {
                IdModality = 2, // PRACTICA_ACADEMICA
                IdStateInscription = 1,
                IdAcademicPeriod = periodId,
                IdStageModality = 1,
                StatusRegister = true,
                OperationRegister = "Seed"
            };
            context.Set<InscriptionModality>().Add(insc301);
            context.SaveChanges();

            context.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = insc301.Id,
                IdStateStage = 10, // PA_INSCRIPCION_PEND_DOC
                Title = "Practica Backend 301",
                InstitutionName = "Empresa Alpha S.A.S.",
                InstitutionContact = "contacto@alpha.co",
                PracticeStartDate = DateTimeOffset.UtcNow,
                PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(4),
                PracticeHours = 320,
                StatusRegister = true,
                OperationRegister = "Seed"
            });
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdUser = 301,
                IdInscriptionModality = insc301.Id,
                StatusRegister = true,
                OperationRegister = "Seed"
            });
            context.Set<TeachingAssignment>().Add(new TeachingAssignment
            {
                IdInscriptionModality = insc301.Id,
                IdTeacher = 401,
                IdTypeTeachingAssignment = cargo.Id,
                IsConfirmed = true,
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            // Inscripción 2: Estudiante 302 + Docente no confirmado 402
            var insc302 = new InscriptionModality
            {
                IdModality = 2,
                IdStateInscription = 1,
                IdAcademicPeriod = periodId,
                IdStageModality = 1,
                StatusRegister = true,
                OperationRegister = "Seed"
            };
            context.Set<InscriptionModality>().Add(insc302);
            context.SaveChanges();

            context.Set<AcademicPractice>().Add(new AcademicPractice
            {
                Id = insc302.Id,
                IdStateStage = 10,
                Title = "Practica Datos 302",
                InstitutionName = "Empresa Beta S.A.S.",
                InstitutionContact = "contacto@beta.co",
                PracticeStartDate = DateTimeOffset.UtcNow,
                PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(4),
                PracticeHours = 320,
                StatusRegister = true,
                OperationRegister = "Seed"
            });
            context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
            {
                IdUser = 302,
                IdInscriptionModality = insc302.Id,
                StatusRegister = true,
                OperationRegister = "Seed"
            });
            context.Set<TeachingAssignment>().Add(new TeachingAssignment
            {
                IdInscriptionModality = insc302.Id,
                IdTeacher = 402,
                IdTypeTeachingAssignment = cargo.Id,
                IsConfirmed = false,
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            context.SaveChanges();
            return (insc301.Id, insc302.Id);
        }

        private static (int CoTerminalId, int SeminarId, int ScientificArticleId, int AcademicAverageId, int SaberProId) SeedMinorModalitiesForStudent301(AppDbContext context)
        {
            SeedingUtilities.SeedCatalogs(context);
            var periodId = EnsureBaseUsersAndPeriod(context, 301);

            int CreateInscription(int modalityId, int stageModalityId)
            {
                var insc = new InscriptionModality
                {
                    IdModality = modalityId,
                    IdStateInscription = 1,
                    IdAcademicPeriod = periodId,
                    IdStageModality = stageModalityId,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<InscriptionModality>().Add(insc);
                context.SaveChanges();

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdUser = 301,
                    IdInscriptionModality = insc.Id,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                });
                context.SaveChanges();
                return insc.Id;
            }

            var ctId = CreateInscription(3, 7);
            context.Set<CoTerminal>().Add(new CoTerminal
            {
                Id = ctId,
                IdStateStage = 9,
                PostgraduateProgramName = "Maestria en Ingenieria",
                UniversityName = "Universidad Popular del Cesar",
                FirstSemesterAverage = 4.5m,
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            var smId = CreateInscription(4, 10);
            context.Set<Seminar>().Add(new Seminar
            {
                Id = smId,
                IdStateStage = 20,
                SeminarName = "Seminario Arquitectura Cloud",
                AttendancePercentage = 95,
                FinalGrade = 4.7m,
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            var saId = CreateInscription(5, 11);
            context.Set<ScientificArticle>().Add(new ScientificArticle
            {
                Id = saId,
                IdStateStage = 21,
                ArticleTitle = "Optimizacion de Consultas Distribuidas",
                JournalName = "Revista Ingenieria Aplicada",
                AcceptanceDate = DateTimeOffset.UtcNow,
                ISSN = "2027-9999",
                JournalCategory = "A1",
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            var aaId = CreateInscription(6, 13);
            context.Set<AcademicAverage>().Add(new AcademicAverage
            {
                Id = aaId,
                IdStateStage = 24,
                CertifiedAverage = 4.65m,
                HasFailedSubjects = false,
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            var spId = CreateInscription(7, 14);
            context.Set<SaberPro>().Add(new SaberPro
            {
                Id = spId,
                IdStateStage = 25,
                ResultScore = 215,
                ExamDate = DateTimeOffset.UtcNow,
                ResultQuintile = "Q1",
                StatusRegister = true,
                OperationRegister = "Seed"
            });

            context.SaveChanges();
            return (ctId, smId, saId, aaId, spId);
        }

        private static int EnsureBaseUsersAndPeriod(AppDbContext context, params int[] userIds)
        {
            if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
            {
                context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Ingenieria de Sistemas", Code = "IS01", StatusRegister = true, OperationRegister = "Seed" });
            }
            if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
            {
                context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "Cedula", Code = "CC", Description = "CC", StatusRegister = true, OperationRegister = "Seed" });
            }

            var period = context.Set<AcademicPeriod>().FirstOrDefault();
            if (period == null)
            {
                period = new AcademicPeriod
                {
                    Code = "2026-1",
                    StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                    EndDate = DateTimeOffset.UtcNow.AddMonths(5),
                    StatusRegister = true,
                    OperationRegister = "Seed"
                };
                context.Set<AcademicPeriod>().Add(period);
            }

            context.SaveChanges();

            foreach (var uid in userIds)
            {
                if (!context.Set<User>().Any(u => u.Id == uid))
                {
                    context.Set<User>().Add(new User
                    {
                        Id = uid,
                        FirstName = $"User{uid}",
                        LastName = "Test",
                        Email = $"user{uid}@unicesar.edu.co",
                        Identification = $"1000{uid}",
                        IdAcademicProgram = 1,
                        IdIdentificationType = 1,
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    });
                }
            }

            context.SaveChanges();
            return period.Id;
        }

        [Fact]
        public async Task AcademicPractice_UpdatePhaseApproval_ActorAndStateMatrix()
        {
            int practice301Id;
            int practice302Id;
            int f0StageId;
            int f1StageId;
            int f2StageId;
            int paInscripcionEnRevisionId;
            int paInscripcionAprobadaId;
            int paInscripcionRechazadaId;
            int paDesarrolloPendDocId;
            int paDesarrolloEnRevisionId;
            int paDesarrolloAprobadaId;
            int paDesarrolloRechazadaId;
            int paEvaluacionPendDocId;
            int paInformeEnRevisionId;
            int paAprobadoId;
            int paEvaluacionRechazadaId;

            using (var context = GetFreshDbContext())
            {
                var seed = SeedAcademicPracticeScenario(context);
                practice301Id = seed.Practice301Id;
                practice302Id = seed.Practice302Id;
                EnsureBaseUsersAndPeriod(context, 405, 501);

                var stageRepo = context.Set<StageModality>();
                f0StageId = stageRepo.First(s => s.Code == StageModalityCodes.PaFaseInscripcion).Id;
                f1StageId = stageRepo.First(s => s.Code == StageModalityCodes.PaFaseDesarrollo).Id;
                f2StageId = stageRepo.First(s => s.Code == StageModalityCodes.PaFaseEvaluacion).Id;

                var stateRepo = context.Set<StateStage>();

                StateStage EnsurePaState(string code, string name, int stageId, bool isFinalStage = false, bool isFinalOverall = false, bool isRejection = false)
                {
                    var existing = stateRepo.FirstOrDefault(s => s.Code == code);
                    if (existing != null) return existing;

                    var state = new StateStage
                    {
                        Code = code,
                        Name = name,
                        IdStageModality = stageId,
                        IsFinalStateForStage = isFinalStage,
                        IsFinalStateForModalityOverall = isFinalOverall,
                        IsRejectionState = isRejection,
                        StatusRegister = true,
                        OperationRegister = "Seed"
                    };
                    stateRepo.Add(state);
                    return state;
                }

                paInscripcionAprobadaId = stateRepo.First(s => s.Code == StateStageCodes.PaInscripcionAprobada).Id;
                paInscripcionRechazadaId = stateRepo.First(s => s.Code == StateStageCodes.PaInscripcionRechazada).Id;
                paDesarrolloAprobadaId = stateRepo.First(s => s.Code == StateStageCodes.PaDesarrolloAprobada).Id;

                var paInscripcionEnRevision = EnsurePaState(StateStageCodes.PaInscripcionEnRevision, "Inscripcion En Revision", f0StageId);
                var paDesarrolloPendDoc = EnsurePaState("PA_DESARROLLO_PEND_DOC", "Desarrollo Pendiente Documentos", f1StageId);
                var paDesarrolloEnRevision = EnsurePaState("PA_DESARROLLO_EN_REVISION", "Desarrollo En Revision", f1StageId);
                var paDesarrolloRechazada = EnsurePaState("PA_DESARROLLO_RECHAZADA", "Desarrollo Rechazada", f1StageId, isFinalStage: true, isFinalOverall: true, isRejection: true);
                var paEvaluacionPendDoc = EnsurePaState("PA_EVALUACION_PEND_DOC", "Evaluacion Pendiente Documentos", f2StageId);
                var paInformeEnRevision = EnsurePaState(StateStageCodes.PaInformeFinalEnRevision, "Informe Final En Revision", f2StageId);
                var paAprobado = EnsurePaState(StateStageCodes.PaAprobado, "Practica Aprobada", f2StageId, isFinalStage: true, isFinalOverall: true);
                var paEvaluacionRechazada = EnsurePaState("PA_EVALUACION_RECHAZADA", "Evaluacion Rechazada", f2StageId, isFinalStage: true, isFinalOverall: true, isRejection: true);

                context.SaveChanges();
                paInscripcionEnRevisionId = paInscripcionEnRevision.Id;
                paDesarrolloPendDocId = paDesarrolloPendDoc.Id;
                paDesarrolloEnRevisionId = paDesarrolloEnRevision.Id;
                paDesarrolloRechazadaId = paDesarrolloRechazada.Id;
                paEvaluacionPendDocId = paEvaluacionPendDoc.Id;
                paInformeEnRevisionId = paInformeEnRevision.Id;
                paAprobadoId = paAprobado.Id;
                paEvaluacionRechazadaId = paEvaluacionRechazada.Id;
            }

            async Task<HttpResponseMessage> SendApprovalAsync(int practiceId, string userId, string permissions, int targetStateId, string obs = "Matrix obs")
            {
                using var req = CreateRequest(HttpMethod.Put, $"/api/AcademicPractice/UpdatePhaseApproval/{practiceId}", permissions, userId);
                req.Content = JsonContent.Create(new UpdatePhaseApprovalDto
                {
                    Id = practiceId,
                    NewStateStageId = targetStateId,
                    Observations = obs,
                    EvaluatorObservations = obs
                });
                return await _client.SendAsync(req);
            }

            async Task SetPracticeStageAndStateAsync(int practiceId, int stageId, int stateStageId)
            {
                using var scope = _factory.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ap = await ctx.Set<AcademicPractice>().FindAsync(practiceId);
                ap!.IdStateStage = stateStageId;
                var insc = await ctx.Set<InscriptionModality>().FindAsync(practiceId);
                insc!.IdStageModality = stageId;
                insc.IsFinalRejection = false;
                await ctx.SaveChangesAsync();
            }

            var committeeDirectorPerms = string.Join(",",
                PermissionCodes.PracticaAcademica.N4PAF0PG,
                PermissionCodes.PracticaAcademica.N4PAF0PA,
                PermissionCodes.PracticaAcademica.N4PAF1PG,
                PermissionCodes.PracticaAcademica.N4PAF2PG);

            try
            {
                // ── FASE 0 (PA_FASE_INSCRIPCION) ───────────────────────────────────────────
                // 1. OtherStudent (405) no vinculado a practice301 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "405", PermissionCodes.PracticaAcademica.N3PAF1R, paInscripcionEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 2. OwnerStudent (301) con N3PAF0R en UpdatePhaseApproval -> 403 Forbidden (en F0 el estudiante radica vía UpdateInstitutionInfo)
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF0R, paInscripcionEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF0R, paInscripcionAprobadaId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF0R, paInscripcionRechazadaId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 3. ProposedTeacher (402, IsConfirmed = false en practice302) con N4PAF0PR -> 403 Forbidden
                (await SendApprovalAsync(practice302Id, "402", PermissionCodes.PracticaAcademica.N4PAF0PR, paInscripcionEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 4. Cross-phase negativo en F0: N4PAF1PR intentando modificar F0 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF1PR, paInscripcionEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 5. Cross-phase negativo: COMMITTEE_DIRECTOR con F0+F1+F2 intentando salto directo F0 -> F2 -> 400 BadRequest
                var directJumpRes = await SendApprovalAsync(practice301Id, "501", committeeDirectorPerms, paInformeEnRevisionId, "Salto directo prohibido");
                directJumpRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                // 6. F0 Evaluador confirmado (401 con N4PAF0PR) -> 200 OK
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF0PR, paInscripcionEnRevisionId, "Revisión F0 docente"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                // 7. F0 Gestores (N4PAF0PG y N4PAF0PA) -> 200 OK
                (await SendApprovalAsync(practice301Id, "501", PermissionCodes.PracticaAcademica.N4PAF0PG, paInscripcionEnRevisionId, "Gestión F0 N4PAF0PG"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);
                (await SendApprovalAsync(practice301Id, "501", PermissionCodes.PracticaAcademica.N4PAF0PA, paInscripcionAprobadaId, "Aval F0 N4PAF0PA"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                using (var checkScope = _factory.Services.CreateScope())
                {
                    var checkContext = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    checkContext.Set<AcademicPractice>().Find(practice301Id)!.IdStateStage.Should().Be(paInscripcionAprobadaId);
                }

                // ── FASE 1 (PA_FASE_DESARROLLO) ────────────────────────────────────────────
                await SetPracticeStageAndStateAsync(practice301Id, f1StageId, paDesarrolloPendDocId);

                // 1. Cross-phase negativo: N4PAF0PR intentando modificar F1 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF0PR, paDesarrolloEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 2. Cross-phase negativo: N3PAF1R intentando enviar estado de F2 (PA_INFORME_FINAL_EN_REVISION) -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF1R, paInformeEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 3. OwnerStudent (301 con N3PAF1R) intentando final o rechazo en F1 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF1R, paDesarrolloAprobadaId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF1R, paDesarrolloRechazadaId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 4. OwnerStudent (301 con N3PAF1R) avanzando a PA_DESARROLLO_EN_REVISION -> 200 OK
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF1R, paDesarrolloEnRevisionId, "Radico avance F1"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                // 5. Evaluador confirmado (401 con N4PAF1PR) en F1 -> 200 OK
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF1PR, paDesarrolloEnRevisionId, "Revisión tutor F1"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                // 6. Gestor F1 (501 con N4PAF1PG) aprobando F1 -> 200 OK
                (await SendApprovalAsync(practice301Id, "501", PermissionCodes.PracticaAcademica.N4PAF1PG, paDesarrolloAprobadaId, "Aprobación F1"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                using (var checkScope = _factory.Services.CreateScope())
                {
                    var checkContext = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    checkContext.Set<AcademicPractice>().Find(practice301Id)!.IdStateStage.Should().Be(paDesarrolloAprobadaId);
                }

                // ── FASE 2 (PA_FASE_EVALUACION) ────────────────────────────────────────────
                await SetPracticeStageAndStateAsync(practice301Id, f2StageId, paEvaluacionPendDocId);

                // 1. Cross-phase negativo: N4PAF0PR y N4PAF1PR intentando modificar F2 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF0PR, paInformeEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF1PR, paInformeEnRevisionId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 2. OwnerStudent (301 con N3PAF2R) intentando final o rechazo en F2 -> 403 Forbidden
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF2R, paAprobadoId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF2R, paEvaluacionRechazadaId))
                    .StatusCode.Should().Be(HttpStatusCode.Forbidden);

                // 3. OwnerStudent (301 con N3PAF2R) avanzando a PA_INFORME_FINAL_EN_REVISION -> 200 OK
                (await SendApprovalAsync(practice301Id, "301", PermissionCodes.PracticaAcademica.N3PAF2R, paInformeEnRevisionId, "Radico informe final F2"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                // 4. Evaluador confirmado (401 con N4PAF2PR) en F2 -> 200 OK
                (await SendApprovalAsync(practice301Id, "401", PermissionCodes.PracticaAcademica.N4PAF2PR, paInformeEnRevisionId, "Revisión jurado F2"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                // 5. Gestor F2 (501 con N4PAF2PG) aprobando F2 -> 200 OK
                (await SendApprovalAsync(practice301Id, "501", PermissionCodes.PracticaAcademica.N4PAF2PG, paAprobadoId, "Aprobación final F2"))
                    .StatusCode.Should().Be(HttpStatusCode.OK);

                using (var checkScope = _factory.Services.CreateScope())
                {
                    var checkContext = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    checkContext.Set<AcademicPractice>().Find(practice301Id)!.IdStateStage.Should().Be(paAprobadoId);
                }
            }
            finally
            {
                using var restoreScope = _factory.Services.CreateScope();
                var restoreContext = restoreScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var practice = await restoreContext.Set<AcademicPractice>().FindAsync(practice301Id);
                if (practice != null)
                {
                    practice.IdStateStage = 10;
                    practice.FinalReportApprovalDate = null;
                    practice.FinalApprovalDate = null;
                    practice.AvalApprovalDate = null;
                    practice.PlanApprovalDate = null;
                    practice.DevelopmentCompletionDate = null;
                }
                var insc = await restoreContext.Set<InscriptionModality>().FindAsync(practice301Id);
                if (insc != null)
                {
                    insc.IdStageModality = 1;
                    insc.IsFinalRejection = false;
                }
                await restoreContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task AcademicPractice_UpdateInstitutionInfo_And_PhaseApproval_EnforceOwnershipAndCoordinatorRoles()
        {
            int practiceId = 0;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedingUtilities.SeedCatalogs(context);

                var period = context.Set<AcademicPeriod>().FirstOrDefault()
                    ?? new AcademicPeriod { Code = $"PER_{Guid.NewGuid():N}"[..10], StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6), StatusRegister = true };
                if (period.Id == 0)
                {
                    context.Set<AcademicPeriod>().Add(period);
                    await context.SaveChangesAsync();
                }

                var insc = new InscriptionModality
                {
                    IdModality = 2,
                    IdStageModality = 1,
                    IdStateInscription = 1,
                    IdAcademicPeriod = period.Id,
                    Observations = "Práctica aislada matrix",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<InscriptionModality>().Add(insc);
                await context.SaveChangesAsync();
                practiceId = insc.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = practiceId,
                    IdUser = 301,
                    StatusRegister = true
                });
                context.Set<AcademicPractice>().Add(new AcademicPractice
                {
                    Id = practiceId,
                    Title = "Práctica Original",
                    InstitutionName = "Empresa Inicial",
                    InstitutionContact = "init@empresa.com",
                    PracticeStartDate = DateTimeOffset.UtcNow.AddDays(1),
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(4),
                    PracticeHours = 300,
                    IdStateStage = 10,
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync();
            }

            try
            {
                var validDto = new Application.Shared.DTOs.AcademicPractices.UpdateInstitutionInfoDto
                {
                    Id = practiceId,
                    Title = "Práctica Validada",
                    InstitutionName = "Empresa Validada SAS",
                    InstitutionContact = "contacto@empresa.com",
                    PracticeStartDate = DateTimeOffset.UtcNow.AddDays(2),
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(5),
                    PracticeHours = 360,
                    Observations = "Actualización estudiante",
                    NewStateStageId = null
                };

                // 1. Estudiante no miembro (302) -> 403 Forbidden
                using (var alienReq = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdateInstitutionInfo/{practiceId}",
                    PermissionCodes.PracticaAcademica.N3PAF0R,
                    userId: "302"))
                {
                    alienReq.Content = JsonContent.Create(validDto);
                    var res = await _client.SendAsync(alienReq);
                    res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                }

                // 2. Estudiante dueño (301) intenta mover a estado final (1 = PaInscripcionAprobada) -> 403 Forbidden
                using (var selfApproveReq = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdateInstitutionInfo/{practiceId}",
                    PermissionCodes.PracticaAcademica.N3PAF0R,
                    userId: "301"))
                {
                    selfApproveReq.Content = JsonContent.Create(validDto with { NewStateStageId = 1 });
                    var res = await _client.SendAsync(selfApproveReq);
                    res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                }

                // 3. Estudiante dueño (301) actualiza info institucional sin auto-aprobar -> 200 OK y BD persistida
                using (var ownerReq = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdateInstitutionInfo/{practiceId}",
                    PermissionCodes.PracticaAcademica.N3PAF0R,
                    userId: "301"))
                {
                    ownerReq.Content = JsonContent.Create(validDto);
                    var res = await _client.SendAsync(ownerReq);
                    res.StatusCode.Should().Be(HttpStatusCode.OK);
                }

                using (var verifyScope = _factory.Services.CreateScope())
                {
                    var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var updated = await verifyContext.Set<AcademicPractice>().FindAsync(practiceId);
                    updated!.InstitutionName.Should().Be("Empresa Validada SAS");
                    updated.IdStateStage.Should().Be(10);
                }

                // 4. Coordinador (305 con N4PAF0PA) aprueba fase -> 200 OK + BD + job HandleAcademicPracticeChangeAsync
                _factory.EnqueuedJobs.Clear();
                using (var coordReq = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdatePhaseApproval/{practiceId}",
                    PermissionCodes.PracticaAcademica.N4PAF0PA,
                    userId: "305"))
                {
                    coordReq.Content = JsonContent.Create(new Application.Shared.DTOs.AcademicPractices.UpdatePhaseApprovalDto
                    {
                        Id = practiceId,
                        NewStateStageId = 1,
                        Observations = "Aprobado por coordinador"
                    });
                    var res = await _client.SendAsync(coordReq);
                    res.StatusCode.Should().Be(HttpStatusCode.OK);
                }

                using (var finalVerifyScope = _factory.Services.CreateScope())
                {
                    var verifyContext = finalVerifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    verifyContext.Set<AcademicPractice>().Find(practiceId)!.IdStateStage.Should().Be(1);
                }
                _factory.EnqueuedJobs.Should().Contain(j => j.MethodName == "HandleAcademicPracticeChangeAsync");
            }
            finally
            {
                using var cleanupScope = _factory.Services.CreateScope();
                var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var uims = cleanupContext.Set<UserInscriptionModality>().Where(x => x.IdInscriptionModality == practiceId).ToList();
                if (uims.Count > 0) cleanupContext.Set<UserInscriptionModality>().RemoveRange(uims);
                var practice = await cleanupContext.Set<AcademicPractice>().FindAsync(practiceId);
                if (practice != null) cleanupContext.Set<AcademicPractice>().Remove(practice);
                var insc = await cleanupContext.Set<InscriptionModality>().FindAsync(practiceId);
                if (insc != null) cleanupContext.Set<InscriptionModality>().Remove(insc);
                await cleanupContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task MinorModalities_Patch_EnforcesOwnership_AntiSelfApproval_AndCommitteeApproval()
        {
            (int CoTerminalId, int SeminarId, int ScientificArticleId, int AcademicAverageId, int SaberProId) minorIds;
            var tempStateIds = new List<int>();
            int saFinalId;
            int gpFinalId;
            int spFinalId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedingUtilities.SeedCatalogs(context);
                minorIds = SeedMinorModalitiesForStudent301(context);

                async Task<int> EnsureFinalStateAsync(int stageModalityId, string code, string name)
                {
                    var existing = context.Set<StateStage>().FirstOrDefault(s => s.Code == code);
                    if (existing != null) return existing.Id;
                    var st = new StateStage
                    {
                        Code = code,
                        Name = name,
                        IdStageModality = stageModalityId,
                        IsInitialState = false,
                        IsFinalStateForStage = true,
                        IsFinalStateForModalityOverall = true,
                        IsRejectionState = false,
                        StatusRegister = true,
                        OperationRegister = "TestSeed"
                    };
                    context.Set<StateStage>().Add(st);
                    await context.SaveChangesAsync();
                    tempStateIds.Add(st.Id);
                    return st.Id;
                }

                saFinalId = await EnsureFinalStateAsync(10, "SA_APROBADO_TEST", "Seminario Aprobado Test");
                gpFinalId = await EnsureFinalStateAsync(13, "GP_APROBADO_TEST", "Grado Promedio Aprobado Test");
                spFinalId = await EnsureFinalStateAsync(14, "SP_APROBADO_TEST", "Saber Pro Aprobado Test");
            }

            try
            {
                var cases = new (string Url, string StudentPerm, string ReviewerPerm, int FinalStateId, Func<int, object> BuildBody, string ExpectedJob)[]
                {
                    (
                        $"/api/CoTerminal/{minorIds.CoTerminalId}",
                        PermissionCodes.CoTerminal.N2CTR,
                        $"{PermissionCodes.CoTerminal.N3CTSGR},{PermissionCodes.CoTerminal.N3CTSGG}",
                        32,
                        st => new Application.Shared.DTOs.CoTerminals.CoTerminalPatchDto { IdStateStage = st, Observations = "Revisión CoTerminal" },
                        "HandleCoTerminalChangeAsync"
                    ),
                    (
                        $"/api/Seminar/{minorIds.SeminarId}",
                        PermissionCodes.Seminario.N2SAR,
                        $"{PermissionCodes.Seminario.N3SASGR},{PermissionCodes.Seminario.N3SASGG}",
                        saFinalId,
                        st => new Application.Shared.DTOs.Seminars.SeminarPatchDto { IdStateStage = st, Observations = "Revisión Seminario" },
                        "HandleSeminarChangeAsync"
                    ),
                    (
                        $"/api/ScientificArticle/{minorIds.ScientificArticleId}",
                        PermissionCodes.PublicacionArticulo.N2PCR,
                        $"{PermissionCodes.PublicacionArticulo.N3PCSGR},{PermissionCodes.PublicacionArticulo.N3PCSGG}",
                        22,
                        st => new Application.Shared.DTOs.ScientificArticles.ScientificArticlePatchDto { IdStateStage = st, Observations = "Revisión Artículo" },
                        "HandleScientificArticleChangeAsync"
                    ),
                    (
                        $"/api/AcademicAverage/{minorIds.AcademicAverageId}",
                        PermissionCodes.GradoPromedio.N2GPR,
                        $"{PermissionCodes.GradoPromedio.N3GPSGR},{PermissionCodes.GradoPromedio.N3GPSGG}",
                        gpFinalId,
                        st => new Application.Shared.DTOs.AcademicAverages.AcademicAveragePatchDto { IdStateStage = st, Observations = "Revisión Promedio" },
                        "HandleAcademicAverageChangeAsync"
                    ),
                    (
                        $"/api/SaberPro/{minorIds.SaberProId}",
                        PermissionCodes.SaberPro.N2SPR,
                        $"{PermissionCodes.SaberPro.N3SPSGR},{PermissionCodes.SaberPro.N3SPSGG}",
                        spFinalId,
                        st => new Application.Shared.DTOs.SaberPros.SaberProPatchDto { IdStateStage = st, Observations = "Revisión Saber Pro" },
                        "HandleSaberProChangeAsync"
                    )
                };

                foreach (var c in cases)
                {
                    // 1. Estudiante ajeno (302) -> 403 Forbidden
                    using (var alienReq = CreateRequest(HttpMethod.Patch, c.Url, c.StudentPerm, userId: "302"))
                    {
                        alienReq.Content = JsonContent.Create(c.BuildBody(c.FinalStateId));
                        var alienRes = await _client.SendAsync(alienReq);
                        alienRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"alien student on {c.Url}");
                    }

                    // 2. Estudiante dueño (301) intenta auto-aprobar estado final -> 403 Forbidden
                    using (var selfReq = CreateRequest(HttpMethod.Patch, c.Url, c.StudentPerm, userId: "301"))
                    {
                        selfReq.Content = JsonContent.Create(c.BuildBody(c.FinalStateId));
                        var selfRes = await _client.SendAsync(selfReq);
                        selfRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"self-approval on {c.Url}");
                    }

                    // 3. Comité/Revisor (305) aprueba estado final -> 200 OK y encola job de notificación
                    _factory.EnqueuedJobs.Clear();
                    using (var reviewerReq = CreateRequest(HttpMethod.Patch, c.Url, c.ReviewerPerm, userId: "305"))
                    {
                        reviewerReq.Content = JsonContent.Create(c.BuildBody(c.FinalStateId));
                        var reviewerRes = await _client.SendAsync(reviewerReq);
                        reviewerRes.StatusCode.Should().Be(HttpStatusCode.OK, $"reviewer patch on {c.Url}");
                    }

                    _factory.EnqueuedJobs.Should().Contain(j => j.MethodName == c.ExpectedJob, $"notification job for {c.Url}");
                }
            }
            finally
            {
                using var cleanupScope = _factory.Services.CreateScope();
                var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                foreach (var stId in tempStateIds)
                {
                    var st = await cleanupContext.Set<StateStage>().FindAsync(stId);
                    if (st != null) cleanupContext.Set<StateStage>().Remove(st);
                }
                await cleanupContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task AcademicPractice_UnlinkedN4PAF0PR_WithoutManagement_GetsForbiddenEvenInPendDoc_AndInactiveByUserExcluded()
        {
            int practiceId = 0;
            using (var context = GetFreshDbContext())
            {
                var seed = SeedAcademicPracticeScenario(context);
                practiceId = seed.Practice301Id;
                EnsureBaseUsersAndPeriod(context, 499);
            }

            // 1. Docente no vinculado (499) con solo N4PAF0PR (sin N4PAF0PG/N4PAF0PA) en PA_INSCRIPCION_PEND_DOC -> 403 Forbidden
            using (var req = CreateRequest(HttpMethod.Put, $"/api/AcademicPractice/UpdatePhaseApproval/{practiceId}", PermissionCodes.PracticaAcademica.N4PAF0PR, "499"))
            {
                req.Content = JsonContent.Create(new UpdatePhaseApprovalDto
                {
                    Id = practiceId,
                    NewStateStageId = 11,
                    Observations = "Bypass sin asignacion"
                });
                var res = await _client.SendAsync(req);
                res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 2. Anti-IDOR en WithDetails y ByUser cruzado para otro estudiante
            using (var crossByUser = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByUser/301", PermissionCodes.PracticaAcademica.N3PAF0C, "405"))
            {
                (await _client.SendAsync(crossByUser)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
            using (var unlinkedDetails = CreateRequest(HttpMethod.Get, $"/api/AcademicPractice/WithDetails/{practiceId}", PermissionCodes.PracticaAcademica.N3PAF0C, "405"))
            {
                (await _client.SendAsync(unlinkedDetails)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 3. Desactivar UserInscriptionModality excluye practica de ByUser/{id}
            try
            {
                using (var deactivateScope = _factory.Services.CreateScope())
                {
                    var deactivateContext = deactivateScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var uim = deactivateContext.Set<UserInscriptionModality>().First(x => x.IdInscriptionModality == practiceId && x.IdUser == 301);
                    uim.StatusRegister = false;
                    await deactivateContext.SaveChangesAsync();
                }

                using var ownByUser = CreateRequest(HttpMethod.Get, "/api/AcademicPractice/ByUser/301", PermissionCodes.PracticaAcademica.N3PAF0C, "301");
                var resp = await _client.SendAsync(ownByUser);
                resp.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPracticeWithDetailsResponseDto>>>();
                body!.Data!.Should().NotContain(x => x.AcademicPractice!.Id == practiceId);
            }
            finally
            {
                using var restoreScope = _factory.Services.CreateScope();
                var restoreContext = restoreScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var uim = restoreContext.Set<UserInscriptionModality>().FirstOrDefault(x => x.IdInscriptionModality == practiceId && x.IdUser == 301);
                if (uim != null)
                {
                    uim.StatusRegister = true;
                    await restoreContext.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task MinorModalities_ReviewerWithOnlySGR_CanReadOwnWithDetails_AndGetsForbiddenOnOtherModality()
        {
            using var context = GetFreshDbContext();
            var ids = SeedMinorModalitiesForStudent301(context);

            var cases = new[]
            {
                (Url: $"/api/CoTerminal/WithDetails/{ids.CoTerminalId}", OwnPerm: PermissionCodes.CoTerminal.N3CTSGR, OtherPerm: PermissionCodes.Seminario.N3SASGR),
                (Url: $"/api/Seminar/WithDetails/{ids.SeminarId}", OwnPerm: PermissionCodes.Seminario.N3SASGR, OtherPerm: PermissionCodes.CoTerminal.N3CTSGR),
                (Url: $"/api/ScientificArticle/WithDetails/{ids.ScientificArticleId}", OwnPerm: PermissionCodes.PublicacionArticulo.N3PCSGR, OtherPerm: PermissionCodes.CoTerminal.N3CTSGR),
                (Url: $"/api/AcademicAverage/WithDetails/{ids.AcademicAverageId}", OwnPerm: PermissionCodes.GradoPromedio.N3GPSGR, OtherPerm: PermissionCodes.CoTerminal.N3CTSGR),
                (Url: $"/api/SaberPro/WithDetails/{ids.SaberProId}", OwnPerm: PermissionCodes.SaberPro.N3SPSGR, OtherPerm: PermissionCodes.CoTerminal.N3CTSGR),
            };

            foreach (var c in cases)
            {
                using var ownReq = CreateRequest(HttpMethod.Get, c.Url, c.OwnPerm, userId: "650");
                var ownResp = await _client.SendAsync(ownReq);
                ownResp.StatusCode.Should().Be(HttpStatusCode.OK, $"reviewer with only {c.OwnPerm} should access {c.Url}");

                using var crossReq = CreateRequest(HttpMethod.Get, c.Url, c.OtherPerm, userId: "651");
                var crossResp = await _client.SendAsync(crossReq);
                crossResp.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"reviewer with {c.OtherPerm} must be forbidden on {c.Url}");
            }
        }

        [Fact]
        public async Task AcademicPractice_UpdateInstitutionInfo_WithoutStateChange_AllowsOnlyEditablePhase0StatesForStudent()
        {
            int practiceId = 0;
            int statePendDocId;
            int stateEnRevisionId = 0;
            int stateObservacionesId = 0;
            int stateInscripcionAprobadaId;
            int stateInscripcionRechazadaId;
            int stateDesarrolloPendId = 0;
            int stateInformeEnRevisionId = 0;
            int stateAprobadoId = 0;
            var createdStateIds = new List<int>();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                SeedingUtilities.SeedCatalogs(context);

                var period = context.Set<AcademicPeriod>().FirstOrDefault()
                    ?? new AcademicPeriod { Code = $"P6C_{Guid.NewGuid():N}"[..10], StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(6), StatusRegister = true };
                if (period.Id == 0)
                {
                    context.Set<AcademicPeriod>().Add(period);
                    await context.SaveChangesAsync();
                }

                StateStage EnsureStageState(string code, string name, int stageId, bool isFinalStage = false, bool isFinalOverall = false, bool isRejection = false)
                {
                    var existing = context.Set<StateStage>().FirstOrDefault(s => s.Code == code);
                    if (existing != null)
                    {
                        // Si otro test previo dejo PA_INSCRIPCION_OBSERVACIONES con IsRejectionState=true, usamos una instancia limpia aislada para este escenario.
                        if (code == StateStageCodes.PaInscripcionObservaciones && existing.IsRejectionState != isRejection)
                        {
                            var isolated = new StateStage
                            {
                                Code = code,
                                Name = name,
                                IdStageModality = stageId,
                                IsFinalStateForStage = isFinalStage,
                                IsFinalStateForModalityOverall = isFinalOverall,
                                IsRejectionState = isRejection,
                                StatusRegister = true,
                                OperationRegister = "TestF6c"
                            };
                            context.Set<StateStage>().Add(isolated);
                            context.SaveChanges();
                            createdStateIds.Add(isolated.Id);
                            return isolated;
                        }
                        return existing;
                    }

                    var created = new StateStage
                    {
                        Code = code,
                        Name = name,
                        IdStageModality = stageId,
                        IsFinalStateForStage = isFinalStage,
                        IsFinalStateForModalityOverall = isFinalOverall,
                        IsRejectionState = isRejection,
                        StatusRegister = true,
                        OperationRegister = "TestF6c"
                    };
                    context.Set<StateStage>().Add(created);
                    context.SaveChanges();
                    createdStateIds.Add(created.Id);
                    return created;
                }

                statePendDocId = 10; // PA_INSCRIPCION_PEND_DOC (Stage 1)
                stateInscripcionAprobadaId = 1; // PA_INSCRIPCION_APROBADA (Stage 1, IsFinalStateForStage = true)
                stateInscripcionRechazadaId = 16; // PA_INSCRIPCION_RECHAZADA (Stage 1, IsRejectionState = true)
                stateEnRevisionId = EnsureStageState(StateStageCodes.PaInscripcionEnRevision, "En Revision F0", 1).Id;
                stateObservacionesId = EnsureStageState(StateStageCodes.PaInscripcionObservaciones, "Con Observaciones F0", 1).Id;
                stateDesarrolloPendId = EnsureStageState("PA_DESARROLLO_PEND_DOC", "Desarrollo Pend Doc", 2).Id;
                stateInformeEnRevisionId = EnsureStageState(StateStageCodes.PaInformeFinalEnRevision, "Informe Final En Revision", 3).Id;
                stateAprobadoId = EnsureStageState(StateStageCodes.PaAprobado, "Practica Aprobada", 3, isFinalStage: true, isFinalOverall: true).Id;

                var insc = new InscriptionModality
                {
                    IdModality = 2,
                    IdStageModality = 1,
                    IdStateInscription = 1,
                    IdAcademicPeriod = period.Id,
                    Observations = "Práctica F6.c",
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<InscriptionModality>().Add(insc);
                await context.SaveChangesAsync();
                practiceId = insc.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = practiceId,
                    IdUser = 301,
                    StatusRegister = true
                });
                context.Set<AcademicPractice>().Add(new AcademicPractice
                {
                    Id = practiceId,
                    Title = "Práctica F6c",
                    InstitutionName = "Empresa F6c",
                    InstitutionContact = "f6c@empresa.com",
                    PracticeStartDate = DateTimeOffset.UtcNow.AddDays(1),
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(4),
                    PracticeHours = 320,
                    IdStateStage = statePendDocId,
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync();
            }

            async Task<HttpResponseMessage> SendStudentInstitutionUpdateAsync(string titleSuffix)
            {
                using var req = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdateInstitutionInfo/{practiceId}",
                    PermissionCodes.PracticaAcademica.N3PAF0R,
                    userId: "301");
                req.Content = JsonContent.Create(new Application.Shared.DTOs.AcademicPractices.UpdateInstitutionInfoDto
                {
                    Id = practiceId,
                    Title = $"Práctica F6c {titleSuffix}",
                    InstitutionName = $"Empresa {titleSuffix}",
                    InstitutionContact = "f6c@empresa.com",
                    PracticeStartDate = DateTimeOffset.UtcNow.AddDays(2),
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(5),
                    PracticeHours = 360,
                    NewStateStageId = null
                });
                return await _client.SendAsync(req);
            }

            async Task SetPracticeStateAsync(int stateStageId, bool isFinalRejection = false)
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ap = await context.Set<AcademicPractice>().FindAsync(practiceId);
                ap!.IdStateStage = stateStageId;
                var insc = await context.Set<InscriptionModality>().FindAsync(practiceId);
                insc!.IsFinalRejection = isFinalRejection;
                await context.SaveChangesAsync();
            }

            try
            {
                // 1. Estados editables de Fase 0 -> 200 OK
                (await SendStudentInstitutionUpdateAsync("PendDoc")).StatusCode.Should().Be(HttpStatusCode.OK);

                await SetPracticeStateAsync(stateObservacionesId);
                (await SendStudentInstitutionUpdateAsync("Observaciones")).StatusCode.Should().Be(HttpStatusCode.OK);

                // 2. Estado en revision de Fase 0, aprobado de Fase 0, rechazado, Fase 1, Fase 2 y aprobado final -> 400 BadRequest
                foreach (var blockedStateId in new[]
                {
                    stateEnRevisionId,
                    stateInscripcionAprobadaId,
                    stateInscripcionRechazadaId,
                    stateDesarrolloPendId,
                    stateInformeEnRevisionId,
                    stateAprobadoId
                })
                {
                    await SetPracticeStateAsync(blockedStateId);
                    var resp = await SendStudentInstitutionUpdateAsync($"Blocked_{blockedStateId}");
                    resp.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"student must not edit institution info when stateId={blockedStateId}");
                }

                // 3. Incluso en estado PendDoc, si la inscripción tiene IsFinalRejection = true -> 400 BadRequest
                await SetPracticeStateAsync(statePendDocId, isFinalRejection: true);
                (await SendStudentInstitutionUpdateAsync("FinalRejection")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                using var cleanupScope = _factory.Services.CreateScope();
                var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var uims = cleanupContext.Set<UserInscriptionModality>().Where(x => x.IdInscriptionModality == practiceId).ToList();
                if (uims.Count > 0) cleanupContext.Set<UserInscriptionModality>().RemoveRange(uims);
                var practice = await cleanupContext.Set<AcademicPractice>().FindAsync(practiceId);
                if (practice != null) cleanupContext.Set<AcademicPractice>().Remove(practice);
                var insc = await cleanupContext.Set<InscriptionModality>().FindAsync(practiceId);
                if (insc != null) cleanupContext.Set<InscriptionModality>().Remove(insc);
                foreach (var stId in createdStateIds)
                {
                    var st = await cleanupContext.Set<StateStage>().FindAsync(stId);
                    if (st != null) cleanupContext.Set<StateStage>().Remove(st);
                }
                await cleanupContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task AcademicPractice_UpdatePhaseApproval_CrossPhaseManagementAndStudentBlocks_ReturnForbidden()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);

            var f0StageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseInscripcion).Id;
            var f1StageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseDesarrollo).Id;
            var f2StageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseEvaluacion).Id;

            StateStage EnsurePaState(string code, string name, int stageId)
            {
                var existing = context.Set<StateStage>().FirstOrDefault(s => s.Code == code);
                if (existing != null) return existing;
                var created = new StateStage
                {
                    Code = code,
                    Name = name,
                    IdStageModality = stageId,
                    StatusRegister = true,
                    OperationRegister = "SeedCrossPhase"
                };
                context.Set<StateStage>().Add(created);
                return created;
            }

            var f0Rev = EnsurePaState(StateStageCodes.PaInscripcionEnRevision, "F0 En Revision", f0StageId);
            var f1Pend = EnsurePaState("PA_DESARROLLO_PEND_DOC", "F1 Pendiente Doc", f1StageId);
            var f1Rev = EnsurePaState("PA_DESARROLLO_EN_REVISION", "F1 En Revision", f1StageId);
            var f2Pend = EnsurePaState("PA_EVALUACION_PEND_DOC", "F2 Pendiente Doc", f2StageId);
            var f2Rev = EnsurePaState(StateStageCodes.PaInformeFinalEnRevision, "F2 Informe En Revision", f2StageId);
            await context.SaveChangesAsync();

            async Task SetPhaseAsync(int stageId, int stateStageId)
            {
                using var scope = _factory.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ap = await ctx.Set<AcademicPractice>().FindAsync(seed.Practice301Id);
                ap!.IdStateStage = stateStageId;
                var insc = await ctx.Set<InscriptionModality>().FindAsync(seed.Practice301Id);
                insc!.IdStageModality = stageId;
                await ctx.SaveChangesAsync();
            }

            async Task<HttpResponseMessage> PutPhaseApprovalAsync(string userId, string perm, int targetStateId)
            {
                using var req = CreateRequest(HttpMethod.Put, $"/api/AcademicPractice/UpdatePhaseApproval/{seed.Practice301Id}", perm, userId);
                req.Content = JsonContent.Create(new UpdatePhaseApprovalDto
                {
                    Id = seed.Practice301Id,
                    NewStateStageId = targetStateId,
                    Observations = "Cross-phase test"
                });
                return await _client.SendAsync(req);
            }

            // Fase 0: N4PAF1PG, N4PAF2PG, estudiante N3PAF1R y estudiante N3PAF2R -> 403 Forbidden
            foreach (var (actorId, perm) in new[]
            {
                ("501", PermissionCodes.PracticaAcademica.N4PAF1PG),
                ("501", PermissionCodes.PracticaAcademica.N4PAF2PG),
                ("301", PermissionCodes.PracticaAcademica.N3PAF1R),
                ("301", PermissionCodes.PracticaAcademica.N3PAF2R)
            })
            {
                (await PutPhaseApprovalAsync(actorId, perm, f0Rev.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // Fase 1: N4PAF0PG, N4PAF0PA, N4PAF2PG y estudiante N3PAF2R -> 403 Forbidden
            await SetPhaseAsync(f1StageId, f1Pend.Id);
            foreach (var (actorId, perm) in new[]
            {
                ("501", PermissionCodes.PracticaAcademica.N4PAF0PG),
                ("501", PermissionCodes.PracticaAcademica.N4PAF0PA),
                ("501", PermissionCodes.PracticaAcademica.N4PAF2PG),
                ("301", PermissionCodes.PracticaAcademica.N3PAF2R)
            })
            {
                (await PutPhaseApprovalAsync(actorId, perm, f1Rev.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // Fase 2: N4PAF0PG, N4PAF0PA y N4PAF1PG -> 403 Forbidden
            await SetPhaseAsync(f2StageId, f2Pend.Id);
            foreach (var perm in new[]
            {
                PermissionCodes.PracticaAcademica.N4PAF0PG,
                PermissionCodes.PracticaAcademica.N4PAF0PA,
                PermissionCodes.PracticaAcademica.N4PAF1PG
            })
            {
                (await PutPhaseApprovalAsync("501", perm, f2Rev.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task AcademicPractice_UpdateInstitutionInfo_AllowsManagersN4PAF0PG_F1PG_F2PG_AndDeniesN4PAF0PA()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);

            async Task<HttpResponseMessage> PutInstitutionInfoAsync(string perm, string suffix)
            {
                using var req = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdateInstitutionInfo/{seed.Practice301Id}",
                    perm,
                    userId: "501");
                req.Content = JsonContent.Create(new UpdateInstitutionInfoDto
                {
                    Id = seed.Practice301Id,
                    Title = $"Practica {suffix}",
                    InstitutionName = $"Empresa {suffix}",
                    InstitutionContact = "contacto@empresa.co",
                    PracticeStartDate = DateTimeOffset.UtcNow.AddDays(1),
                    PracticeEndDate = DateTimeOffset.UtcNow.AddMonths(4),
                    PracticeHours = 320,
                    Observations = $"Actualizado por {suffix}"
                });
                return await _client.SendAsync(req);
            }

            // N4PAF0PA -> 403 Forbidden
            (await PutInstitutionInfoAsync(PermissionCodes.PracticaAcademica.N4PAF0PA, "F0PA"))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Gestores N4PAF0PG, N4PAF1PG y N4PAF2PG -> 200 OK
            foreach (var managerPerm in new[]
            {
                PermissionCodes.PracticaAcademica.N4PAF0PG,
                PermissionCodes.PracticaAcademica.N4PAF1PG,
                PermissionCodes.PracticaAcademica.N4PAF2PG
            })
            {
                (await PutInstitutionInfoAsync(managerPerm, managerPerm))
                    .StatusCode.Should().Be(HttpStatusCode.OK, $"{managerPerm} debe poder actualizar información de institución");
            }
        }

        [Fact]
        public async Task AcademicPractice_RevokedOrInactiveTeacherAssignment_BlockedWithForbidden_OnUpdatePhaseApproval()
        {
            using var context = GetFreshDbContext();
            var seed = SeedAcademicPracticeScenario(context);
            EnsureBaseUsersAndPeriod(context, 410, 411, 412);

            var cargo = context.Set<TypeTeachingAssignment>().First(c => c.Code == "TUTOR_PA");
            context.Set<TeachingAssignment>().AddRange(
                new TeachingAssignment
                {
                    IdInscriptionModality = seed.Practice301Id,
                    IdTeacher = 410,
                    IdTypeTeachingAssignment = cargo.Id,
                    IsConfirmed = true,
                    RevocationDate = DateTimeOffset.UtcNow.AddDays(-1),
                    StatusRegister = true,
                    OperationRegister = "SeedRevoked"
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = seed.Practice301Id,
                    IdTeacher = 411,
                    IdTypeTeachingAssignment = cargo.Id,
                    IsConfirmed = true,
                    RevocationDate = null,
                    StatusRegister = false,
                    OperationRegister = "SeedInactive"
                },
                new TeachingAssignment
                {
                    IdInscriptionModality = seed.Practice301Id,
                    IdTeacher = 412,
                    IdTypeTeachingAssignment = cargo.Id,
                    IsConfirmed = true,
                    RevocationDate = null,
                    StatusRegister = true,
                    OperationRegister = "SeedActive"
                });

            var f0StageId = context.Set<StageModality>().First(s => s.Code == StageModalityCodes.PaFaseInscripcion).Id;
            var f0Rev = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.PaInscripcionEnRevision)
                ?? context.Set<StateStage>().Add(new StateStage
                {
                    Code = StateStageCodes.PaInscripcionEnRevision,
                    Name = "F0 En Revision",
                    IdStageModality = f0StageId,
                    StatusRegister = true
                }).Entity;
            await context.SaveChangesAsync();

            async Task<HttpResponseMessage> EvaluateAsync(string teacherId)
            {
                using var req = CreateRequest(
                    HttpMethod.Put,
                    $"/api/AcademicPractice/UpdatePhaseApproval/{seed.Practice301Id}",
                    PermissionCodes.PracticaAcademica.N4PAF0PR,
                    teacherId);
                req.Content = JsonContent.Create(new UpdatePhaseApprovalDto
                {
                    Id = seed.Practice301Id,
                    NewStateStageId = f0Rev.Id,
                    EvaluatorObservations = "Evaluación docente"
                });
                return await _client.SendAsync(req);
            }

            // Docente revocado (RevocationDate != null) -> 403 Forbidden
            (await EvaluateAsync("410")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Docente inactivo (StatusRegister == false) -> 403 Forbidden
            (await EvaluateAsync("411")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Control: docente activo confirmado -> 200 OK
            (await EvaluateAsync("412")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
