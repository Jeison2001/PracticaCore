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
    }
}
