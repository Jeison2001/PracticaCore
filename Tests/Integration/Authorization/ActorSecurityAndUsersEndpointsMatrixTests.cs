using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs;
using Application.Shared.DTOs.Permissions;
using Application.Shared.DTOs.RolePermissions;
using Application.Shared.DTOs.Roles;
using Application.Shared.DTOs.TeacherEnabledCargos;
using Application.Shared.DTOs.TypeTeachingAssignments;
using Application.Shared.DTOs.UserPermissions;
using Application.Shared.DTOs.UserRoles;
using Application.Shared.DTOs.Users;
using Domain.Common;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration.Authorization
{
    public class ActorSecurityAndUsersEndpointsMatrixTests : IntegrationTestBase
    {
        public ActorSecurityAndUsersEndpointsMatrixTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Theory]
        [InlineData("GET", "/api/User")]
        [InlineData("GET", "/api/UserRole/ByUser/1")]
        [InlineData("POST", "/api/UserRole")]
        [InlineData("GET", "/api/Role")]
        [InlineData("GET", "/api/Permission")]
        [InlineData("GET", "/api/TeacherEnabledCargo/ByUser/1")]
        public async Task AnonymousActor_ReturnsUnauthorized_OnProtectedEndpoints(string method, string url)
        {
            var httpMethod = method == "POST" ? HttpMethod.Post : HttpMethod.Get;
            object? body = method == "POST" ? new UserRoleDto { IdUser = 1, IdRole = 1, StatusRegister = true } : null;

            using var request = CreateRequest(httpMethod, url, anonymous: true, body: body);
            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ProfileActor_WithN2CFP_CanReadOwnUser_AndGetsExactDtoData()
        {
            using var context = GetFreshDbContext();
            var (ownUser, _) = await SeedTwoUsersAsync(context);

            using var request = CreateRequest(
                HttpMethod.Get,
                $"/api/User/{ownUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().NotBeNull();
            payload.Data!.Id.Should().Be(ownUser.Id);
            payload.Data.Email.Should().Be(ownUser.Email);
            payload.Data.FirstName.Should().Be(ownUser.FirstName);
            payload.Data.LastName.Should().Be(ownUser.LastName);
        }

        [Fact]
        public async Task ProfileActor_WithN2CFP_GetsForbidden_OnCrossUserGetAndPut_AntiIdor()
        {
            using var context = GetFreshDbContext();
            var (ownUser, otherUser) = await SeedTwoUsersAsync(context);

            using var getOtherReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/{otherUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            var updateDto = new UserDto
            {
                Id = otherUser.Id,
                IdIdentificationType = otherUser.IdIdentificationType,
                Identification = otherUser.Identification,
                Email = otherUser.Email,
                FirstName = "Tampered",
                LastName = otherUser.LastName,
                IdAcademicProgram = otherUser.IdAcademicProgram,
                StatusRegister = true
            };

            using var putOtherReq = CreateRequest(
                HttpMethod.Put,
                $"/api/User/{otherUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP,
                body: updateDto);

            var getOtherRes = await _client.SendAsync(getOtherReq);
            var putOtherRes = await _client.SendAsync(putOtherReq);

            getOtherRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            putOtherRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ProfileActor_WithN2CFP_GetsForbidden_OnUserListAndPatchStatus()
        {
            using var context = GetFreshDbContext();
            var (ownUser, _) = await SeedTwoUsersAsync(context);

            using var listReq = CreateRequest(
                HttpMethod.Get,
                "/api/User?PageNumber=1&PageSize=10",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            using var patchStatusReq = CreateRequest(
                HttpMethod.Patch,
                $"/api/User/{ownUser.Id}/status",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP,
                body: new UpdateStatusRequestDto { StatusRegister = false });

            var listRes = await _client.SendAsync(listReq);
            var patchRes = await _client.SendAsync(patchStatusReq);

            listRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            patchRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ProfileActor_WithN2CFP_CanReadOwnRolesAndPermissions_AndGetsForbiddenForOtherUser()
        {
            using var context = GetFreshDbContext();
            var (ownUser, otherUser) = await SeedTwoUsersAsync(context);

            var role = new Role
            {
                Code = $"STU_{Guid.NewGuid():N}"[..10],
                Name = "Student Role",
                Description = "Student",
                StatusRegister = true
            };
            var permission = new Permission
            {
                Code = PermissionCodes.Configuraciones.N2CFP,
                Description = "Perfil propio",
                StatusRegister = true
            };
            context.Set<Role>().Add(role);
            context.Set<Permission>().Add(permission);
            await context.SaveChangesAsync();

            var userRole = new UserRole
            {
                IdUser = ownUser.Id,
                IdRole = role.Id,
                StatusRegister = true
            };
            var userPermission = new UserPermission
            {
                IdUser = ownUser.Id,
                IdPermission = permission.Id,
                StatusRegister = true
            };
            context.Set<UserRole>().Add(userRole);
            context.Set<UserPermission>().Add(userPermission);
            await context.SaveChangesAsync();

            using var ownRolesReq = CreateRequest(
                HttpMethod.Get,
                $"/api/UserRole/ByUser/{ownUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            using var ownPermsReq = CreateRequest(
                HttpMethod.Get,
                $"/api/UserPermission/ByUser/{ownUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            using var otherRolesReq = CreateRequest(
                HttpMethod.Get,
                $"/api/UserRole/ByUser/{otherUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            using var otherPermsReq = CreateRequest(
                HttpMethod.Get,
                $"/api/UserPermission/ByUser/{otherUser.Id}",
                userId: ownUser.Id.ToString(),
                permissions: PermissionCodes.Configuraciones.N2CFP);

            var ownRolesRes = await _client.SendAsync(ownRolesReq);
            var ownPermsRes = await _client.SendAsync(ownPermsReq);
            var otherRolesRes = await _client.SendAsync(otherRolesReq);
            var otherPermsRes = await _client.SendAsync(otherPermsReq);

            ownRolesRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var rolesPayload = await ownRolesRes.Content.ReadFromJsonAsync<ApiResponse<List<UserRoleInfoDto>>>();
            rolesPayload.Should().NotBeNull();
            rolesPayload!.Success.Should().BeTrue();
            rolesPayload.Data.Should().ContainSingle(r => r.Role.Id == role.Id && r.Role.Code == role.Code && r.UserRole.Id == userRole.Id);

            ownPermsRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var permsPayload = await ownPermsRes.Content.ReadFromJsonAsync<ApiResponse<List<UserPermissionInfoDto>>>();
            permsPayload.Should().NotBeNull();
            permsPayload!.Success.Should().BeTrue();
            permsPayload.Data.Should().ContainSingle(p => p.Permission.Id == permission.Id && p.Permission.Code == permission.Code && p.UserPermission.Id == userPermission.Id);

            otherRolesRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            otherPermsRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UserAdmin_WithN3CFSU_GetsPaginatedUsers_WithSeededRoles()
        {
            using var context = GetFreshDbContext();
            var (user1, user2) = await SeedTwoUsersAsync(context);

            var role1 = new Role
            {
                Code = $"R1_{Guid.NewGuid():N}"[..10],
                Name = "Role One",
                Description = "First seeded role",
                StatusRegister = true
            };
            var role2 = new Role
            {
                Code = $"R2_{Guid.NewGuid():N}"[..10],
                Name = "Role Two",
                Description = "Second seeded role",
                StatusRegister = true
            };
            context.Set<Role>().AddRange(role1, role2);
            await context.SaveChangesAsync();

            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = user1.Id, IdRole = role1.Id, StatusRegister = true },
                new UserRole { IdUser = user2.Id, IdRole = role2.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            using var request = CreateRequest(
                HttpMethod.Get,
                "/api/User?PageNumber=1&PageSize=10",
                userId: "9001",
                permissions: PermissionCodes.Configuraciones.N3CFSU);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<UserDto>>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().NotBeNull();
            payload.Data!.Items.Should().HaveCount(2);

            var dto1 = payload.Data.Items.Single(u => u.Id == user1.Id);
            dto1.Roles.Should().NotBeNull();
            dto1.Roles.Should().ContainSingle(r => r.Id == role1.Id && r.Code == role1.Code && r.Name == role1.Name);

            var dto2 = payload.Data.Items.Single(u => u.Id == user2.Id);
            dto2.Roles.Should().NotBeNull();
            dto2.Roles.Should().ContainSingle(r => r.Id == role2.Id && r.Code == role2.Code && r.Name == role2.Name);
        }

        [Fact]
        public async Task RoleAndPermissionEndpoints_AllowAdminsWithExactData_AndDenyProfileOnly()
        {
            using var context = GetFreshDbContext();
            var role = new Role
            {
                Code = $"ADM_{Guid.NewGuid():N}"[..10],
                Name = "Admin Role",
                Description = "Admin",
                StatusRegister = true
            };
            var permission = new Permission
            {
                Code = $"P_{Guid.NewGuid():N}"[..10],
                Description = "Custom Permission",
                StatusRegister = true
            };
            context.Set<Role>().Add(role);
            context.Set<Permission>().Add(permission);
            await context.SaveChangesAsync();

            var rolePermission = new RolePermission
            {
                IdRole = role.Id,
                IdPermission = permission.Id,
                StatusRegister = true
            };
            context.Set<RolePermission>().Add(rolePermission);
            await context.SaveChangesAsync();

            foreach (var adminPerm in new[] { PermissionCodes.Configuraciones.N3CFSRL, PermissionCodes.Configuraciones.N3CFSU })
            {
                using var roleReq = CreateRequest(HttpMethod.Get, "/api/Role?PageNumber=1&PageSize=10", userId: "9002", permissions: adminPerm);
                using var permReq = CreateRequest(HttpMethod.Get, "/api/Permission?PageNumber=1&PageSize=10", userId: "9002", permissions: adminPerm);

                var roleRes = await _client.SendAsync(roleReq);
                var permRes = await _client.SendAsync(permReq);

                roleRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var rolesPayload = await roleRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<RoleDto>>>();
                rolesPayload!.Success.Should().BeTrue();
                rolesPayload.Data!.Items.Should().Contain(r => r.Id == role.Id && r.Code == role.Code && r.Name == role.Name);

                permRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var permsPayload = await permRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<PermissionDto>>>();
                permsPayload!.Success.Should().BeTrue();
                permsPayload.Data!.Items.Should().Contain(p => p.Id == permission.Id && p.Code == permission.Code);
            }

            using var rolePermReq = CreateRequest(
                HttpMethod.Get,
                $"/api/RolePermission/ByRole?id={role.Id}",
                userId: "9003",
                permissions: PermissionCodes.Configuraciones.N3CFSRL);
            var rolePermRes = await _client.SendAsync(rolePermReq);

            rolePermRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var rpPayload = await rolePermRes.Content.ReadFromJsonAsync<ApiResponse<List<RolePermissionInfoDto>>>();
            rpPayload!.Success.Should().BeTrue();
            rpPayload.Data.Should().ContainSingle(rp => rp.Permission.Id == permission.Id && rp.Permission.Code == permission.Code && rp.RolePermission.Id == rolePermission.Id);

            using var deniedPermReq = CreateRequest(
                HttpMethod.Get,
                "/api/Permission",
                userId: "9004",
                permissions: PermissionCodes.Configuraciones.N2CFP);
            using var deniedRolePermReq = CreateRequest(
                HttpMethod.Get,
                $"/api/RolePermission/ByRole?id={role.Id}",
                userId: "9004",
                permissions: PermissionCodes.Configuraciones.N2CFP);

            var deniedPermRes = await _client.SendAsync(deniedPermReq);
            var deniedRolePermRes = await _client.SendAsync(deniedRolePermReq);

            deniedPermRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            deniedRolePermRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task TeacherEnabledCargo_ByUser_AllowsN3CFSUAndN3CFSRL_AndDeniesUnauthorizedActors()
        {
            using var context = GetFreshDbContext();
            var (teacher, _) = await SeedTwoUsersAsync(context);

            var cargoDirector = new TypeTeachingAssignment
            {
                Code = $"DIR_{Guid.NewGuid():N}"[..10],
                Name = "Director",
                Description = "Director de trabajo",
                MaxAssignments = 10,
                StatusRegister = true
            };
            var cargoAsesor = new TypeTeachingAssignment
            {
                Code = $"ASE_{Guid.NewGuid():N}"[..10],
                Name = "Asesor",
                Description = "Asesor",
                MaxAssignments = 15,
                StatusRegister = true
            };
            context.Set<TypeTeachingAssignment>().AddRange(cargoDirector, cargoAsesor);
            await context.SaveChangesAsync();

            var postPayload1 = new AssignTeacherEnabledCargosRequestDto
            {
                UserId = teacher.Id,
                TypeTeachingAssignmentIds = new List<int> { cargoDirector.Id }
            };
            using var postUserAdminReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeacherEnabledCargo/ByUser",
                userId: "9010",
                permissions: PermissionCodes.Configuraciones.N3CFSU,
                body: postPayload1);
            var postUserAdminRes = await _client.SendAsync(postUserAdminReq);

            postUserAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var postUserAdminData = await postUserAdminRes.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            postUserAdminData!.Success.Should().BeTrue();
            postUserAdminData.Data.Should().ContainSingle(c =>
                c.IdTypeTeachingAssignment == cargoDirector.Id &&
                c.CargoCode == cargoDirector.Code);

            using var getUserAdminReq = CreateRequest(
                HttpMethod.Get,
                $"/api/TeacherEnabledCargo/ByUser/{teacher.Id}",
                userId: "9010",
                permissions: PermissionCodes.Configuraciones.N3CFSU);
            var getUserAdminRes = await _client.SendAsync(getUserAdminReq);

            getUserAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getUserAdminData = await getUserAdminRes.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            getUserAdminData!.Data.Should().ContainSingle(c =>
                c.IdTypeTeachingAssignment == cargoDirector.Id &&
                c.CargoCode == cargoDirector.Code);

            var postPayload2 = new AssignTeacherEnabledCargosRequestDto
            {
                UserId = teacher.Id,
                TypeTeachingAssignmentIds = new List<int> { cargoDirector.Id, cargoAsesor.Id }
            };
            using var postRoleAdminReq = CreateRequest(
                HttpMethod.Post,
                "/api/TeacherEnabledCargo/ByUser",
                userId: "9011",
                permissions: PermissionCodes.Configuraciones.N3CFSRL,
                body: postPayload2);
            var postRoleAdminRes = await _client.SendAsync(postRoleAdminReq);

            postRoleAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var postRoleAdminData = await postRoleAdminRes.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            postRoleAdminData!.Success.Should().BeTrue();
            postRoleAdminData.Data.Should().HaveCount(2);
            postRoleAdminData.Data.Should().Contain(c => c.IdTypeTeachingAssignment == cargoDirector.Id && c.CargoCode == cargoDirector.Code);
            postRoleAdminData.Data.Should().Contain(c => c.IdTypeTeachingAssignment == cargoAsesor.Id && c.CargoCode == cargoAsesor.Code);

            using var getRoleAdminReq = CreateRequest(
                HttpMethod.Get,
                $"/api/TeacherEnabledCargo/ByUser/{teacher.Id}",
                userId: "9011",
                permissions: PermissionCodes.Configuraciones.N3CFSRL);
            var getRoleAdminRes = await _client.SendAsync(getRoleAdminReq);

            getRoleAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getRoleAdminData = await getRoleAdminRes.Content.ReadFromJsonAsync<ApiResponse<List<TeacherEnabledCargoDto>>>();
            getRoleAdminData!.Data.Should().HaveCount(2);

            foreach (var deniedPerm in new[] { PermissionCodes.Configuraciones.N2CFP, PermissionCodes.ModalidadesGrado.N3MGPG })
            {
                using var deniedGet = CreateRequest(
                    HttpMethod.Get,
                    $"/api/TeacherEnabledCargo/ByUser/{teacher.Id}",
                    userId: "9012",
                    permissions: deniedPerm);
                using var deniedPost = CreateRequest(
                    HttpMethod.Post,
                    "/api/TeacherEnabledCargo/ByUser",
                    userId: "9012",
                    permissions: deniedPerm,
                    body: postPayload1);

                var deniedGetRes = await _client.SendAsync(deniedGet);
                var deniedPostRes = await _client.SendAsync(deniedPost);

                deniedGetRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                deniedPostRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Fact]
        public async Task TeacherEnabledCargo_ByCargo_AllowsCommitteeAndAdminActors_ReturnsOnlyActiveEnabledTeachers_AndDeniesN2CFP()
        {
            using var context = GetFreshDbContext();
            var (activeTeacher, inactiveTeacher) = await SeedTwoUsersAsync(context);
            inactiveTeacher.StatusRegister = false;
            await context.SaveChangesAsync();

            var targetCargo = new TypeTeachingAssignment
            {
                Code = $"JUR_{Guid.NewGuid():N}"[..10],
                Name = "Jurado",
                Description = "Jurado evaluador",
                MaxAssignments = 10,
                StatusRegister = true
            };
            var otherCargo = new TypeTeachingAssignment
            {
                Code = $"ASE_{Guid.NewGuid():N}"[..10],
                Name = "Asesor",
                Description = "Asesor",
                MaxAssignments = 10,
                StatusRegister = true
            };
            context.Set<TypeTeachingAssignment>().AddRange(targetCargo, otherCargo);
            await context.SaveChangesAsync();

            context.Set<TeacherEnabledCargo>().AddRange(
                new TeacherEnabledCargo { IdUser = activeTeacher.Id, IdTypeTeachingAssignment = targetCargo.Id, StatusRegister = true },
                new TeacherEnabledCargo { IdUser = inactiveTeacher.Id, IdTypeTeachingAssignment = targetCargo.Id, StatusRegister = true });
            await context.SaveChangesAsync();

            var allowedPermissions = new[]
            {
                PermissionCodes.ProyectoGrado.N3PGRP,
                PermissionCodes.ProyectoGrado.N4PGPPA,
                PermissionCodes.ProyectoGrado.N4PGPAA,
                PermissionCodes.PracticaAcademica.N4PAF0PA,
                PermissionCodes.Configuraciones.N3CFSU
            };

            foreach (var perm in allowedPermissions)
            {
                using var req = CreateRequest(
                    HttpMethod.Get,
                    $"/api/TeacherEnabledCargo/ByCargo/{targetCargo.Id}",
                    userId: "9020",
                    permissions: perm);

                var res = await _client.SendAsync(req);

                res.StatusCode.Should().Be(HttpStatusCode.OK);
                var payload = await res.Content.ReadFromJsonAsync<ApiResponse<List<TeacherByCargoDto>>>();
                payload.Should().NotBeNull();
                payload!.Success.Should().BeTrue();
                payload.Data.Should().ContainSingle(t => t.IdUser == activeTeacher.Id);
                payload.Data.Should().NotContain(t => t.IdUser == inactiveTeacher.Id);

                var dto = payload.Data!.Single(t => t.IdUser == activeTeacher.Id);
                dto.FullName.Should().Be($"{activeTeacher.FirstName} {activeTeacher.LastName}");
                dto.Email.Should().Be(activeTeacher.Email);
                dto.IdTypeTeachingAssignment.Should().Be(targetCargo.Id);
            }

            using var deniedReq = CreateRequest(
                HttpMethod.Get,
                $"/api/TeacherEnabledCargo/ByCargo/{targetCargo.Id}",
                userId: "9021",
                permissions: PermissionCodes.Configuraciones.N2CFP);

            var deniedRes = await _client.SendAsync(deniedReq);
            deniedRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task TypeTeachingAssignment_ReadAllowsN3CFSCAndN3CFSU_MutationAllowsN3CFSCAndDeniesN2CFP()
        {
            using var context = GetFreshDbContext();
            var seeded = new TypeTeachingAssignment
            {
                Code = $"TTA_{Guid.NewGuid():N}"[..10],
                Name = "Initial Cargo",
                Description = "Seeded cargo",
                MaxAssignments = 8,
                StatusRegister = true
            };
            context.Set<TypeTeachingAssignment>().Add(seeded);
            await context.SaveChangesAsync();

            foreach (var readPerm in new[] { PermissionCodes.Configuraciones.N3CFSC, PermissionCodes.Configuraciones.N3CFSU })
            {
                using var getAllReq = CreateRequest(HttpMethod.Get, "/api/TypeTeachingAssignment?PageNumber=1&PageSize=10", userId: "9030", permissions: readPerm);
                using var getByIdReq = CreateRequest(HttpMethod.Get, $"/api/TypeTeachingAssignment/{seeded.Id}", userId: "9030", permissions: readPerm);

                var getAllRes = await _client.SendAsync(getAllReq);
                var getByIdRes = await _client.SendAsync(getByIdReq);

                getAllRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var listPayload = await getAllRes.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<TypeTeachingAssignmentDto>>>();
                listPayload!.Data!.Items.Should().Contain(x => x.Id == seeded.Id && x.Code == seeded.Code);

                getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
                var itemPayload = await getByIdRes.Content.ReadFromJsonAsync<ApiResponse<TypeTeachingAssignmentDto>>();
                itemPayload!.Data!.Id.Should().Be(seeded.Id);
                itemPayload.Data.Code.Should().Be(seeded.Code);
                itemPayload.Data.Name.Should().Be(seeded.Name);
            }

            var newDto = new TypeTeachingAssignmentDto
            {
                Code = $"NEW_{Guid.NewGuid():N}"[..10],
                Name = "Created Cargo",
                Description = "Created via N3CFSC",
                MaxAssignments = 12,
                StatusRegister = true
            };
            using var createReq = CreateRequest(
                HttpMethod.Post,
                "/api/TypeTeachingAssignment",
                userId: "9031",
                permissions: PermissionCodes.Configuraciones.N3CFSC,
                body: newDto);
            var createRes = await _client.SendAsync(createReq);

            createRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdPayload = await createRes.Content.ReadFromJsonAsync<ApiResponse<TypeTeachingAssignmentDto>>();
            createdPayload!.Success.Should().BeTrue();
            createdPayload.Data!.Id.Should().BeGreaterThan(0);
            createdPayload.Data.Code.Should().Be(newDto.Code);

            var updateDto = createdPayload.Data with { Name = "Updated Cargo" };
            using var updateReq = CreateRequest(
                HttpMethod.Put,
                $"/api/TypeTeachingAssignment/{createdPayload.Data.Id}",
                userId: "9031",
                permissions: PermissionCodes.Configuraciones.N3CFSC,
                body: updateDto);
            var updateRes = await _client.SendAsync(updateReq);

            updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedPayload = await updateRes.Content.ReadFromJsonAsync<ApiResponse<TypeTeachingAssignmentDto>>();
            updatedPayload!.Success.Should().BeTrue();
            updatedPayload.Data!.Name.Should().Be("Updated Cargo");

            using var deniedPostReq = CreateRequest(
                HttpMethod.Post,
                "/api/TypeTeachingAssignment",
                userId: "9032",
                permissions: PermissionCodes.Configuraciones.N2CFP,
                body: newDto with { Code = $"DEN_{Guid.NewGuid():N}"[..10] });
            using var deniedPutReq = CreateRequest(
                HttpMethod.Put,
                $"/api/TypeTeachingAssignment/{seeded.Id}",
                userId: "9032",
                permissions: PermissionCodes.Configuraciones.N2CFP,
                body: updateDto with { Id = seeded.Id, Code = seeded.Code });

            var deniedPostRes = await _client.SendAsync(deniedPostReq);
            var deniedPutRes = await _client.SendAsync(deniedPutReq);

            deniedPostRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            deniedPutRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private static async Task<(User First, User Second)> SeedTwoUsersAsync(AppDbContext context)
        {
            var idType = new IdentificationType
            {
                Code = $"ID_{Guid.NewGuid():N}"[..8],
                Name = "Cedula",
                Description = "Cedula",
                StatusRegister = true
            };
            var faculty = new Faculty
            {
                Code = $"FAC_{Guid.NewGuid():N}"[..8],
                Name = "Ingenieria",
                StatusRegister = true
            };
            var program = new AcademicProgram
            {
                Code = $"PRG_{Guid.NewGuid():N}"[..8],
                Name = "Sistemas",
                Faculty = faculty,
                StatusRegister = true
            };

            context.Set<IdentificationType>().Add(idType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            await context.SaveChangesAsync();

            var suffix1 = Guid.NewGuid().ToString("N")[..8];
            var suffix2 = Guid.NewGuid().ToString("N")[..8];

            var first = new User
            {
                FirstName = $"First_{suffix1}",
                LastName = "Actor",
                Email = $"user1_{suffix1}@unicesar.edu.co",
                Identification = $"10{suffix1}",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };
            var second = new User
            {
                FirstName = $"Second_{suffix2}",
                LastName = "Actor",
                Email = $"user2_{suffix2}@unicesar.edu.co",
                Identification = $"20{suffix2}",
                IdIdentificationType = idType.Id,
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            context.Set<User>().AddRange(first, second);
            await context.SaveChangesAsync();

            return (first, second);
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            string? userId = null,
            string? permissions = null,
            object? body = null,
            bool anonymous = false)
        {
            var request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            if (anonymous)
            {
                request.Headers.Add("X-Test-Anonymous", "true");
                return request;
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                request.Headers.Add("X-Test-UserId", userId);
            }

            if (permissions != null)
            {
                request.Headers.Add("X-Test-Permissions", string.IsNullOrWhiteSpace(permissions) ? "NONE" : permissions);
            }

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return request;
        }

        [Fact]
        public async Task SelfProfile_UpdateUser_CannotSelfDeactivateOrReactivate()
        {
            using var context = GetFreshDbContext();
            var (selfUser, _) = await SeedTwoUsersAsync(context);

            try
            {
                // 1. Actor activo con N2CFP (sin N3CFSU) intenta auto-desactivarse -> 200 OK pero StatusRegister sigue true
                var deactivateAttemptDto = new UserDto
                {
                    Id = selfUser.Id,
                    IdIdentificationType = selfUser.IdIdentificationType,
                    Identification = selfUser.Identification,
                    Email = selfUser.Email,
                    FirstName = "SelfUpdated",
                    LastName = selfUser.LastName,
                    IdAcademicProgram = selfUser.IdAcademicProgram,
                    StatusRegister = false
                };

                using (var req = CreateRequest(
                    HttpMethod.Put,
                    $"/api/User/{selfUser.Id}",
                    userId: selfUser.Id.ToString(),
                    permissions: PermissionCodes.Configuraciones.N2CFP,
                    body: deactivateAttemptDto))
                {
                    var res = await _client.SendAsync(req);
                    res.StatusCode.Should().Be(HttpStatusCode.OK);

                    var payload = await res.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
                    payload.Should().NotBeNull();
                    payload!.Success.Should().BeTrue();
                    payload.Data.Should().NotBeNull();
                    payload.Data!.StatusRegister.Should().BeTrue();
                    payload.Data.FirstName.Should().Be("SelfUpdated");
                }

                using (var verifyScope = _factory.Services.CreateScope())
                {
                    var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var persisted = await verifyContext.Set<User>().FindAsync(selfUser.Id);
                    persisted.Should().NotBeNull();
                    persisted!.StatusRegister.Should().BeTrue();
                    persisted.FirstName.Should().Be("SelfUpdated");
                }

                // 2. Desactivar usuario en BD; el mismo actor intenta auto-reactivarse -> 403 Forbidden y sigue false
                using (var disableScope = _factory.Services.CreateScope())
                {
                    var disableContext = disableScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var persisted = await disableContext.Set<User>().FindAsync(selfUser.Id);
                    persisted!.StatusRegister = false;
                    await disableContext.SaveChangesAsync();
                }

                var reactivateAttemptDto = deactivateAttemptDto with
                {
                    FirstName = "ReactivateAttempt",
                    StatusRegister = true
                };

                using (var reactivateReq = CreateRequest(
                    HttpMethod.Put,
                    $"/api/User/{selfUser.Id}",
                    userId: selfUser.Id.ToString(),
                    permissions: PermissionCodes.Configuraciones.N2CFP,
                    body: reactivateAttemptDto))
                {
                    var res = await _client.SendAsync(reactivateReq);
                    res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                }

                using (var verifyDisabledScope = _factory.Services.CreateScope())
                {
                    var verifyDisabledContext = verifyDisabledScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var persisted = await verifyDisabledContext.Set<User>().FindAsync(selfUser.Id);
                    persisted.Should().NotBeNull();
                    persisted!.StatusRegister.Should().BeFalse();
                    persisted.FirstName.Should().Be("SelfUpdated");
                }
            }
            finally
            {
                using var restoreScope = _factory.Services.CreateScope();
                var restoreContext = restoreScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = await restoreContext.Set<User>().FindAsync(selfUser.Id);
                if (persisted != null)
                {
                    persisted.StatusRegister = true;
                    await restoreContext.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task UserPermission_GetByUser_IsStrictlyReadOnly_DoesNotSyncRolePermissions()
        {
            using var context = GetFreshDbContext();
            var (targetUser, _) = await SeedTwoUsersAsync(context);

            var suffix = Guid.NewGuid().ToString("N")[..6];
            var role = new Role
            {
                Code = $"ROL_{suffix}",
                Name = $"Role {suffix}",
                Description = "Role sync guard test",
                StatusRegister = true
            };
            var assignedPermission = new Permission
            {
                Code = $"PRM_A_{suffix}",
                Description = "Existing UserPermission",
                StatusRegister = true
            };
            var roleOnlyPermission = new Permission
            {
                Code = $"PRM_R_{suffix}",
                Description = "RolePermission not assigned to user",
                StatusRegister = true
            };

            context.Set<Role>().Add(role);
            context.Set<Permission>().AddRange(assignedPermission, roleOnlyPermission);
            await context.SaveChangesAsync();

            var userRole = new UserRole
            {
                IdUser = targetUser.Id,
                IdRole = role.Id,
                StatusRegister = true
            };
            var existingUserPermission = new UserPermission
            {
                IdUser = targetUser.Id,
                IdPermission = assignedPermission.Id,
                StatusRegister = true
            };
            context.Set<UserRole>().Add(userRole);
            context.Set<UserPermission>().Add(existingUserPermission);
            await context.SaveChangesAsync();

            var tempRolePermission = new RolePermission
            {
                IdRole = role.Id,
                IdPermission = roleOnlyPermission.Id,
                StatusRegister = true
            };

            try
            {
                context.Set<RolePermission>().Add(tempRolePermission);
                await context.SaveChangesAsync();

                using var adminGetReq = CreateRequest(
                    HttpMethod.Get,
                    $"/api/UserPermission/ByUser/{targetUser.Id}",
                    userId: "9901",
                    permissions: PermissionCodes.Configuraciones.N3CFSU);

                var res = await _client.SendAsync(adminGetReq);
                res.StatusCode.Should().Be(HttpStatusCode.OK);

                var payload = await res.Content.ReadFromJsonAsync<ApiResponse<List<UserPermissionInfoDto>>>();
                payload.Should().NotBeNull();
                payload!.Success.Should().BeTrue();
                payload.Data.Should().NotBeNull();
                payload.Data!.Should().ContainSingle(p =>
                    p.UserPermission.Id == existingUserPermission.Id
                    && p.Permission.Id == assignedPermission.Id
                    && p.Permission.Code == assignedPermission.Code);
                payload.Data.Should().NotContain(p =>
                    p.Permission.Id == roleOnlyPermission.Id
                    || p.Permission.Code == roleOnlyPermission.Code);

                using var verifyScope = _factory.Services.CreateScope();
                var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userPermRows = verifyContext.Set<UserPermission>()
                    .Where(up => up.IdUser == targetUser.Id)
                    .ToList();
                userPermRows.Should().ContainSingle();
                userPermRows[0].IdPermission.Should().Be(assignedPermission.Id);
            }
            finally
            {
                using var cleanupScope = _factory.Services.CreateScope();
                var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rpToRemove = await cleanupContext.Set<RolePermission>().FindAsync(tempRolePermission.Id);
                if (rpToRemove != null)
                {
                    cleanupContext.Set<RolePermission>().Remove(rpToRemove);
                    await cleanupContext.SaveChangesAsync();
                }
            }
        }
    }
}
