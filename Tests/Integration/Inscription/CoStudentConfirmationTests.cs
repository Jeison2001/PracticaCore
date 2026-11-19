using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Responses;
using Application.Common.Services;
using Application.Common.Services.Notifications.Handlers;
using Application.Shared.DTOs.Documents;
using Application.Shared.DTOs.Evaluations;
using Application.Shared.DTOs.InscriptionModalities;
using Application.Shared.DTOs.InscriptionWithStudents;
using Application.Shared.DTOs.Proposals;
using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Notifications;
using Domain.Interfaces.Services.Notifications.Builders;
using Domain.Interfaces.Services.Storage;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Integration.Utilities;
using Xunit;

namespace Tests.Integration.Inscription
{
    public class CoStudentConfirmationTests : IntegrationTestBase
    {
        public CoStudentConfirmationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateInscription_AntiIdor_WhenAuthenticatedUserNotInStudents_ReturnsForbidden()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Intento IDOR"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new() { Identification = seed.StudentA.Identification, IdIdentificationType = seed.IdType.Id },
                    new() { Identification = seed.StudentB.Identification, IdIdentificationType = seed.IdType.Id }
                }
            };

            using var req = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                seed.StudentC.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                dto);

            var res = await _client.SendAsync(req);

            res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await verifyDb.Set<InscriptionModality>().CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task CreateGroupInscription_SetsConfirmationFlags_EnqueuesInvitationEmail_AndResolvesInvitedStudentRecipient()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);

            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Inscripción grupal A y B"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new() { Identification = seed.StudentA.Identification, IdIdentificationType = seed.IdType.Id },
                    new() { Identification = seed.StudentB.Identification, IdIdentificationType = seed.IdType.Id }
                }
            };

            using var req = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                seed.StudentA.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                dto);

            var res = await _client.SendAsync(req);

            res.StatusCode.Should().Be(HttpStatusCode.Created);
            var payload = await res.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            var created = payload.Data!;
            created.Students.Should().HaveCount(2);

            var dtoA = created.Students.Single(s => s.IdUser == seed.StudentA.Id);
            var dtoB = created.Students.Single(s => s.IdUser == seed.StudentB.Id);
            dtoA.Id.Should().BeGreaterThan(0);
            dtoA.IsConfirmed.Should().BeTrue();
            dtoA.StatusRegister.Should().BeTrue();
            dtoB.Id.Should().BeGreaterThan(0);
            dtoB.IsConfirmed.Should().BeFalse();
            dtoB.StatusRegister.Should().BeTrue();

            _factory.EnqueuedJobs.Should().Contain(j => j.MethodName == "HandleInscriptionCreationAsync");

            // Verificar flujo directo de InscriptionChangeHandler.HandleCreationAsync + EmailRecipientResolverService
            using var scope = _factory.Services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var eventDataBuilder = scope.ServiceProvider.GetRequiredService<IInscriptionEventDataBuilder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<InscriptionChangeHandler>>();
            var recipientResolver = scope.ServiceProvider.GetRequiredService<IEmailRecipientResolverService>();

            var enqueuedNotifications = new List<(string EventName, Dictionary<string, object> Data)>();
            var queueMock = new Mock<IEmailNotificationQueueService>();
            queueMock.Setup(q => q.EnqueueEventNotification(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Callback<string, Dictionary<string, object>>((eventName, data) => enqueuedNotifications.Add((eventName, data)))
                .Returns("mock-job-id");

            var inscriptionEntity = await uow.GetRepository<InscriptionModality, int>()
                .GetByIdAsync(created.InscriptionModality.Id);
            inscriptionEntity.Should().NotBeNull();

            var changeHandler = new InscriptionChangeHandler(queueMock.Object, eventDataBuilder, uow, logger);
            await changeHandler.HandleCreationAsync(inscriptionEntity!);

            enqueuedNotifications.Should().ContainSingle(n => n.EventName == "INSCRIPTION_CREATED");
            enqueuedNotifications.Should().ContainSingle(n => n.EventName == "INSCRIPTION_COSTUDENT_INVITED");

            var invitedData = enqueuedNotifications.Single(n => n.EventName == "INSCRIPTION_COSTUDENT_INVITED").Data;
            invitedData["InscriptionId"].Should().Be(created.InscriptionModality.Id);
            invitedData["ModalityName"].Should().Be(seed.ProyectoGradoModality.Name);
            invitedData["AcademicPeriod"].Should().Be(seed.Period.Code);
            invitedData["AcademicPeriodCode"].Should().Be(seed.Period.Code);
            invitedData.Should().ContainKey("InscriptionDate");
            invitedData["CreatorName"].Should().Be($"{seed.StudentA.FirstName} {seed.StudentA.LastName}");
            invitedData["CreatorEmail"].Should().Be(seed.StudentA.Email);
            invitedData["InvitedStudentName"].Should().Be($"{seed.StudentB.FirstName} {seed.StudentB.LastName}");
            invitedData["InvitedStudentEmail"].Should().Be(seed.StudentB.Email);

            var rules = new List<EmailRecipientRule>
            {
                new()
                {
                    RecipientType = "TO",
                    RuleType = "EVENT_PARTICIPANT",
                    RuleValue = "INVITED_STUDENT",
                    Priority = 1,
                    StatusRegister = true
                }
            };
            var recipients = await recipientResolver.ResolveRecipientsAsync(rules, invitedData);
            recipients.To.Should().ContainSingle().Which.Should().Be(seed.StudentB.Email);
        }

        [Fact]
        public async Task CommitteePatch_BlocksApprovalWhileCoStudentUnconfirmed_AllowsRejection()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // B puede consultar detalle de su invitación pendiente via GET /api/Inscription/{id}
            using (var getBReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Inscription/{inscriptionId}",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGC))
            {
                var getBRes = await _client.SendAsync(getBReq);
                getBRes.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Comité intenta aprobar con B sin confirmar -> 400 BadRequest
            using var approveReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{inscriptionId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Intento aprobar"
                });

            var approveRes = await _client.SendAsync(approveReq);
            approveRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var approvePayload = await approveRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
            approvePayload!.Errors.Should().Contain(e => e.Contains("pendientes de confirmar"));

            // Comité SÍ puede rechazar aunque existan co-estudiantes sin confirmar -> 200 OK
            using var rejectReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{inscriptionId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.RejectedState.Id,
                    Observations = "Rechazada por documentación incompleta"
                });

            var rejectRes = await _client.SendAsync(rejectReq);
            rejectRes.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task PendingInvitation_DoesNotBlockInviteeFromCreatingOwnInscription_AndBlocksLaterConfirmation()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var groupInscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // B crea su propia inscripción individual -> 201 Created
            var ownDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Inscripción propia de B"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new() { Identification = seed.StudentB.Identification, IdIdentificationType = seed.IdType.Id }
                }
            };

            using var createOwnReq = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                ownDto);

            var createOwnRes = await _client.SendAsync(createOwnReq);
            createOwnRes.StatusCode.Should().Be(HttpStatusCode.Created);

            // Luego B intenta confirmar la invitación de A -> 400 BadRequest
            using var confirmReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{groupInscriptionId}/confirm-participation",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);

            var confirmRes = await _client.SendAsync(confirmReq);
            confirmRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var confirmPayload = await confirmRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
            confirmPayload!.Errors.Should().Contain(e => e.Contains("otra modalidad"));
        }

        [Fact]
        public async Task ConfirmParticipation_AllowsCommitteeApproval_AndGrantsProgressivePermissionsToBothStudents()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // B confirma su participación -> 200 OK
            using var confirmReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/confirm-participation",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);

            var confirmRes = await _client.SendAsync(confirmReq);
            confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var confirmPayload = await confirmRes.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsResponseDto>>();
            confirmPayload!.Success.Should().BeTrue();
            confirmPayload.Data!.Students.Should().HaveCount(2);
            confirmPayload.Data.Students.Should().OnlyContain(s => s.IsConfirmed && s.StatusRegister);

            // Comité aprueba -> 200 OK
            using var approveReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{inscriptionId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Aprobada tras confirmación"
                });

            var approveRes = await _client.SendAsync(approveReq);
            approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var permsA = await verifyDb.Set<UserPermission>()
                .Where(up => up.IdUser == seed.StudentA.Id && up.StatusRegister)
                .Join(verifyDb.Set<Permission>(), up => up.IdPermission, p => p.Id, (_, p) => p.Code)
                .ToListAsync();

            var permsB = await verifyDb.Set<UserPermission>()
                .Where(up => up.IdUser == seed.StudentB.Id && up.StatusRegister)
                .Join(verifyDb.Set<Permission>(), up => up.IdPermission, p => p.Id, (_, p) => p.Code)
                .ToListAsync();

            var expectedCodes = new[]
            {
                PermissionCodes.ProyectoGrado.N1PG,
                PermissionCodes.ProyectoGrado.N2PGP,
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N3PGCP
            };

            permsA.Should().Contain(expectedCodes);
            permsB.Should().Contain(expectedCodes);
        }

        [Fact]
        public async Task RejectParticipation_DeactivatesInviteeLink_ExcludesFromByUser_AndGrantsPermissionsOnlyToCreatorOnApproval()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // B rechaza su participación -> 200 OK
            using var rejectReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/reject-participation",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGR);

            var rejectRes = await _client.SendAsync(rejectReq);
            rejectRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var rejectPayload = await rejectRes.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsResponseDto>>();
            rejectPayload!.Data!.Students.Should().ContainSingle(s => s.IdUser == seed.StudentA.Id && s.IsConfirmed);

            using (var checkScope = _factory.Services.CreateScope())
            {
                var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var linkB = await checkDb.Set<UserInscriptionModality>()
                    .FirstAsync(x => x.IdInscriptionModality == inscriptionId && x.IdUser == seed.StudentB.Id);
                linkB.StatusRegister.Should().BeFalse();
                linkB.OperationRegister.Should().Be("REJECTED_BY_COSTUDENT");
            }

            // ByUser/{B} vacío
            using var byBReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Inscription/ByUser/{seed.StudentB.Id}",
                seed.StudentB.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);
            var byBRes = await _client.SendAsync(byBReq);
            byBRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var byBPayload = await byBRes.Content.ReadFromJsonAsync<ApiResponse<List<InscriptionWithStudentsResponseDto>>>();
            byBPayload!.Data.Should().BeEmpty();

            // Comité aprueba inscripción de A -> 200 OK
            using var approveReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{inscriptionId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Aprobada para creador individual"
                });
            var approveRes = await _client.SendAsync(approveReq);
            approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var permsA = await verifyDb.Set<UserPermission>()
                .Where(up => up.IdUser == seed.StudentA.Id && up.StatusRegister)
                .Join(verifyDb.Set<Permission>(), up => up.IdPermission, p => p.Id, (_, p) => p.Code)
                .ToListAsync();
            var permsB = await verifyDb.Set<UserPermission>()
                .Where(up => up.IdUser == seed.StudentB.Id && up.StatusRegister)
                .ToListAsync();

            permsA.Should().Contain(
            [
                PermissionCodes.ProyectoGrado.N1PG,
                PermissionCodes.ProyectoGrado.N2PGP,
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N3PGCP
            ]);
            permsB.Should().BeEmpty();
        }

        [Fact]
        public async Task ForbiddenAndInvalidParticipationActions_AndUnconfirmedHelperGuard_AreEnforced()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // C ajeno intentando confirmar o rechazar -> 403 Forbidden
            using var foreignConfirmReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/confirm-participation",
                seed.StudentC.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);
            using var foreignRejectReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/reject-participation",
                seed.StudentC.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);

            var foreignConfirmRes = await _client.SendAsync(foreignConfirmReq);
            var foreignRejectRes = await _client.SendAsync(foreignRejectReq);
            foreignConfirmRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            foreignRejectRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // A ya confirmado intentando confirmar -> 400 BadRequest
            using var alreadyConfirmedReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/confirm-participation",
                seed.StudentA.Id,
                PermissionCodes.ModalidadesGrado.N2MGC);
            var alreadyConfirmedRes = await _client.SendAsync(alreadyConfirmedReq);
            alreadyConfirmedRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // ModalityAccessAuthorizationHelper y StudentDataService bloquean/excluyen a B sin confirmar por defecto
            using var scope = _factory.Services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var studentDataService = scope.ServiceProvider.GetRequiredService<IStudentDataService>();

            (await ModalityAccessAuthorizationHelper.IsUserInInscriptionAsync(uow, inscriptionId, seed.StudentA.Id)).Should().BeTrue();
            (await ModalityAccessAuthorizationHelper.IsUserInInscriptionAsync(uow, inscriptionId, seed.StudentB.Id)).Should().BeFalse();
            (await ModalityAccessAuthorizationHelper.IsUserInInscriptionAsync(uow, inscriptionId, seed.StudentB.Id, requireConfirmed: false)).Should().BeTrue();

            var (_, confirmedEmails, confirmedCount) = await studentDataService.GetStudentDataByInscriptionAsync(inscriptionId);
            confirmedCount.Should().Be(1);
            confirmedEmails.Should().Be(seed.StudentA.Email);
        }

        [Fact]
        public async Task CreatorStudent_RejectParticipation_ReturnsBadRequest()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // El creador (IsConfirmed = true) no puede rechazar su propia participación -> 400 BadRequest
            using var creatorRejectReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/reject-participation",
                seed.StudentA.Id,
                PermissionCodes.ModalidadesGrado.N2MGR);

            var creatorRejectRes = await _client.SendAsync(creatorRejectReq);
            creatorRejectRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RespondParticipation_WhenInscriptionNotPending_AndAfterRejection_ReturnsExpectedStatuses()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);

            var studentRole = await context.Set<Role>().FirstAsync(r => r.Code == "STUDENT");
            var studentD = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "444111444",
                FirstName = "Estudiante",
                LastName = "CasoAprobadoA",
                Email = "aprobado.d@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            var studentE = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "555111555",
                FirstName = "Estudiante",
                LastName = "CasoAprobadoB",
                Email = "aprobado.e@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            context.Set<User>().AddRange(studentD, studentE);
            await context.SaveChangesAsync();
            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = studentD.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentE.Id, IdRole = studentRole.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            // Caso 1: Inscripción RECHAZADA -> confirm-participation y reject-participation retornan 400 BadRequest
            var rejectedInscId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);
            using (var rejectByCommitteeReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{rejectedInscId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.RejectedState.Id,
                    Observations = "Rechazada por comité"
                }))
            {
                (await _client.SendAsync(rejectByCommitteeReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            foreach (var action in new[] { "confirm-participation", "reject-participation" })
            {
                using var req = CreateRequest(
                    HttpMethod.Post,
                    $"/api/Inscription/{rejectedInscId}/{action}",
                    seed.StudentB.Id,
                    PermissionCodes.ModalidadesGrado.N2MGC);
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            // Caso 2: Inscripción APROBADA -> confirm-participation y reject-participation retornan 400 BadRequest
            var approvedInscId = await CreateGroupInscriptionAsync(studentD, studentE, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);
            using (var confirmEReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{approvedInscId}/confirm-participation",
                studentE.Id,
                PermissionCodes.ModalidadesGrado.N2MGC))
            {
                (await _client.SendAsync(confirmEReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
            using (var approveByCommitteeReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{approvedInscId}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Aprobada por comité"
                }))
            {
                (await _client.SendAsync(approveByCommitteeReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            foreach (var action in new[] { "confirm-participation", "reject-participation" })
            {
                using var req = CreateRequest(
                    HttpMethod.Post,
                    $"/api/Inscription/{approvedInscId}/{action}",
                    studentE.Id,
                    PermissionCodes.ModalidadesGrado.N2MGC);
                (await _client.SendAsync(req)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            // Caso 3: Co-estudiante rechaza (StatusRegister = false) y reintenta confirmar o rechazar -> 403 Forbidden
            var retryInscId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentC, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);
            using (var firstRejectReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{retryInscId}/reject-participation",
                seed.StudentC.Id,
                PermissionCodes.ModalidadesGrado.N2MGR))
            {
                (await _client.SendAsync(firstRejectReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            foreach (var action in new[] { "confirm-participation", "reject-participation" })
            {
                using var retryReq = CreateRequest(
                    HttpMethod.Post,
                    $"/api/Inscription/{retryInscId}/{action}",
                    seed.StudentC.Id,
                    PermissionCodes.ModalidadesGrado.N2MGC);
                (await _client.SendAsync(retryReq)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Theory]
        [InlineData("confirm-participation")]
        [InlineData("reject-participation")]
        public async Task RespondParticipation_UnauthenticatedOrMissingPermission_ReturnsUnauthorizedAndForbidden(string action)
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);
            var inscriptionId = await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            // Sin autenticación -> 401 Unauthorized
            using (var anonReq = new HttpRequestMessage(HttpMethod.Post, $"/api/Inscription/{inscriptionId}/{action}"))
            {
                anonReq.Headers.Add("X-Test-Anonymous", "true");
                var anonRes = await _client.SendAsync(anonReq);
                anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }

            // Autenticado sin N2MGC ni N2MGR -> 403 Forbidden
            using (var noPermReq = CreateRequest(
                HttpMethod.Post,
                $"/api/Inscription/{inscriptionId}/{action}",
                seed.StudentB.Id,
                PermissionCodes.Configuraciones.N2CFP))
            {
                var noPermRes = await _client.SendAsync(noPermReq);
                noPermRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task CreateGroupInscription_WhenInviteeHasConfirmedVsUnconfirmedInscription_EnforcesActiveConfirmedRule()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);

            var studentRole = await context.Set<Role>().FirstAsync(r => r.Code == "STUDENT");
            var studentD = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "444000444",
                FirstName = "Estudiante",
                LastName = "Confirmado",
                Email = "confirmado.d@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            var studentE = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "555000555",
                FirstName = "Estudiante",
                LastName = "Invitador",
                Email = "invitador.e@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            context.Set<User>().AddRange(studentD, studentE);
            await context.SaveChangesAsync();
            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = studentD.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentE.Id, IdRole = studentRole.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            // 1. A invita a B (B queda con IsConfirmed = false). Luego C invita a B en otro grupo -> 201 Created (permitido).
            await CreateGroupInscriptionAsync(seed.StudentA, seed.StudentB, seed.ProyectoGradoModality.Id, seed.Period.Id, seed.IdType.Id);

            var secondGroupDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Segundo grupo invitando a B aún no confirmado"
                },
                Students =
                [
                    new() { Identification = seed.StudentC.Identification, IdIdentificationType = seed.IdType.Id },
                    new() { Identification = seed.StudentB.Identification, IdIdentificationType = seed.IdType.Id }
                ]
            };
            using (var allowReq = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                seed.StudentC.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                secondGroupDto))
            {
                var allowRes = await _client.SendAsync(allowReq);
                allowRes.StatusCode.Should().Be(HttpStatusCode.Created);
            }

            // 2. D crea su propia inscripción (IsConfirmed = true). Luego E intenta invitar a D -> 400 BadRequest.
            var dIndividualDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Inscripción individual confirmada de D"
                },
                Students =
                [
                    new() { Identification = studentD.Identification, IdIdentificationType = seed.IdType.Id }
                ]
            };
            using (var dCreateReq = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                studentD.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                dIndividualDto))
            {
                (await _client.SendAsync(dCreateReq)).StatusCode.Should().Be(HttpStatusCode.Created);
            }

            var blockedGroupDto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    Observations = "Grupo E intentando invitar a D ya confirmado"
                },
                Students =
                [
                    new() { Identification = studentE.Identification, IdIdentificationType = seed.IdType.Id },
                    new() { Identification = studentD.Identification, IdIdentificationType = seed.IdType.Id }
                ]
            };
            using (var blockReq = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                studentE.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                blockedGroupDto))
            {
                var blockRes = await _client.SendAsync(blockReq);
                blockRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var payload = await blockRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
                payload!.Errors.Should().Contain(e => e.Contains("ya tiene una inscripción activa registrada"));
            }
        }

        [Fact]
        public async Task UnconfirmedCoStudent_IsBlockedWithForbidden_AcrossDownstreamPhaseAndTransversalEndpoints()
        {
            var storageMock = new Mock<IFileStorageService>();
            storageMock
                .Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("stored_test.pdf");

            using var isolatedFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_ => storageMock.Object);
                });
            });
            using var client = isolatedFactory.CreateClient();

            int inscriptionId;
            int studentAId;
            int studentBId;
            int researchLineId;
            int researchSubLineId;
            int docTypeId;
            int teacherId;
            int directorCargoId;

            using (var seedScope = isolatedFactory.Services.CreateScope())
            {
                var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var seed = await SeedCoStudentScenarioAsync(context);
                studentAId = seed.StudentA.Id;
                studentBId = seed.StudentB.Id;

                var propStage = await context.Set<StageModality>().FirstAsync(s => s.Code == StageModalityCodes.PgFasePropuesta);
                var researchLine = new ResearchLine { Name = "Línea CoStudent", Description = "Desc", StatusRegister = true };
                context.Set<ResearchLine>().Add(researchLine);
                await context.SaveChangesAsync();
                researchLineId = researchLine.Id;

                var researchSubLine = new ResearchSubLine { Name = "SubLínea CoStudent", Description = "Desc", IdResearchLine = researchLineId, StatusRegister = true };
                context.Set<ResearchSubLine>().Add(researchSubLine);

                var directorCargo = new TypeTeachingAssignment { Code = "DIRECTOR", Name = "Director", MaxAssignments = 5, StatusRegister = true };
                context.Set<TypeTeachingAssignment>().Add(directorCargo);

                var teacher = new User
                {
                    IdIdentificationType = seed.IdType.Id,
                    Identification = "888000888",
                    FirstName = "Docente",
                    LastName = "Disponible",
                    Email = "docente.disp@unicesar.edu.co",
                    IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                    StatusRegister = true
                };
                context.Set<User>().Add(teacher);
                await context.SaveChangesAsync();

                directorCargoId = directorCargo.Id;
                researchSubLineId = researchSubLine.Id;
                teacherId = teacher.Id;
                context.Set<TeacherEnabledCargo>().Add(new TeacherEnabledCargo
                {
                    IdUser = teacherId,
                    IdTypeTeachingAssignment = directorCargoId,
                    StatusRegister = true
                });

                var docType = await context.Set<DocumentType>().FirstAsync(dt => dt.Code == DocumentTypeCodes.AnteproyectoEntregable);
                docTypeId = docType.Id;

                var insc = new InscriptionModality
                {
                    IdModality = seed.ProyectoGradoModality.Id,
                    IdAcademicPeriod = seed.Period.Id,
                    IdStateInscription = seed.ApprovedState.Id,
                    IdStageModality = propStage.Id,
                    StatusRegister = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.Set<InscriptionModality>().Add(insc);
                await context.SaveChangesAsync();
                inscriptionId = insc.Id;

                context.Set<UserInscriptionModality>().AddRange(
                    new UserInscriptionModality
                    {
                        IdInscriptionModality = inscriptionId,
                        IdUser = studentAId,
                        IsConfirmed = true,
                        StatusRegister = true
                    },
                    new UserInscriptionModality
                    {
                        IdInscriptionModality = inscriptionId,
                        IdUser = studentBId,
                        IsConfirmed = false,
                        StatusRegister = true
                    });
                await context.SaveChangesAsync();
            }

            var bPermissions = string.Join(",",
                PermissionCodes.ModalidadesGrado.N2MGC,
                PermissionCodes.ModalidadesGrado.N2MGR,
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N3PGCP,
                PermissionCodes.ProyectoGrado.N3PGRA,
                PermissionCodes.ProyectoGrado.N3PGRPR);

            // 1. POST /api/Proposal -> 403 Forbidden
            using (var propReq = CreateRequest(
                HttpMethod.Post,
                "/api/Proposal",
                studentBId,
                bPermissions,
                new CreateProposalDto
                {
                    Id = inscriptionId,
                    Title = "Intento co-estudiante no confirmado",
                    Description = "Desc",
                    GeneralObjective = "Objetivo",
                    SpecificObjectives = ["Obj 1"],
                    IdResearchLine = researchLineId,
                    IdResearchSubLine = researchSubLineId
                }))
            {
                var propRes = await client.SendAsync(propReq);
                propRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 2. GET /api/Document/ByInscriptionModality/{id} -> 403 Forbidden
            using (var getDocsReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10",
                studentBId,
                bPermissions))
            {
                var getDocsRes = await client.SendAsync(getDocsReq);
                getDocsRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 3. POST /api/Document -> 403 Forbidden
            var uploadContent = new MultipartFormDataContent();
            uploadContent.Add(new StringContent(inscriptionId.ToString()), "IdInscriptionModality");
            uploadContent.Add(new StringContent(docTypeId.ToString()), "IdDocumentType");
            uploadContent.Add(new StringContent("Doc no confirmado"), "Name");
            var pdfBytes = new ByteArrayContent([1, 2, 3, 4]);
            pdfBytes.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            uploadContent.Add(pdfBytes, "File", "unconfirmed.pdf");

            using (var postDocReq = new HttpRequestMessage(HttpMethod.Post, "/api/Document"))
            {
                postDocReq.Headers.Add("X-Test-UserId", studentBId.ToString());
                postDocReq.Headers.Add("X-Test-Permissions", bPermissions);
                postDocReq.Content = uploadContent;

                var postDocRes = await client.SendAsync(postDocReq);
                postDocRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 4. GET /api/Evaluation/history/Proposal/{id} -> 403 Forbidden
            using (var historyReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Evaluation/history/Proposal/{inscriptionId}",
                studentBId,
                bPermissions))
            {
                var historyRes = await client.SendAsync(historyReq);
                historyRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // 5. POST /api/TeachingAssignment -> 403 Forbidden
            using (var taReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeachingAssignment",
                studentBId,
                bPermissions,
                new TeachingAssignmentDto
                {
                    IdInscriptionModality = inscriptionId,
                    IdTeacher = teacherId,
                    IdTypeTeachingAssignment = directorCargoId,
                    IsConfirmed = false,
                    StatusRegister = true
                }))
            {
                var taRes = await client.SendAsync(taReq);
                taRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }

            // Sanity check: estudiante A confirmado (IsConfirmed = true) sí pasa los guards -> 200 OK
            using (var ownerDocsReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Document/ByInscriptionModality/{inscriptionId}?PageNumber=1&PageSize=10",
                studentAId,
                bPermissions))
            {
                (await client.SendAsync(ownerDocsReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
            using (var ownerHistoryReq = CreateRequest(
                HttpMethod.Get,
                $"/api/Evaluation/history/Proposal/{inscriptionId}",
                studentAId,
                bPermissions))
            {
                (await client.SendAsync(ownerHistoryReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Fact]
        public async Task ThreeStudentGroupInscription_CommitteeApprovalBlockedUntilThirdCoStudentConfirmsOrRejects()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedCoStudentScenarioAsync(context);

            var studentRole = await context.Set<Role>().FirstAsync(r => r.Code == "STUDENT");
            var studentD = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "666000666",
                FirstName = "Estudiante",
                LastName = "D",
                Email = "est.d@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            var studentE = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "777000777",
                FirstName = "Estudiante",
                LastName = "E",
                Email = "est.e@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            var studentF = new User
            {
                IdIdentificationType = seed.IdType.Id,
                Identification = "888000111",
                FirstName = "Estudiante",
                LastName = "F",
                Email = "est.f@unicesar.edu.co",
                IdAcademicProgram = seed.StudentA.IdAcademicProgram,
                StatusRegister = true
            };
            context.Set<User>().AddRange(studentD, studentE, studentF);
            await context.SaveChangesAsync();
            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = studentD.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentE.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentF.Id, IdRole = studentRole.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            async Task<int> CreateThreeStudentGroupAsync(User u1, User u2, User u3)
            {
                var dto = new InscriptionWithStudentsCreateDto
                {
                    InscriptionModality = new InscriptionModalityCreateDto
                    {
                        IdModality = seed.ProyectoGradoModality.Id,
                        IdAcademicPeriod = seed.Period.Id,
                        Observations = "Grupo de 3 estudiantes"
                    },
                    Students =
                    [
                        new() { Identification = u1.Identification, IdIdentificationType = seed.IdType.Id },
                        new() { Identification = u2.Identification, IdIdentificationType = seed.IdType.Id },
                        new() { Identification = u3.Identification, IdIdentificationType = seed.IdType.Id }
                    ]
                };

                using var req = CreateRequest(HttpMethod.Post, "/api/Inscription", u1.Id, PermissionCodes.ModalidadesGrado.N2MGR, dto);
                var res = await _client.SendAsync(req);
                res.StatusCode.Should().Be(HttpStatusCode.Created);
                var payload = await res.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
                return payload!.Data!.InscriptionModality.Id;
            }

            // Grupo 1 [A, B, C]: B confirma, C sigue sin confirmar -> Comité bloqueado (400). Luego C confirma -> Comité aprueba (200).
            var group1Id = await CreateThreeStudentGroupAsync(seed.StudentA, seed.StudentB, seed.StudentC);

            using (var confirmBReq = CreateRequest(HttpMethod.Post, $"/api/Inscription/{group1Id}/confirm-participation", seed.StudentB.Id, PermissionCodes.ModalidadesGrado.N2MGC))
            {
                (await _client.SendAsync(confirmBReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var blockedApproveReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{group1Id}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Intento con C sin confirmar"
                }))
            {
                var blockedRes = await _client.SendAsync(blockedApproveReq);
                blockedRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var payload = await blockedRes.Content.ReadFromJsonAsync<ApiResponse<object>>();
                payload!.Errors.Should().Contain(e => e.Contains("pendientes de confirmar"));
            }

            using (var confirmCReq = CreateRequest(HttpMethod.Post, $"/api/Inscription/{group1Id}/confirm-participation", seed.StudentC.Id, PermissionCodes.ModalidadesGrado.N2MGC))
            {
                (await _client.SendAsync(confirmCReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var approveGroup1Req = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{group1Id}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Aprobado con A, B y C confirmados"
                }))
            {
                (await _client.SendAsync(approveGroup1Req)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Grupo 2 [D, E, F]: E confirma, F sigue sin confirmar -> Comité bloqueado (400). Luego F rechaza -> Comité aprueba (200) y F no recibe permisos.
            var group2Id = await CreateThreeStudentGroupAsync(studentD, studentE, studentF);

            using (var confirmEReq = CreateRequest(HttpMethod.Post, $"/api/Inscription/{group2Id}/confirm-participation", studentE.Id, PermissionCodes.ModalidadesGrado.N2MGC))
            {
                (await _client.SendAsync(confirmEReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var blockedApprove2Req = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{group2Id}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Intento con F sin confirmar"
                }))
            {
                (await _client.SendAsync(blockedApprove2Req)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            using (var rejectFReq = CreateRequest(HttpMethod.Post, $"/api/Inscription/{group2Id}/reject-participation", studentF.Id, PermissionCodes.ModalidadesGrado.N2MGC))
            {
                (await _client.SendAsync(rejectFReq)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using (var approveGroup2Req = CreateRequest(
                HttpMethod.Patch,
                $"/api/InscriptionModality/{group2Id}",
                seed.CommitteeUser.Id,
                PermissionCodes.ModalidadesGrado.N3MGPR,
                new InscriptionModalityPatchDto
                {
                    IdStateInscription = seed.ApprovedState.Id,
                    Observations = "Aprobado con D y E tras rechazo de F"
                }))
            {
                (await _client.SendAsync(approveGroup2Req)).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fPerms = await verifyDb.Set<UserPermission>()
                .Where(up => up.IdUser == studentF.Id && up.StatusRegister)
                .ToListAsync();
            fPerms.Should().BeEmpty();
        }

        private async Task<int> CreateGroupInscriptionAsync(
            User creator,
            User invitee,
            int modalityId,
            int periodId,
            int idTypeId)
        {
            var dto = new InscriptionWithStudentsCreateDto
            {
                InscriptionModality = new InscriptionModalityCreateDto
                {
                    IdModality = modalityId,
                    IdAcademicPeriod = periodId,
                    Observations = "Inscripción grupal"
                },
                Students = new List<UserInscriptionModalityCreateDto>
                {
                    new() { Identification = creator.Identification, IdIdentificationType = idTypeId },
                    new() { Identification = invitee.Identification, IdIdentificationType = idTypeId }
                }
            };

            using var req = CreateRequest(
                HttpMethod.Post,
                "/api/Inscription",
                creator.Id,
                PermissionCodes.ModalidadesGrado.N2MGR,
                dto);

            var res = await _client.SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.Created);
            var payload = await res.Content.ReadFromJsonAsync<ApiResponse<InscriptionWithStudentsDto>>();
            return payload!.Data!.InscriptionModality.Id;
        }

        private static async Task<CoStudentSeed> SeedCoStudentScenarioAsync(AppDbContext context)
        {
            SeedingUtilities.SeedCatalogs(context);

            var idType = new IdentificationType { Code = "CC", Name = "Cédula", Description = "Cédula", StatusRegister = true };
            var faculty = new Faculty { Code = "ING", Name = "Ingeniería", StatusRegister = true };
            var program = new AcademicProgram { Code = "SYS", Name = "Sistemas", Faculty = faculty, StatusRegister = true };
            var period = new AcademicPeriod
            {
                Code = "2026-2",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
                StatusRegister = true
            };
            var studentRole = new Role { Code = "STUDENT", Name = "Estudiante", Description = "Estudiante", StatusRegister = true };

            context.Set<IdentificationType>().Add(idType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            context.Set<AcademicPeriod>().Add(period);
            context.Set<Role>().Add(studentRole);

            // Asegurar permisos progresivos de Proyecto de Grado para StartProposalPhaseOnApprovalHandler
            var permissionsToSeed = new[]
            {
                new Permission { Code = PermissionCodes.ProyectoGrado.N1PG, Description = "PG", StatusRegister = true },
                new Permission { Code = PermissionCodes.ProyectoGrado.N2PGP, ParentCode = PermissionCodes.ProyectoGrado.N1PG, Description = "Propuesta", StatusRegister = true },
                new Permission { Code = PermissionCodes.ProyectoGrado.N3PGRP, ParentCode = PermissionCodes.ProyectoGrado.N2PGP, Description = "Registrar propuesta", StatusRegister = true },
                new Permission { Code = PermissionCodes.ProyectoGrado.N3PGCP, ParentCode = PermissionCodes.ProyectoGrado.N2PGP, Description = "Consultar propuesta", StatusRegister = true }
            };
            foreach (var perm in permissionsToSeed)
            {
                if (!await context.Set<Permission>().AnyAsync(p => p.Code == perm.Code))
                    context.Set<Permission>().Add(perm);
            }

            await context.SaveChangesAsync();

            var proyectoGrado = await context.Set<Modality>().FirstAsync(m => m.Code == ModalityCodes.ProyectoGrado);
            proyectoGrado.MaxStudents = 3;
            await context.SaveChangesAsync();

            var studentA = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "111000111",
                FirstName = "Estudiante",
                LastName = "Creador",
                Email = "creador.a@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            var studentB = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "222000222",
                FirstName = "CoEstudiante",
                LastName = "Invitado",
                Email = "invitado.b@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            var studentC = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "333000333",
                FirstName = "Externo",
                LastName = "Tercero",
                Email = "externo.c@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            var committee = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "999000999",
                FirstName = "Comite",
                LastName = "Evaluador",
                Email = "comite@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            context.Set<User>().AddRange(studentA, studentB, studentC, committee);
            await context.SaveChangesAsync();

            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = studentA.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentB.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = studentC.Id, IdRole = studentRole.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            var approvedState = await context.Set<StateInscription>().FirstAsync(s => s.Code == StateInscriptionCodes.Aprobado);
            var rejectedState = await context.Set<StateInscription>().FirstAsync(s => s.Code == StateInscriptionCodes.Rechazado);

            return new CoStudentSeed(
                idType,
                period,
                proyectoGrado,
                approvedState,
                rejectedState,
                studentA,
                studentB,
                studentC,
                committee);
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

        private sealed record CoStudentSeed(
            IdentificationType IdType,
            AcademicPeriod Period,
            Modality ProyectoGradoModality,
            StateInscription ApprovedState,
            StateInscription RejectedState,
            User StudentA,
            User StudentB,
            User StudentC,
            User CommitteeUser);
    }
}
