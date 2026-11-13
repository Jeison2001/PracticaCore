using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Auth;
using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Services.Auth;
using FluentAssertions;
using Google.Apis.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Integration.Auth
{
    public class AuthControllerTests : IntegrationTestBase
    {
        private readonly Mock<ITokenValidator> _tokenValidatorMock;

        public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _tokenValidatorMock = new Mock<ITokenValidator>();
            _tokenValidatorMock.SetupGet(x => x.Provider).Returns("google");
        }

        [Fact]
        public async Task TokenLogin_WithValidToken_ReturnsOkAndToken()
        {
            // Arrange
            var email = "test@unicesar.edu.co";
            var validToken = "valid-google-token";
            
            // Simular Token Validator
            var payload = new TokenPayload
            {
                Email = email,
                GivenName = "Test",
                FamilyName = "User"
            };

            _tokenValidatorMock.Setup(x => x.ValidateAsync(validToken))
                .ReturnsAsync(payload);

            var customFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _tokenValidatorMock.Object);
                });
            });

            var client = customFactory.CreateClient();

            // Seed de dependencias en el NUEVO contexto creado por WithWebHostBuilder
            using (var scope = customFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var identificationType = new IdentificationType 
                { 
                    Code = "CC", 
                    Name = "Cedula", 
                    OperationRegister = "INSERT", 
                    StatusRegister = true, 
                    CreatedAt = DateTime.UtcNow 
                };
                context.Set<IdentificationType>().Add(identificationType);
                
                var faculty = new Faculty 
                { 
                    Code = "ING",
                    Name = "Ingenieria", 
                    OperationRegister = "INSERT", 
                    StatusRegister = true, 
                    CreatedAt = DateTime.UtcNow 
                };
                context.Set<Faculty>().Add(faculty);
                await context.SaveChangesAsync();

                var academicProgram = new AcademicProgram 
                { 
                    Code = "SYS", 
                    Name = "Systems", 
                    IdFaculty = faculty.Id, 
                    OperationRegister = "INSERT", 
                    StatusRegister = true, 
                    CreatedAt = DateTime.UtcNow 
                };
                context.Set<AcademicProgram>().Add(academicProgram);
                await context.SaveChangesAsync();

                // Seed de usuario
                var user = new User
                {
                    Email = email,
                    FirstName = "Test",
                    LastName = "User",
                    Identification = "123456789",
                    OperationRegister = "INSERT",
                    StatusRegister = true,
                    CreatedAt = DateTime.UtcNow,
                    IdIdentificationType = identificationType.Id,
                    IdAcademicProgram = academicProgram.Id
                };
                
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            var request = new TokenAuthRequest { IdToken = validToken, Provider = "google" };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
            apiResponse.Data.User.Email.Should().Be(email);
        }

        [Fact]
        public async Task TokenLogin_WithEmptyToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new TokenAuthRequest { IdToken = "" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task TokenLogin_WhenValidationFails_ReturnsUnauthorized()
        {
            // Arrange
            var invalidToken = "invalid-token";
            _tokenValidatorMock.Setup(x => x.ValidateAsync(invalidToken))
                .ThrowsAsync(new InvalidJwtException("Invalid token"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _tokenValidatorMock.Object);
                });
            }).CreateClient();

            var request = new TokenAuthRequest { IdToken = invalidToken, Provider = "google" };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Register_WithZeroRoleId_AssignsStudentRole()
        {
            int idTypeId;
            int programId;
            int studentRoleId;
            string studentRoleName;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var idType = context.Set<IdentificationType>().FirstOrDefault(x => x.StatusRegister)
                    ?? new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
                if (idType.Id == 0) context.Set<IdentificationType>().Add(idType);

                var faculty = context.Set<Faculty>().FirstOrDefault(x => x.StatusRegister)
                    ?? new Faculty { Name = "Fac", Code = "F1", StatusRegister = true };
                if (faculty.Id == 0) context.Set<Faculty>().Add(faculty);

                var program = context.Set<AcademicProgram>().FirstOrDefault(x => x.StatusRegister)
                    ?? new AcademicProgram { Name = "Prog", Code = "P1", Faculty = faculty, StatusRegister = true };
                if (program.Id == 0) context.Set<AcademicProgram>().Add(program);

                var studentRole = context.Set<Role>().FirstOrDefault(r => r.Code == "STUDENT" && r.StatusRegister)
                    ?? new Role { Code = "STUDENT", Name = "Estudiante", Description = "Estudiante", StatusRegister = true };
                if (studentRole.Id == 0) context.Set<Role>().Add(studentRole);

                await context.SaveChangesAsync();
                idTypeId = idType.Id;
                programId = program.Id;
                studentRoleId = studentRole.Id;
                studentRoleName = studentRole.Name;
            }

            var request = new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"reg_{Guid.NewGuid():N}@unicesar.edu.co",
                FirstName = "New",
                LastName = "Student",
                IdAcademicProgram = programId,
                RoleId = 0
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Role.Should().Be(studentRoleName);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var assignedRole = context.Set<UserRole>().FirstOrDefault(ur => ur.IdUser == apiResponse.Data.Id);
                assignedRole.Should().NotBeNull();
                assignedRole!.IdRole.Should().Be(studentRoleId);
            }
        }

        [Fact]
        public async Task Register_WithNonStudentRoleId_ReturnsBadRequest()
        {
            int idTypeId;
            int programId;
            int adminRoleId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var idType = context.Set<IdentificationType>().FirstOrDefault(x => x.StatusRegister)
                    ?? new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
                if (idType.Id == 0) context.Set<IdentificationType>().Add(idType);

                var faculty = context.Set<Faculty>().FirstOrDefault(x => x.StatusRegister)
                    ?? new Faculty { Name = "Fac", Code = "F1", StatusRegister = true };
                if (faculty.Id == 0) context.Set<Faculty>().Add(faculty);

                var program = context.Set<AcademicProgram>().FirstOrDefault(x => x.StatusRegister)
                    ?? new AcademicProgram { Name = "Prog", Code = "P1", Faculty = faculty, StatusRegister = true };
                if (program.Id == 0) context.Set<AcademicProgram>().Add(program);

                if (!context.Set<Role>().Any(r => r.Code == "STUDENT" && r.StatusRegister))
                    context.Set<Role>().Add(new Role { Code = "STUDENT", Name = "Estudiante", Description = "Estudiante", StatusRegister = true });

                var adminRole = context.Set<Role>().FirstOrDefault(r => r.Code == "ADMIN" && r.StatusRegister)
                    ?? new Role { Code = "ADMIN", Name = "Administrador", Description = "Admin", StatusRegister = true };
                if (adminRole.Id == 0) context.Set<Role>().Add(adminRole);

                await context.SaveChangesAsync();
                idTypeId = idType.Id;
                programId = program.Id;
                adminRoleId = adminRole.Id;
            }

            var request = new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"privesc_{Guid.NewGuid():N}@unicesar.edu.co",
                FirstName = "Attacker",
                LastName = "User",
                IdAcademicProgram = programId,
                RoleId = adminRoleId
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
