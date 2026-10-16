using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Responses;
using Application.Shared.DTOs.AcademicPractices;
using Application.Shared.DTOs.Teachers;
using Domain.Common;
using Domain.Common.AcademicPractice;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.AcademicPractices
{
    public class AcademicPracticeChallengerTests : IntegrationTestBase
    {
        private const string BaseUrl = "/api/AcademicPractice";

        public AcademicPracticeChallengerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private (AcademicPractice practice, InscriptionModality inscription) SeedPractice(
            AppDbContext context, 
            string title = "Challenger Practice")
        {
            var modality = new Modality { Name = "Modality " + Guid.NewGuid(), Description = "Test" };
            context.Set<Modality>().Add(modality);
            var stateInscription = new StateInscription { Name = "State " + Guid.NewGuid() };
            context.Set<StateInscription>().Add(stateInscription);
            var period = new AcademicPeriod 
            { 
                Code = "P" + Guid.NewGuid().ToString("N").Substring(0, 5), 
                StartDate = DateTime.UtcNow, 
                EndDate = DateTime.UtcNow.AddMonths(6) 
            };
            context.Set<AcademicPeriod>().Add(period);
            var stateStage = new StateStage { Name = "Stage " + Guid.NewGuid(), Code = "STAGE_" + Guid.NewGuid().ToString("N").Substring(0, 4) };
            context.Set<StateStage>().Add(stateStage);
            context.SaveChanges();

            var inscription = new InscriptionModality
            {
                IdModality = modality.Id,
                IdStateInscription = stateInscription.Id,
                IdAcademicPeriod = period.Id
            };
            context.Set<InscriptionModality>().Add(inscription);
            context.SaveChanges();

            var ap = new AcademicPractice
            {
                Id = inscription.Id,
                IdStateStage = stateStage.Id,
                Title = title,
                InstitutionName = "Test University",
                InstitutionContact = "contact@ufps.edu.co",
                PracticeStartDate = DateTime.UtcNow,
                PracticeEndDate = DateTime.UtcNow.AddMonths(4),
                PracticeHours = 120,
                StatusRegister = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Set<AcademicPractice>().Add(ap);
            context.SaveChanges();

            return (ap, inscription);
        }

        private User SeedUser(AppDbContext context, int userId, string firstName, string lastName, string email)
        {
            if (!context.Set<AcademicProgram>().Any(x => x.Id == 1))
                context.Set<AcademicProgram>().Add(new AcademicProgram { Id = 1, Name = "Prog", Code = "P1" });
            if (!context.Set<IdentificationType>().Any(x => x.Id == 1))
                context.Set<IdentificationType>().Add(new IdentificationType { Id = 1, Name = "ID", Code = "C1", Description = "D" });

            var existing = context.Set<User>().Find(userId);
            if (existing != null) return existing;

            var user = new User
            {
                Id = userId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Identification = "ID-" + userId,
                IdAcademicProgram = 1,
                IdIdentificationType = 1,
                StatusRegister = true
            };
            context.Set<User>().Add(user);
            context.SaveChanges();
            return user;
        }

        private TypeTeachingAssignment SeedCargo(AppDbContext context, string code, string name)
        {
            var existing = context.Set<TypeTeachingAssignment>().FirstOrDefault(c => c.Code == code);
            if (existing != null) return existing;

            var cargo = new TypeTeachingAssignment
            {
                Code = code,
                Name = name,
                Description = name,
                StatusRegister = true
            };
            context.Set<TypeTeachingAssignment>().Add(cargo);
            context.SaveChanges();
            return cargo;
        }

        [Fact]
        public async Task Challenge1_GetAll_ReturnsCompleteTeachersPayload_AndExcludesRevoked()
        {
            // Arrange
            int teacher1Id = 501;
            int teacher2Id = 502;
            int revokedTeacherId = 503;

            int ap1Id;
            int ap2Id;
            int ap3EmptyId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var t1 = SeedUser(context, teacher1Id, "Alberto", "Perez", "alberto.perez@ufps.edu.co");
                var t2 = SeedUser(context, teacher2Id, "Beatriz", "Castro", "beatriz.castro@ufps.edu.co");
                var tRev = SeedUser(context, revokedTeacherId, "Carlos", "Revocado", "carlos.revocado@ufps.edu.co");

                var directorCargo = SeedCargo(context, "DIRECTOR", "Director");
                var juradoCargo = SeedCargo(context, "JURADO", "Jurado Evaluador");

                // AP1: 2 active teachers
                var (ap1, _) = SeedPractice(context, "Practice With 2 Active Teachers");
                ap1Id = ap1.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = t1.Id,
                    IdInscriptionModality = ap1.Id,
                    IdTypeTeachingAssignment = directorCargo.Id,
                    StatusRegister = true
                });
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = t2.Id,
                    IdInscriptionModality = ap1.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    StatusRegister = true
                });

                // AP2: 1 active, 1 revoked teacher
                var (ap2, _) = SeedPractice(context, "Practice With Active And Revoked Teacher");
                ap2Id = ap2.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = t1.Id,
                    IdInscriptionModality = ap2.Id,
                    IdTypeTeachingAssignment = directorCargo.Id,
                    StatusRegister = true
                });
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = tRev.Id,
                    IdInscriptionModality = ap2.Id,
                    IdTypeTeachingAssignment = juradoCargo.Id,
                    StatusRegister = true,
                    RevocationDate = DateTimeOffset.UtcNow.AddDays(-5)
                });

                // AP3: 0 teachers
                var (ap3, _) = SeedPractice(context, "Practice With No Teachers");
                ap3EmptyId = ap3.Id;

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=50");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // AP1 verification
            var itemAp1 = result.Data!.Items.FirstOrDefault(i => i.AcademicPractice?.Id == ap1Id);
            itemAp1.Should().NotBeNull();
            itemAp1!.Teachers.Should().HaveCount(2);
            itemAp1.Teachers.Should().Contain(t => t.IdTeacher == teacher1Id && t.FullName == "Alberto Perez" && t.CargoCode == "DIRECTOR");
            itemAp1.Teachers.Should().Contain(t => t.IdTeacher == teacher2Id && t.FullName == "Beatriz Castro" && t.CargoCode == "JURADO");

            // AP2 verification (revoked excluded)
            var itemAp2 = result.Data!.Items.FirstOrDefault(i => i.AcademicPractice?.Id == ap2Id);
            itemAp2.Should().NotBeNull();
            itemAp2!.Teachers.Should().HaveCount(1);
            itemAp2.Teachers.First().IdTeacher.Should().Be(teacher1Id);
            itemAp2.Teachers.Should().NotContain(t => t.IdTeacher == revokedTeacherId);

            // AP3 verification (empty list, not null)
            var itemAp3 = result.Data!.Items.FirstOrDefault(i => i.AcademicPractice?.Id == ap3EmptyId);
            itemAp3.Should().NotBeNull();
            itemAp3!.Teachers.Should().NotBeNull();
            itemAp3.Teachers.Should().BeEmpty();
        }

        [Fact]
        public async Task Challenge2_JsonSerialization_ExposesAllRequiredDtoFieldsAndCompatibilityProperties()
        {
            // Arrange
            int teacherId = 510;
            int apId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacher = SeedUser(context, teacherId, "Elena", "Rios", "elena.rios@ufps.edu.co");
                var cargo = SeedCargo(context, "CO_DIRECTOR", "Co-Director");

                var (ap, _) = SeedPractice(context, "Practice For JSON Conformance");
                apId = ap.Id;

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = ap.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            // Act - Fetch raw JSON text to inspect serialized property names
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{apId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var rawJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // Assert JSON payload structure
            root.GetProperty("success").GetBoolean().Should().BeTrue();
            var data = root.GetProperty("data");
            var teachers = data.GetProperty("teachers");
            teachers.GetArrayLength().Should().Be(1);

            var t0 = teachers[0];

            // 1. Acceptance criteria standard fields
            t0.GetProperty("idTeacher").GetInt32().Should().Be(teacherId);
            t0.GetProperty("fullName").GetString().Should().Be("Elena Rios");
            t0.GetProperty("email").GetString().Should().Be("elena.rios@ufps.edu.co");
            t0.GetProperty("cargoCode").GetString().Should().Be("CO_DIRECTOR");
            t0.GetProperty("cargoName").GetString().Should().Be("Co-Director");
            t0.GetProperty("cargoId").GetInt32().Should().BeGreaterThan(0);

            // 2. Acceptance criteria backward compatibility accessors
            t0.GetProperty("idUser").GetInt32().Should().Be(teacherId);
            t0.GetProperty("assignmentType").GetString().Should().Be("Co-Director");
            t0.GetProperty("idTypeTeachingAssignment").GetInt32().Should().Be(t0.GetProperty("cargoId").GetInt32());
            t0.GetProperty("statusRegister").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Challenge3_ByTeacherQuery_ReturnsOnlyAssignedPracticesWithTeachersPopulated()
        {
            // Arrange
            int targetTeacherId = 520;
            int otherTeacherId = 521;
            int apTargetId;
            int apOtherId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var targetTeacher = SeedUser(context, targetTeacherId, "Target", "Docente", "target@ufps.edu.co");
                var otherTeacher = SeedUser(context, otherTeacherId, "Other", "Docente", "other@ufps.edu.co");
                var cargo = SeedCargo(context, "DOCENTE_ASESOR", "Asesor");

                var (ap1, _) = SeedPractice(context, "Target Practice");
                apTargetId = ap1.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = targetTeacher.Id,
                    IdInscriptionModality = ap1.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });

                var (ap2, _) = SeedPractice(context, "Other Practice");
                apOtherId = ap2.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = otherTeacher.Id,
                    IdInscriptionModality = ap2.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByTeacher/{targetTeacherId}?PageNumber=1&PageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apTargetId);
            result.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == apOtherId);

            var practiceItem = result.Data!.Items.First(i => i.AcademicPractice!.Id == apTargetId);
            practiceItem.Teachers.Should().NotBeEmpty();
            practiceItem.Teachers.First().IdTeacher.Should().Be(targetTeacherId);
            practiceItem.Teachers.First().CargoCode.Should().Be("DOCENTE_ASESOR");
            practiceItem.Teachers.First().AssignmentType.Should().Be("Asesor");
        }

        [Fact]
        public async Task Challenge4_ByUserQuery_ReturnsPopulatedTeachersForStudentPractice()
        {
            // Arrange
            int studentUserId = 530;
            int teacherId = 531;
            int apId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var student = SeedUser(context, studentUserId, "Daniel", "Estudiante", "daniel@ufps.edu.co");
                var teacher = SeedUser(context, teacherId, "Guillermo", "Docente", "guillermo@ufps.edu.co");
                var cargo = SeedCargo(context, "DOCENTE_DIRECTOR", "Director de Practica");

                var (ap, inscription) = SeedPractice(context, "Student Practice With Teacher");
                apId = ap.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdUser = student.Id,
                    IdInscriptionModality = inscription.Id
                });

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = inscription.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/ByUser/{studentUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeEmpty();

            var practice = result.Data!.FirstOrDefault(p => p.AcademicPractice!.Id == apId);
            practice.Should().NotBeNull();
            practice!.Teachers.Should().NotBeEmpty();
            practice.Teachers.First().IdTeacher.Should().Be(teacherId);
            practice.Teachers.First().FullName.Should().Be("Guillermo Docente");
            practice.Teachers.First().CargoCode.Should().Be("DOCENTE_DIRECTOR");
            practice.Teachers.First().CargoName.Should().Be("Director de Practica");
            practice.Teachers.First().IdUser.Should().Be(teacherId);
            practice.Teachers.First().AssignmentType.Should().Be("Director de Practica");
        }

        [Fact]
        public async Task Challenge5_BatchLoading_AcrossPaginationPages_EliminatesNPlusOne()
        {
            // Arrange: Seed 20 practices, each with 2 distinct teachers
            var practiceIds = new List<int>();
            int baseTeacherId = 600;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cargo1 = SeedCargo(context, "CARGO_A", "Cargo A");
                var cargo2 = SeedCargo(context, "CARGO_B", "Cargo B");

                for (int i = 0; i < 20; i++)
                {
                    var t1 = SeedUser(context, baseTeacherId + (i * 2), $"Teacher{i}_1", "Test", $"t{i}_1@ufps.edu.co");
                    var t2 = SeedUser(context, baseTeacherId + (i * 2) + 1, $"Teacher{i}_2", "Test", $"t{i}_2@ufps.edu.co");

                    var (ap, _) = SeedPractice(context, $"Batch Test AP {i}");
                    practiceIds.Add(ap.Id);

                    context.Set<TeachingAssignment>().Add(new TeachingAssignment
                    {
                        IdTeacher = t1.Id,
                        IdInscriptionModality = ap.Id,
                        IdTypeTeachingAssignment = cargo1.Id,
                        StatusRegister = true
                    });
                    context.Set<TeachingAssignment>().Add(new TeachingAssignment
                    {
                        IdTeacher = t2.Id,
                        IdInscriptionModality = ap.Id,
                        IdTypeTeachingAssignment = cargo2.Id,
                        StatusRegister = true
                    });
                }
                await context.SaveChangesAsync();
            }

            // Act - Fetch Page 1 (size 10)
            var responsePage1 = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10");
            responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
            var resultPage1 = await responsePage1.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();

            // Act - Fetch Page 2 (size 10)
            var responsePage2 = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=2&PageSize=10");
            responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);
            var resultPage2 = await responsePage2.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();

            // Assert
            resultPage1.Should().NotBeNull();
            resultPage1!.Data!.Items.Should().HaveCount(10);
            foreach (var item in resultPage1.Data!.Items)
            {
                if (practiceIds.Contains(item.AcademicPractice!.Id))
                {
                    item.Teachers.Should().HaveCount(2, "Each seeded practice must have exactly 2 active teachers batch loaded");
                    item.Teachers.Select(t => t.IdTeacher).Distinct().Should().HaveCount(2);
                }
            }

            resultPage2.Should().NotBeNull();
            resultPage2!.Data!.Items.Should().HaveCount(10);
            foreach (var item in resultPage2.Data!.Items)
            {
                if (practiceIds.Contains(item.AcademicPractice!.Id))
                {
                    item.Teachers.Should().HaveCount(2, "Page 2 practices must also have 2 teachers loaded without query degradation");
                }
            }

            // Direct test of TeacherAssignmentLoader with empty list - must not query or throw
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emptyResult = await TeacherAssignmentLoader.LoadActiveByInscriptionIdsAsync(context, new List<int>());
                emptyResult.Should().NotBeNull();
                emptyResult.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task Challenge6_ServerSideTeacherFilters_StressTest()
        {
            // Arrange
            int teacherIdAlpha = 701;
            int teacherIdBeta = 702;
            int apAlphaId;
            int apBetaId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacherAlpha = SeedUser(context, teacherIdAlpha, "Valentina", "Ochoa", "valentina.ochoa@ufps.edu.co");
                var teacherBeta = SeedUser(context, teacherIdBeta, "Sebastian", "Morales", "sebastian.morales@ufps.edu.co");

                var cargoDirector = SeedCargo(context, "DIRECTOR_EXCLUSIVO", "Director Exclusivo");
                var cargoJurado = SeedCargo(context, "JURADO_EXCLUSIVO", "Jurado Exclusivo");

                var (ap1, _) = SeedPractice(context, "Practice Alpha Valentina");
                apAlphaId = ap1.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacherAlpha.Id,
                    IdInscriptionModality = ap1.Id,
                    IdTypeTeachingAssignment = cargoDirector.Id,
                    StatusRegister = true
                });

                var (ap2, _) = SeedPractice(context, "Practice Beta Sebastian");
                apBetaId = ap2.Id;
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacherBeta.Id,
                    IdInscriptionModality = ap2.Id,
                    IdTypeTeachingAssignment = cargoJurado.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // 1. Filter by TeacherName@like
            var nameResp = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=Valentina");
            nameResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var nameResult = await nameResp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            nameResult!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apAlphaId);
            nameResult.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == apBetaId);

            // 2. Filter by TeacherCargoCode@eq
            var cargoResp = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherCargoCode@eq]=JURADO_EXCLUSIVO");
            cargoResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var cargoResult = await cargoResp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            cargoResult!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apBetaId);
            cargoResult.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == apAlphaId);

            // 3. Filter by TeacherEmail@like
            var emailResp = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherEmail@like]=valentina.ochoa");
            emailResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var emailResult = await emailResp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            emailResult!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apAlphaId);
            emailResult.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == apBetaId);

            // 4. Filter with non-matching name - should return 0 matching items
            var nonMatchingResp = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=InexistenteTotal999");
            nonMatchingResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var nonMatchingResult = await nonMatchingResp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            nonMatchingResult!.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Challenge7_PartialNameFiltering_CaseInsensitiveAndSubstring()
        {
            // Arrange
            int teacherId = 801;
            int apId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacher = SeedUser(context, teacherId, "Guillermo", "Santander", "guillermo.santander@ufps.edu.co");
                var cargo = SeedCargo(context, "ASESOR_CI", "Asesor CI");

                var (ap, _) = SeedPractice(context, "Practice For Substring Search");
                apId = ap.Id;

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = ap.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            // 1. Full uppercase "GUILLERMO"
            var respUpper = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=GUILLERMO");
            respUpper.StatusCode.Should().Be(HttpStatusCode.OK);
            var resUpper = await respUpper.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resUpper!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apId);

            // 2. Full lowercase "santander"
            var respLower = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=santander");
            respLower.StatusCode.Should().Be(HttpStatusCode.OK);
            var resLower = await respLower.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resLower!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apId);

            // 3. Substring of first name "illerm"
            var respSub1 = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=illerm");
            respSub1.StatusCode.Should().Be(HttpStatusCode.OK);
            var resSub1 = await respSub1.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resSub1!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apId);

            // 4. Substring of last name "tande"
            var respSub2 = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=tande");
            respSub2.StatusCode.Should().Be(HttpStatusCode.OK);
            var resSub2 = await respSub2.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resSub2!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apId);

            // 5. Full combined name "Guillermo Santander"
            var respFull = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=Guillermo%20Santander");
            respFull.StatusCode.Should().Be(HttpStatusCode.OK);
            var resFull = await respFull.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resFull!.Data!.Items.Should().Contain(i => i.AcademicPractice!.Id == apId);
        }

        [Fact]
        public async Task Challenge8_TeacherFilter_InactiveStatusRegister_StrictlyExcluded()
        {
            // Arrange
            int inactiveTeacherId = 810;
            int apId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacher = SeedUser(context, inactiveTeacherId, "Inactivo", "User", "inactivo@ufps.edu.co");
                var cargo = SeedCargo(context, "CARGO_INACT", "Cargo Inactivo");

                var (ap, _) = SeedPractice(context, "Practice Inactive Assignment");
                apId = ap.Id;

                // StatusRegister = false, RevocationDate = null
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = ap.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = false,
                    RevocationDate = null
                });
                await context.SaveChangesAsync();
            }

            // Act 1: GetAll with filter on Inactivo -> must NOT return apId
            var respFilter = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]=Inactivo");
            respFilter.StatusCode.Should().Be(HttpStatusCode.OK);
            var resFilter = await respFilter.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            resFilter!.Data!.Items.Should().NotContain(i => i.AcademicPractice!.Id == apId);

            // Act 2: WithDetails -> teachers list must be empty
            var respDetails = await _client.GetAsync($"{BaseUrl}/WithDetails/{apId}");
            respDetails.StatusCode.Should().Be(HttpStatusCode.OK);
            var resDetails = await respDetails.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeWithDetailsResponseDto>>();
            resDetails!.Data!.Teachers.Should().BeEmpty();
        }

        [Fact]
        public async Task Challenge9_WithDetails_EmptyTeachers_ReturnsEmptyListNotThrowing()
        {
            // Arrange
            int apEmptyId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var (ap, _) = SeedPractice(context, "Practice Definitely Zero Teachers");
                apEmptyId = ap.Id;
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/WithDetails/{apEmptyId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AcademicPracticeWithDetailsResponseDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Teachers.Should().NotBeNull();
            result.Data.Teachers.Should().BeEmpty();
        }

        [Theory]
        [InlineData("%")]
        [InlineData("_")]
        [InlineData("' OR '1'='1")]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("")]
        public async Task Challenge10_AdversarialFilterInputs_HandleGracefullyWithoutException(string input)
        {
            // Act
            var escaped = Uri.EscapeDataString(input);
            var response = await _client.GetAsync($"{BaseUrl}/GetAll?PageNumber=1&PageSize=10&filters[TeacherName@like]={escaped}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Challenge11_KeysetPagination_WithTeacherFilter_ExecutesCorrectly()
        {
            // Arrange: Seed 2 practices with same teacher
            int teacherId = 850;
            int ap1Id;
            int ap2Id;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacher = SeedUser(context, teacherId, "KeysetDocente", "Perez", "keyset@ufps.edu.co");
                var cargo = SeedCargo(context, "KEYSET_CARGO", "Cargo Keyset");

                var (ap1, _) = SeedPractice(context, "Practice Keyset Page 1");
                var (ap2, _) = SeedPractice(context, "Practice Keyset Page 2");
                ap1Id = ap1.Id;
                ap2Id = ap2.Id;

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = ap1.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdTeacher = teacher.Id,
                    IdInscriptionModality = ap2.Id,
                    IdTypeTeachingAssignment = cargo.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            // Act 1: Get first item with PageSize=1
            var page1Url = $"{BaseUrl}/GetAll?PageSize=1&filters[TeacherName@like]=KeysetDocente";
            var resp1 = await _client.GetAsync(page1Url);
            resp1.StatusCode.Should().Be(HttpStatusCode.OK);
            var res1 = await resp1.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            res1!.Data!.Items.Should().HaveCount(1);

            var firstItem = res1.Data.Items.First();
            var cursorCreatedAt = firstItem.AcademicPractice!.CreatedAt.ToString("o");
            var cursorId = firstItem.AcademicPractice.Id;

            // Act 2: Fetch next page using cursor
            var page2Url = $"{BaseUrl}/GetAll?PageSize=1&CursorId={cursorId}&CursorCreatedAt={Uri.EscapeDataString(cursorCreatedAt)}&filters[TeacherName@like]=KeysetDocente";
            var resp2 = await _client.GetAsync(page2Url);
            resp2.StatusCode.Should().Be(HttpStatusCode.OK);
            var res2 = await resp2.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<AcademicPracticeWithDetailsResponseDto>>>();
            res2!.Data!.Items.Should().HaveCount(1);
            res2.Data.Items.First().AcademicPractice!.Id.Should().NotBe(cursorId);
        }
    }
}
