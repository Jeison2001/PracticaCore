using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.TeachingAssignments
{
    public class TeachingAssignmentControllerTests : IntegrationTestBase
    {
        public TeachingAssignmentControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<(User Teacher, InscriptionModality InscriptionModality, TypeTeachingAssignment TypeTeaching)> SeedDataAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var identificationType = new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
            var faculty = new Faculty { Code = "ING", Name = "Ingenieria", StatusRegister = true };
            var program = new AcademicProgram { Code = "SIS", Name = "Sistemas", Faculty = faculty, StatusRegister = true };
            
            context.Set<IdentificationType>().Add(identificationType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            await context.SaveChangesAsync();

            var teacher = new User 
            { 
                FirstName = "Test", 
                LastName = "Teacher", 
                Email = $"teacher_{Guid.NewGuid()}@test.com", 
                Identification = Guid.NewGuid().ToString().Substring(0, 10),
                IdIdentificationType = identificationType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            
            var typeTeaching = new TypeTeachingAssignment { Name = "Director", Description = "Director de tesis", StatusRegister = true };
            
            var modality = new Modality { Name = "Pasantia", Description = "Pasantia", StatusRegister = true };
            var state = new StateInscription { Name = "Activo", Description = "Activo", StatusRegister = true };
            var period = new AcademicPeriod { Code = "2024-1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(6), StatusRegister = true };
            
            context.Set<Modality>().Add(modality);
            context.Set<StateInscription>().Add(state);
            context.Set<AcademicPeriod>().Add(period);
            context.Set<User>().Add(teacher);
            context.Set<TypeTeachingAssignment>().Add(typeTeaching);
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

            // Habilitar el cargo para el docente (TeacherEnabledCargo) — requerido por CreateTeachingAssignmentCommandHandler
            context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
            {
                IdUser = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = true
            });
            await context.SaveChangesAsync();

            return (teacher, inscriptionModality, typeTeaching);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenDataIsValid()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TeachingAssignment", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.IdInscriptionModality.Should().Be(dto.IdInscriptionModality);
            result.Data.IdTeacher.Should().Be(dto.IdTeacher);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenCargoNotEnabledForTeacher()
        {
            // Arrange
            var (teacher, inscriptionModality, _) = await SeedDataAsync();

            // Cargo adicional que NO está habilitado para el docente
            int notEnabledCargoId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jurado = new TypeTeachingAssignment
                {
                    Code = "JURADO_EVALUADOR",
                    Name = "Jurado Evaluador",
                    Description = "Jurado",
                    MaxAssignments = 20,
                    StatusRegister = true
                };
                context.Set<TypeTeachingAssignment>().Add(jurado);
                await context.SaveChangesAsync();
                notEnabledCargoId = jurado.Id;
            }

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = notEnabledCargoId,
                StatusRegister = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/TeachingAssignment", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("no tiene habilitado el cargo"));
        }

        [Fact]
        public async Task GetByIdInscription_ShouldReturnList_WhenExists()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            
            // Create existing assignment
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByInscription/{inscriptionModality.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeachingAssignmentTeacherDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenDataIsValid()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int assignmentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            var updateDto = new TeachingAssignmentDto
            {
                Id = assignmentId,
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = false // Changing status
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TeachingAssignment/{assignmentId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.StatusRegister.Should().BeFalse();
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_ReturnsAssignedProjectsWithModalityAndState()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            int assignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().ContainSingle(a => a.Id == assignmentId);

            var item = result.Data!.First(a => a.Id == assignmentId);
            item.IdInscriptionModality.Should().Be(inscriptionModality.Id);
            item.IdModality.Should().Be(inscriptionModality.IdModality);
            item.ModalityName.Should().Be("Pasantia");
            item.IdStateInscription.Should().Be(inscriptionModality.IdStateInscription);
            item.StateInscriptionName.Should().Be("Activo");
            item.IdTypeTeachingAssignment.Should().Be(typeTeaching.Id);
            item.CargoName.Should().Be("Director");
            item.StatusRegister.Should().BeTrue();
            item.RevocationDate.Should().BeNull();
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_FiltersRevokedByDefault_AndIncludesWhenRequested()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            int revokedAssignmentId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var revoked = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    RevocationDate = DateTimeOffset.UtcNow,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(revoked);
                await context.SaveChangesAsync();
                revokedAssignmentId = revoked.Id;
            }

            // Act 1: Default (without includeRevoked)
            var response1 = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result1!.Data.Should().NotContain(a => a.Id == revokedAssignmentId);

            // Act 2: With includeRevoked = true
            var response2 = await _client.GetAsync($"/api/TeachingAssignment/ByTeacherAndCargo/{teacher.Id}/{typeTeaching.Id}?includeRevoked=true");
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<List<TeacherAssignedProjectDto>>>();
            result2!.Data.Should().Contain(a => a.Id == revokedAssignmentId);
        }

        [Fact]
        public async Task GetAssignmentsByTeacherAndCargo_WithInvalidIds_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/TeachingAssignment/ByTeacherAndCargo/0/1");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var response2 = await _client.GetAsync("/api/TeachingAssignment/ByTeacherAndCargo/1/0");
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_ReturnsCategorizedByCargo()
        {
            // Arrange
            var (teacher, inscriptionModality, typeDirector) = await SeedDataAsync();

            TypeTeachingAssignment typeAsesor;
            InscriptionModality inscription2;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                typeAsesor = new TypeTeachingAssignment { Code = "ASESOR", Name = "Asesor", Description = "Asesor", StatusRegister = true };
                context.Set<TypeTeachingAssignment>().Add(typeAsesor);

                inscription2 = new InscriptionModality
                {
                    IdModality = inscriptionModality.IdModality,
                    IdStateInscription = inscriptionModality.IdStateInscription,
                    IdAcademicPeriod = inscriptionModality.IdAcademicPeriod,
                    StatusRegister = true
                };
                context.Set<InscriptionModality>().Add(inscription2);
                await context.SaveChangesAsync();

                // Asignar docente como Director en inscriptionModality
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeDirector.Id,
                    StatusRegister = true
                });

                // Asignar docente como Asesor en inscription2
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscription2.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeAsesor.Id,
                    StatusRegister = true
                });

                await context.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacher/{teacher.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var directorGroup = result.Data!.FirstOrDefault(g => g.IdTypeTeachingAssignment == typeDirector.Id);
            directorGroup.Should().NotBeNull();
            directorGroup!.CargoName.Should().Be("Director");
            directorGroup.ActiveAssignmentsCount.Should().Be(1);
            directorGroup.Assignments.Should().ContainSingle(p => p.IdInscriptionModality == inscriptionModality.Id);

            var asesorGroup = result.Data!.FirstOrDefault(g => g.IdTypeTeachingAssignment == typeAsesor.Id);
            asesorGroup.Should().NotBeNull();
            asesorGroup!.CargoName.Should().Be("Asesor");
            asesorGroup.ActiveAssignmentsCount.Should().Be(1);
            asesorGroup.Assignments.Should().ContainSingle(p => p.IdInscriptionModality == inscription2.Id);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_WithCargoFilter_ReturnsOnlyFilteredCargo()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var response = await _client.GetAsync($"/api/TeachingAssignment/ByTeacher/{teacher.Id}?idCargo={typeTeaching.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            result!.Data.Should().ContainSingle(g => g.IdTypeTeachingAssignment == typeTeaching.Id);
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_WithInvalidId_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/TeachingAssignment/ByTeacher/0");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_AsStudentWithN3PGRP_ForcesUnconfirmed_DoesNotSendEmail_AndHidesFromTeacherQueries()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int studentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var student = new User
                {
                    FirstName = "Student",
                    LastName = "Proposer",
                    Email = $"student_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher.IdIdentificationType,
                    IdAcademicProgram = teacher.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });

                var researchLine = new ResearchLine { Name = "Line " + Guid.NewGuid(), Description = "Desc", StatusRegister = true };
                context.Set<ResearchLine>().Add(researchLine);
                await context.SaveChangesAsync();

                var subLine = new ResearchSubLine { Name = "SubLine " + Guid.NewGuid(), Description = "Desc", IdResearchLine = researchLine.Id, StatusRegister = true };
                context.Set<ResearchSubLine>().Add(subLine);

                var stateStage = new StateStage { Name = "Stage " + Guid.NewGuid(), Code = "ST_" + Guid.NewGuid().ToString("N")[..6], StatusRegister = true };
                context.Set<StateStage>().Add(stateStage);
                await context.SaveChangesAsync();

                context.Set<Proposal>().Add(new Proposal
                {
                    Id = inscriptionModality.Id,
                    Title = "Student Proposal",
                    GeneralObjective = "Goal",
                    SpecificObjectives = new List<string> { "Obj 1" },
                    Description = "Desc",
                    IdResearchLine = researchLine.Id,
                    IdResearchSubLine = subLine.Id,
                    IdStateStage = stateStage.Id,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            _factory.EnqueuedJobs.Clear();

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                IsConfirmed = true, // attempt to spoof committee confirmation
                StatusRegister = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/TeachingAssignment")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            created!.Data!.IsConfirmed.Should().BeFalse();
            _factory.EnqueuedJobs.Should().NotContain(j => j.MethodName == "HandleTeachingAssignmentCreationAsync");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await context.Set<TeachingAssignment>().FirstAsync(x => x.Id == created.Data.Id);
                persisted.IsConfirmed.Should().BeFalse();
            }

            using var byTeacherReq = new HttpRequestMessage(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacher/{teacher.Id}");
            byTeacherReq.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N4PGPPR);
            byTeacherReq.Headers.Add("X-Test-UserId", teacher.Id.ToString());
            var byTeacherResp = await _client.SendAsync(byTeacherReq);
            byTeacherResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var byTeacherData = await byTeacherResp.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            byTeacherData!.Data.Should().BeEmpty();

            var proposalsByTeacherResp = await _client.GetAsync($"/api/Proposal/ByTeacher/{teacher.Id}?PageNumber=1&PageSize=10");
            proposalsByTeacherResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var proposalsByTeacher = await proposalsByTeacherResp.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProposalWithDetailsResponseDto>>>();
            proposalsByTeacher!.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_AsStudentForJuradoEvaluador_ReturnsBadRequest()
        {
            var (teacher, inscriptionModality, _) = await SeedDataAsync();
            int studentId;
            int juradoCargoId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var student = new User
                {
                    FirstName = "Student",
                    LastName = "JuradoTest",
                    Email = $"student_j_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher.IdIdentificationType,
                    IdAcademicProgram = teacher.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);

                var jurado = new TypeTeachingAssignment
                {
                    Code = "JURADO_EVALUADOR",
                    Name = "Jurado Evaluador",
                    Description = "Jurado",
                    MaxAssignments = 20,
                    StatusRegister = true
                };
                context.Set<TypeTeachingAssignment>().Add(jurado);
                await context.SaveChangesAsync();

                studentId = student.Id;
                juradoCargoId = jurado.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                {
                    IdUser = teacher.Id,
                    IdTypeTeachingAssignment = juradoCargoId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = juradoCargoId,
                StatusRegister = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/TeachingAssignment")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", studentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_AsNonMemberStudent_ReturnsUnauthorized()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int nonMemberStudentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var outsider = new User
                {
                    FirstName = "Outsider",
                    LastName = "Student",
                    Email = $"outsider_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher.IdIdentificationType,
                    IdAcademicProgram = teacher.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(outsider);
                await context.SaveChangesAsync();
                nonMemberStudentId = outsider.Id;
            }

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                StatusRegister = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/TeachingAssignment")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            request.Headers.Add("X-Test-UserId", nonMemberStudentId.ToString());

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Update_AsCommitteeRatifiesStudentSuggestion_ConfirmsAndEnqueuesCreationEmail()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int assignmentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var suggestion = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    IsConfirmed = false,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(suggestion);
                await context.SaveChangesAsync();
                assignmentId = suggestion.Id;
            }

            _factory.EnqueuedJobs.Clear();

            var updateDto = new TeachingAssignmentDto
            {
                Id = assignmentId,
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                IsConfirmed = false,
                StatusRegister = true
            };

            var response = await _client.PutAsJsonAsync($"/api/TeachingAssignment/{assignmentId}", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            updated!.Data!.IsConfirmed.Should().BeTrue();
            _factory.EnqueuedJobs.Should().ContainSingle(j => j.MethodName == "HandleTeachingAssignmentCreationAsync");
            _factory.EnqueuedJobs.Should().NotContain(j => j.MethodName == "HandleTeachingAssignmentChangeAsync");

            var byTeacherResp = await _client.GetAsync($"/api/TeachingAssignment/ByTeacher/{teacher.Id}");
            byTeacherResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var groups = await byTeacherResp.Content.ReadFromJsonAsync<ApiResponse<List<TeacherCargoGroupDto>>>();
            groups!.Data.Should().ContainSingle();
        }

        [Fact]
        public async Task Create_SecondConfirmedJurado_AdvancesPreliminaryProjectToEnEvaluacion()
        {
            var (teacher1, inscriptionModality, _) = await SeedDataAsync();
            int teacher2Id;
            int juradoCargoId;
            int radicadoPendId;
            int enEvaluacionId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var radicadoPend = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.ApRadicadoPendAsigEval)
                    ?? new StateStage { Code = StateStageCodes.ApRadicadoPendAsigEval, Name = "Radicado Pend Asig Eval", StatusRegister = true };
                if (radicadoPend.Id == 0) context.Set<StateStage>().Add(radicadoPend);

                var enEvaluacion = context.Set<StateStage>().FirstOrDefault(s => s.Code == StateStageCodes.ApEnEvaluacion)
                    ?? new StateStage { Code = StateStageCodes.ApEnEvaluacion, Name = "En Evaluacion", StatusRegister = true };
                if (enEvaluacion.Id == 0) context.Set<StateStage>().Add(enEvaluacion);

                await context.SaveChangesAsync();
                radicadoPendId = radicadoPend.Id;
                enEvaluacionId = enEvaluacion.Id;

                var teacher2 = new User
                {
                    FirstName = "Second",
                    LastName = "Evaluator",
                    Email = $"teacher2_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher1.IdIdentificationType,
                    IdAcademicProgram = teacher1.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(teacher2);

                var jurado = context.Set<TypeTeachingAssignment>().FirstOrDefault(t => t.Code == "JURADO_EVALUADOR" && t.StatusRegister)
                    ?? new TypeTeachingAssignment
                    {
                        Code = "JURADO_EVALUADOR",
                        Name = "Jurado Evaluador",
                        Description = "Jurado",
                        MaxAssignments = 20,
                        StatusRegister = true
                    };
                if (jurado.Id == 0)
                    context.Set<TypeTeachingAssignment>().Add(jurado);

                await context.SaveChangesAsync();
                teacher2Id = teacher2.Id;
                juradoCargoId = jurado.Id;

                context.Set<TeacherEnabledCargo>().AddRange(
                    new TeacherEnabledCargo { IdUser = teacher1.Id, IdTypeTeachingAssignment = juradoCargoId, StatusRegister = true },
                    new TeacherEnabledCargo { IdUser = teacher2Id, IdTypeTeachingAssignment = juradoCargoId, StatusRegister = true });

                context.Set<PreliminaryProject>().Add(new PreliminaryProject
                {
                    Id = inscriptionModality.Id,
                    IdStateStage = radicadoPendId,
                    StatusRegister = true,
                    OperationRegister = "Seed"
                });
                await context.SaveChangesAsync();
            }

            var firstResp = await _client.PostAsJsonAsync("/api/TeachingAssignment", new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher1.Id,
                IdTypeTeachingAssignment = juradoCargoId,
                StatusRegister = true
            });
            firstResp.StatusCode.Should().Be(HttpStatusCode.Created);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var projectAfterOne = await context.Set<PreliminaryProject>().FindAsync(inscriptionModality.Id);
                projectAfterOne!.IdStateStage.Should().Be(radicadoPendId);
            }

            var secondResp = await _client.PostAsJsonAsync("/api/TeachingAssignment", new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher2Id,
                IdTypeTeachingAssignment = juradoCargoId,
                StatusRegister = true
            });
            secondResp.StatusCode.Should().Be(HttpStatusCode.Created);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var projectAfterTwo = await context.Set<PreliminaryProject>().FindAsync(inscriptionModality.Id);
                projectAfterTwo!.IdStateStage.Should().Be(enEvaluacionId);
            }
        }

        [Fact]
        public async Task GetAssignmentsByTeacher_AndByTeacherAndCargo_EnforceOwnershipWithoutManagementPermission()
        {
            var (teacher1, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int teacher2Id;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var teacher2 = new User
                {
                    FirstName = "Other",
                    LastName = "Teacher",
                    Email = $"other_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher1.IdIdentificationType,
                    IdAcademicProgram = teacher1.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(teacher2);
                await context.SaveChangesAsync();
                teacher2Id = teacher2.Id;

                context.Set<TeachingAssignment>().Add(new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher2Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            // Teacher 1 querying Teacher 2 with only review permission -> Forbid
            using var crossTeacherReq = new HttpRequestMessage(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacher/{teacher2Id}");
            crossTeacherReq.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N4PGPPR);
            crossTeacherReq.Headers.Add("X-Test-UserId", teacher1.Id.ToString());
            (await _client.SendAsync(crossTeacherReq)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var crossCargoReq = new HttpRequestMessage(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacherAndCargo/{teacher2Id}/{typeTeaching.Id}");
            crossCargoReq.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N4PGPPR);
            crossCargoReq.Headers.Add("X-Test-UserId", teacher1.Id.ToString());
            (await _client.SendAsync(crossCargoReq)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Teacher 2 querying self with only review permission -> OK
            using var selfReq = new HttpRequestMessage(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacher/{teacher2Id}");
            selfReq.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N4PGPPR);
            selfReq.Headers.Add("X-Test-UserId", teacher2Id.ToString());
            (await _client.SendAsync(selfReq)).StatusCode.Should().Be(HttpStatusCode.OK);

            // Committee/management user querying Teacher 2 -> OK
            using var committeeReq = new HttpRequestMessage(HttpMethod.Get, $"/api/TeachingAssignment/ByTeacherAndCargo/{teacher2Id}/{typeTeaching.Id}");
            committeeReq.Headers.Add("X-Test-Permissions", $"{PermissionCodes.ProyectoGrado.N4PGPPR},{PermissionCodes.ProyectoGrado.N4PGPPG}");
            committeeReq.Headers.Add("X-Test-UserId", teacher1.Id.ToString());
            (await _client.SendAsync(committeeReq)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_AsCoordinatorForPreviouslySuggestedAssignment_PromotesToConfirmedAndEnqueuesCreationEmail()
        {
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int studentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var student = new User
                {
                    FirstName = "Suggester",
                    LastName = "Student",
                    Email = $"suggester_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher.IdIdentificationType,
                    IdAcademicProgram = teacher.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(student);
                await context.SaveChangesAsync();
                studentId = student.Id;

                context.Set<UserInscriptionModality>().Add(new UserInscriptionModality
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdUser = studentId,
                    StatusRegister = true
                });
                await context.SaveChangesAsync();
            }

            var dto = new TeachingAssignmentDto
            {
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                IsConfirmed = true,
                StatusRegister = true
            };

            // Step 1: Student suggests teacher -> unconfirmed, no email
            using var studentReq = new HttpRequestMessage(HttpMethod.Post, "/api/TeachingAssignment")
            {
                Content = JsonContent.Create(dto)
            };
            studentReq.Headers.Add("X-Test-Permissions", PermissionCodes.ProyectoGrado.N3PGRP);
            studentReq.Headers.Add("X-Test-UserId", studentId.ToString());

            var studentResp = await _client.SendAsync(studentReq);
            studentResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var suggested = await studentResp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            suggested!.Data!.IsConfirmed.Should().BeFalse();

            _factory.EnqueuedJobs.Clear();

            // Step 2: Coordinator POSTs same teacher/cargo -> promotes existing assignment idempotently & enqueues email
            var coordResp = await _client.PostAsJsonAsync("/api/TeachingAssignment", dto);
            coordResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var promoted = await coordResp.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            promoted!.Data!.Id.Should().Be(suggested.Data.Id);
            promoted.Data.IsConfirmed.Should().BeTrue();
            _factory.EnqueuedJobs.Should().ContainSingle(j => j.MethodName == "HandleTeachingAssignmentCreationAsync");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var matches = await context.Set<TeachingAssignment>()
                    .Where(x => x.IdInscriptionModality == inscriptionModality.Id &&
                                x.IdTeacher == teacher.Id &&
                                x.IdTypeTeachingAssignment == typeTeaching.Id &&
                                x.StatusRegister)
                    .ToListAsync();
                matches.Should().ContainSingle();
                matches[0].IsConfirmed.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Update_ReassigningToTeacherWithoutEnabledCargo_ReturnsBadRequest()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int assignmentId;
            int otherTeacherId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var otherTeacher = new User
                {
                    FirstName = "Unenabled",
                    LastName = "Teacher",
                    Email = $"unenabled_{Guid.NewGuid()}@test.com",
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    IdIdentificationType = teacher.IdIdentificationType,
                    IdAcademicProgram = teacher.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(otherTeacher);

                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    IsConfirmed = true,
                    StatusRegister = true
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();

                assignmentId = assignment.Id;
                otherTeacherId = otherTeacher.Id;
            }

            var updateDto = new TeachingAssignmentDto
            {
                Id = assignmentId,
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = otherTeacherId,
                IdTypeTeachingAssignment = typeTeaching.Id,
                IsConfirmed = true,
                StatusRegister = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TeachingAssignment/{assignmentId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await verifyContext.Set<TeachingAssignment>().FindAsync(assignmentId);
                persisted.Should().NotBeNull();
                persisted!.IdTeacher.Should().Be(teacher.Id);
            }
        }

        [Fact]
        public async Task Update_ReactivatingAssignmentWhenCargoDisabled_ReturnsBadRequest()
        {
            // Arrange
            var (teacher, inscriptionModality, typeTeaching) = await SeedDataAsync();
            int assignmentId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var enabledCargo = await context.Set<TeacherEnabledCargo>()
                    .FirstAsync(c => c.IdUser == teacher.Id && c.IdTypeTeachingAssignment == typeTeaching.Id);
                enabledCargo.StatusRegister = false;

                var assignment = new TeachingAssignment
                {
                    IdInscriptionModality = inscriptionModality.Id,
                    IdTeacher = teacher.Id,
                    IdTypeTeachingAssignment = typeTeaching.Id,
                    IsConfirmed = true,
                    StatusRegister = false
                };
                context.Set<TeachingAssignment>().Add(assignment);
                await context.SaveChangesAsync();
                assignmentId = assignment.Id;
            }

            var reactivateDto = new TeachingAssignmentDto
            {
                Id = assignmentId,
                IdInscriptionModality = inscriptionModality.Id,
                IdTeacher = teacher.Id,
                IdTypeTeachingAssignment = typeTeaching.Id,
                IsConfirmed = true,
                StatusRegister = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/TeachingAssignment/{assignmentId}", reactivateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TeachingAssignmentDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();

            using (var verifyScope = _factory.Services.CreateScope())
            {
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await verifyContext.Set<TeachingAssignment>().FindAsync(assignmentId);
                persisted.Should().NotBeNull();
                persisted!.StatusRegister.Should().BeFalse();
            }
        }
    }
}
