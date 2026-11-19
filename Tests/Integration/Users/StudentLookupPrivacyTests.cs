using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Responses;
using Application.Shared.DTOs.Users;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Xunit;

namespace Tests.Integration.Users
{
    public class StudentLookupPrivacyTests : IntegrationTestBase
    {
        public StudentLookupPrivacyTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetAllUsers_ForbidsN2MGR_AndAllowsN3CFSU()
        {
            using var context = GetFreshDbContext();
            await SeedActorsAsync(context);

            using var mgrReq = CreateRequest(
                HttpMethod.Get,
                "/api/User?PageNumber=1&PageSize=10",
                userId: "9001",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            using var adminReq = CreateRequest(
                HttpMethod.Get,
                "/api/User?PageNumber=1&PageSize=10",
                userId: "9002",
                permissions: PermissionCodes.Configuraciones.N3CFSU);

            var mgrRes = await _client.SendAsync(mgrReq);
            var adminRes = await _client.SendAsync(adminReq);

            mgrRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            adminRes.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task StudentLookup_ActiveStudent_ReturnsMinimalFiveFieldDtoWithoutSensitiveFields()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedActorsAsync(context);

            using var req = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={Uri.EscapeDataString($"  {seed.ActiveStudent.Identification}  ")}",
                userId: "9003",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            var res = await _client.SendAsync(req);

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var rawJson = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            root.GetProperty("success").GetBoolean().Should().BeTrue();

            var data = root.GetProperty("data");
            var propertyNames = data.EnumerateObject().Select(p => p.Name).ToList();
            propertyNames.Should().BeEquivalentTo(
                [
                    "idIdentificationType",
                    "identification",
                    "firstName",
                    "lastName",
                    "email"
                ]);

            rawJson.ToLowerInvariant().Should().NotContain("phonenumber");
            rawJson.ToLowerInvariant().Should().NotContain("cumulativeaverage");
            rawJson.ToLowerInvariant().Should().NotContain("approvedcredits");
            rawJson.ToLowerInvariant().Should().NotContain("totalacademiccredits");
            rawJson.ToLowerInvariant().Should().NotContain("roles");
            rawJson.ToLowerInvariant().Should().NotContain("observation");

            var parsed = JsonSerializer.Deserialize<ApiResponse<StudentLookupDto>>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            parsed.Should().NotBeNull();
            parsed!.Data.Should().NotBeNull();
            parsed.Data!.IdIdentificationType.Should().Be(seed.ActiveStudent.IdIdentificationType);
            parsed.Data.Identification.Should().Be(seed.ActiveStudent.Identification);
            parsed.Data.FirstName.Should().Be(seed.ActiveStudent.FirstName);
            parsed.Data.LastName.Should().Be(seed.ActiveStudent.LastName);
            parsed.Data.Email.Should().Be(seed.ActiveStudent.Email);
        }

        [Fact]
        public async Task StudentLookup_TeacherAdminOrInactiveStudent_ReturnsNotFound()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedActorsAsync(context);

            using var teacherReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.Teacher.Identification}",
                userId: "9004",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            using var sysAdminReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.SystemAdmin.Identification}",
                userId: "9004",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            using var inactiveReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.InactiveStudent.Identification}",
                userId: "9004",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            using var inactiveStudentRoleReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.InactiveStudentRoleUser.Identification}",
                userId: "9004",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            using var unknownReq = CreateRequest(
                HttpMethod.Get,
                "/api/User/student-lookup?identification=NO-EXISTE-999",
                userId: "9004",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            var teacherRes = await _client.SendAsync(teacherReq);
            var sysAdminRes = await _client.SendAsync(sysAdminReq);
            var inactiveRes = await _client.SendAsync(inactiveReq);
            var inactiveStudentRoleRes = await _client.SendAsync(inactiveStudentRoleReq);
            var unknownRes = await _client.SendAsync(unknownReq);

            teacherRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
            sysAdminRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
            inactiveRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
            inactiveStudentRoleRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
            unknownRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData("/api/User/student-lookup")]
        [InlineData("/api/User/student-lookup?identification=")]
        [InlineData("/api/User/student-lookup?identification=%20%20%20")]
        public async Task StudentLookup_EmptyOrMissingIdentification_ReturnsBadRequest(string url)
        {
            using var context = GetFreshDbContext();
            await SeedActorsAsync(context);

            using var req = CreateRequest(
                HttpMethod.Get,
                url,
                userId: "9005",
                permissions: PermissionCodes.ModalidadesGrado.N2MGR);

            var res = await _client.SendAsync(req);

            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var payload = await res.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("El número de identificación es requerido.");
        }

        [Fact]
        public async Task StudentLookup_WithoutN2MGR_ReturnsForbidden_AndAnonymousReturnsUnauthorized()
        {
            using var context = GetFreshDbContext();
            var seed = await SeedActorsAsync(context);

            using var noPermReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.ActiveStudent.Identification}",
                userId: "9006",
                permissions: PermissionCodes.Configuraciones.N2CFP);

            using var adminOnlyReq = CreateRequest(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.ActiveStudent.Identification}",
                userId: "9007",
                permissions: PermissionCodes.Configuraciones.N3CFSU);

            using var anonReq = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/User/student-lookup?identification={seed.ActiveStudent.Identification}");
            anonReq.Headers.Add("X-Test-Anonymous", "true");

            var noPermRes = await _client.SendAsync(noPermReq);
            var adminOnlyRes = await _client.SendAsync(adminOnlyReq);
            var anonRes = await _client.SendAsync(anonReq);

            noPermRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            adminOnlyRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<(User ActiveStudent, User Teacher, User SystemAdmin, User InactiveStudent, User InactiveStudentRoleUser)> SeedActorsAsync(AppDbContext context)
        {
            var idType = new IdentificationType
            {
                Code = "CC",
                Name = "Cédula",
                Description = "Cédula",
                StatusRegister = true
            };
            var faculty = new Faculty
            {
                Code = "ING",
                Name = "Ingeniería",
                StatusRegister = true
            };
            var program = new AcademicProgram
            {
                Code = "SYS",
                Name = "Ingeniería de Sistemas",
                Faculty = faculty,
                StatusRegister = true
            };
            var studentRole = new Role
            {
                Code = "STUDENT",
                Name = "Estudiante",
                Description = "Estudiante",
                StatusRegister = true
            };
            var teacherRole = new Role
            {
                Code = "TEACHER",
                Name = "Docente",
                Description = "Docente",
                StatusRegister = true
            };
            var adminRole = new Role
            {
                Code = "SYSTEM_ADMINISTRATOR",
                Name = "Administrador del Sistema",
                Description = "Administrador del Sistema",
                StatusRegister = true
            };

            context.Set<IdentificationType>().Add(idType);
            context.Set<Faculty>().Add(faculty);
            context.Set<AcademicProgram>().Add(program);
            context.Set<Role>().AddRange(studentRole, teacherRole, adminRole);
            await context.SaveChangesAsync();

            var activeStudent = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "1001001001",
                FirstName = "Ana",
                LastName = "Pérez",
                Email = "ana.perez@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                PhoneNumber = "3001112233",
                CumulativeAverage = 4.35,
                ApprovedCredits = 120,
                TotalAcademicCredits = 160,
                Observation = "Observación privada",
                StatusRegister = true
            };

            var teacher = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "2002002002",
                FirstName = "Carlos",
                LastName = "Docente",
                Email = "carlos.docente@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            var systemAdmin = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "4004004004",
                FirstName = "Super",
                LastName = "Admin",
                Email = "super.admin@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            var inactiveStudent = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "3003003003",
                FirstName = "Laura",
                LastName = "Inactiva",
                Email = "laura.inactiva@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = false
            };

            var inactiveStudentRoleUser = new User
            {
                IdIdentificationType = idType.Id,
                Identification = "5005005005",
                FirstName = "Mario",
                LastName = "RolInactivo",
                Email = "mario.rolinactivo@unicesar.edu.co",
                IdAcademicProgram = program.Id,
                StatusRegister = true
            };

            context.Set<User>().AddRange(activeStudent, teacher, systemAdmin, inactiveStudent, inactiveStudentRoleUser);
            await context.SaveChangesAsync();

            context.Set<UserRole>().AddRange(
                new UserRole { IdUser = activeStudent.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = teacher.Id, IdRole = teacherRole.Id, StatusRegister = true },
                new UserRole { IdUser = systemAdmin.Id, IdRole = adminRole.Id, StatusRegister = true },
                new UserRole { IdUser = inactiveStudent.Id, IdRole = studentRole.Id, StatusRegister = true },
                new UserRole { IdUser = inactiveStudentRoleUser.Id, IdRole = studentRole.Id, StatusRegister = false });
            await context.SaveChangesAsync();

            return (activeStudent, teacher, systemAdmin, inactiveStudent, inactiveStudentRoleUser);
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            string userId,
            string permissions)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-UserId", userId);
            request.Headers.Add("X-Test-Permissions", permissions);
            return request;
        }
    }
}
