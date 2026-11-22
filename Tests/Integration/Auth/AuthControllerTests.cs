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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
                assignedRole.IdUserCreatedAt.Should().Be((int)apiResponse.Data.Id);
            }
        }

        [Fact]
        public async Task TokenLogin_WhenUserNotRegistered_ReturnsUnauthorizedWithUsuarioNoRegistradoAndDoesNotAutoCreateUser()
        {
            var unregisteredEmail = $"unregistered_{Guid.NewGuid():N}@unicesar.edu.co";
            const string validToken = "valid-unregistered-token";

            _tokenValidatorMock.Setup(x => x.ValidateAsync(validToken))
                .ReturnsAsync(new TokenPayload
                {
                    Email = $"  {unregisteredEmail.ToUpperInvariant()}  ",
                    GivenName = "Ghost",
                    FamilyName = "Student"
                });

            using var customFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _tokenValidatorMock.Object);
                });
            });

            using var client = customFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/auth/token",
                new TokenAuthRequest { IdToken = validToken, Provider = "google" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().ContainSingle()
                .Which.Should().Be($"USUARIO_NO_REGISTRADO: El correo '{unregisteredEmail}' no se encuentra registrado. Complete primero su registro institucional.");

            using var verifyScope = customFactory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await verifyDb.Users.AnyAsync(u => u.Email == unregisteredEmail)).Should().BeFalse();
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

        [Fact]
        public async Task RefreshTokens_WithInactiveUser_ReturnsUnauthorized_ClearsCookies_AndRevokesAllUserTokens()
        {
            var tokenToUse = $"rt_{Guid.NewGuid():N}";
            var secondaryToken = $"rt_{Guid.NewGuid():N}";
            var userId = await SeedUserWithRefreshTokensAsync(
                userIsActive: false,
                tokenToUse,
                secondaryToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", $"refreshToken={tokenToUse}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Usuario inválido o inactivo.");

            response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
            var cookieList = setCookies!.ToList();
            cookieList.Should().Contain(c => c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().Contain(c => c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().NotContain(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase)
                                             && !c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));

            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingTokens = await verifyContext.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();
            remainingTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task RefreshTokens_WithActiveUser_ReturnsOkAndRotatesTokens()
        {
            var initialToken = $"rt_{Guid.NewGuid():N}";
            var userId = await SeedUserWithRefreshTokensAsync(
                userIsActive: true,
                initialToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", $"refreshToken={initialToken}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().NotBeNull();
            payload.Data!.Token.Should().NotBeNullOrEmpty();
            payload.Data.User.Id.Should().Be(userId);

            response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
            var cookieList = setCookies!.ToList();
            cookieList.Should().Contain(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase)
                                         && !c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().Contain(c => c.StartsWith("accessToken=", StringComparison.OrdinalIgnoreCase)
                                         && !c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));

            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokens = await verifyContext.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();

            tokens.Should().ContainSingle(t => t.Token == initialToken && t.RevokedAt != null && !t.IsActive);
            tokens.Should().ContainSingle(t => t.Token != initialToken && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow);

            // El reúso del token rotado aún vigente debe ser detectado y revocar todas las sesiones del usuario.
            using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            reuseRequest.Headers.Add("X-Test-Anonymous", "true");
            reuseRequest.Headers.Add("Cookie", $"refreshToken={initialToken}");
            reuseRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var reuseResponse = await _client.SendAsync(reuseRequest);
            reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            using var reuseVerifyScope = _factory.Services.CreateScope();
            var reuseVerifyContext = reuseVerifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingAfterReuse = await reuseVerifyContext.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();
            remainingAfterReuse.Should().BeEmpty();
        }

        private async Task<int> SeedUserWithRefreshTokensAsync(bool userIsActive, params string[] refreshTokens)
        {
            using var scope = _factory.Services.CreateScope();
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

            await context.SaveChangesAsync();

            var user = new User
            {
                IdIdentificationType = idType.Id,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"refresh_{Guid.NewGuid():N}@unicesar.edu.co",
                FirstName = "Refresh",
                LastName = "User",
                IdAcademicProgram = program.Id,
                StatusRegister = userIsActive,
                OperationRegister = "SEED"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            foreach (var token in refreshTokens)
            {
                context.RefreshTokens.Add(new RefreshToken
                {
                    IdUser = user.Id,
                    Token = token,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                    StatusRegister = true,
                    OperationRegister = "SEED"
                });
            }
            await context.SaveChangesAsync();

            return user.Id;
        }

        [Fact]
        public async Task Register_WithInvalidAcademicProgram_ReturnsBadRequest()
        {
            int idTypeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var idType = context.Set<IdentificationType>().FirstOrDefault(x => x.StatusRegister)
                    ?? new IdentificationType { Code = "CC", Name = "Cedula", Description = "Cedula", StatusRegister = true };
                if (idType.Id == 0) context.Set<IdentificationType>().Add(idType);

                if (!context.Set<Role>().Any(r => r.Code == "STUDENT" && r.StatusRegister))
                    context.Set<Role>().Add(new Role { Code = "STUDENT", Name = "Estudiante", Description = "Estudiante", StatusRegister = true });

                await context.SaveChangesAsync();
                idTypeId = idType.Id;
            }

            var request = new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"invalidprog_{Guid.NewGuid():N}@unicesar.edu.co",
                FirstName = "Invalid",
                LastName = "Program",
                IdAcademicProgram = 999999,
                RoleId = 0
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task MaxActiveSessions_AfterThreeLogins_EvictsFirstSessionCleanlyWithoutRevokingSecondOrThirdSessions()
        {
            using var customFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:MaxActiveSessions"] = "2"
                    });
                });
            });

            using var client = customFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var email = $"fifo_{Guid.NewGuid():N}@unicesar.edu.co";
            using (var scope = customFactory.Services.CreateScope())
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

                await context.SaveChangesAsync();

                context.Users.Add(new User
                {
                    IdIdentificationType = idType.Id,
                    Identification = Guid.NewGuid().ToString("N")[..10],
                    Email = email,
                    FirstName = "Session",
                    LastName = "Limit",
                    IdAcademicProgram = program.Id,
                    StatusRegister = true,
                    OperationRegister = "SEED"
                });
                await context.SaveChangesAsync();
            }

            static string ExtractRefreshTokenCookie(HttpResponseMessage res)
            {
                res.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
                var cookie = cookies!.FirstOrDefault(c =>
                    c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase)
                    && !c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
                cookie.Should().NotBeNull();
                return cookie!.Split(';', 2)[0]["refreshToken=".Length..];
            }

            var login1 = await client.PostAsJsonAsync("/api/Auth/manual", new ManualAuthRequest { Email = email });
            login1.StatusCode.Should().Be(HttpStatusCode.OK);
            var rt1 = ExtractRefreshTokenCookie(login1);

            var login2 = await client.PostAsJsonAsync("/api/Auth/manual", new ManualAuthRequest { Email = email });
            login2.StatusCode.Should().Be(HttpStatusCode.OK);
            var rt2 = ExtractRefreshTokenCookie(login2);

            var login3 = await client.PostAsJsonAsync("/api/Auth/manual", new ManualAuthRequest { Email = email });
            login3.StatusCode.Should().Be(HttpStatusCode.OK);
            var rt3 = ExtractRefreshTokenCookie(login3);

            // El 1.er refreshToken desplazado por FIFO recibe 401, limpia cookies y NO dispara revocación global por reúso
            using var refresh1Req = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            refresh1Req.Headers.Add("X-Test-Anonymous", "true");
            refresh1Req.Headers.Add("Cookie", $"refreshToken={rt1}");
            refresh1Req.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var refresh1Res = await client.SendAsync(refresh1Req);
            refresh1Res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var refresh1Payload = await refresh1Res.Content.ReadFromJsonAsync<ApiResponse<object>>();
            refresh1Payload.Should().NotBeNull();
            refresh1Payload!.Success.Should().BeFalse();
            refresh1Payload.Errors.Should().Contain("Refresh token inválido.");

            refresh1Res.Headers.TryGetValues("Set-Cookie", out var refresh1Cookies).Should().BeTrue();
            var refresh1CookieList = refresh1Cookies!.ToList();
            refresh1CookieList.Should().Contain(c => c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
            refresh1CookieList.Should().Contain(c => c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));

            // El 2.º y el 3.er login siguen completamente activos (200 OK al refrescar el 2.º y el 3.º)
            using var refresh2Req = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            refresh2Req.Headers.Add("X-Test-Anonymous", "true");
            refresh2Req.Headers.Add("Cookie", $"refreshToken={rt2}");
            refresh2Req.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var refresh2Res = await client.SendAsync(refresh2Req);
            refresh2Res.StatusCode.Should().Be(HttpStatusCode.OK);

            using var refresh3Req = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            refresh3Req.Headers.Add("X-Test-Anonymous", "true");
            refresh3Req.Headers.Add("Cookie", $"refreshToken={rt3}");
            refresh3Req.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var refresh3Res = await client.SendAsync(refresh3Req);
            refresh3Res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RefreshTokens_WithExpiredToken_ReturnsUnauthorizedAndClearsCookies()
        {
            var expiredToken = $"rt_exp_{Guid.NewGuid():N}";
            var userId = await SeedUserWithRefreshTokensAsync(userIsActive: true);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.RefreshTokens.Add(new RefreshToken
                {
                    IdUser = userId,
                    Token = expiredToken,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
                    StatusRegister = true,
                    OperationRegister = "SEED"
                });
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", $"refreshToken={expiredToken}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Refresh token expirado.");

            response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
            var cookieList = setCookies!.ToList();
            cookieList.Should().Contain(c => c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().Contain(c => c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task LogoutAll_Authenticated_RevokesAllUserRefreshTokensAndClearsCookies_WhileAnonymousReturnsUnauthorized()
        {
            var userToken1 = $"rt_{Guid.NewGuid():N}";
            var userToken2 = $"rt_{Guid.NewGuid():N}";
            var otherUserToken = $"rt_{Guid.NewGuid():N}";

            var userId = await SeedUserWithRefreshTokensAsync(userIsActive: true, userToken1, userToken2);
            var otherUserId = await SeedUserWithRefreshTokensAsync(userIsActive: true, otherUserToken);

            // Petición anónima debe ser rechazada con 401
            using var anonRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout-all");
            anonRequest.Headers.Add("X-Test-Anonymous", "true");
            var anonResponse = await _client.SendAsync(anonRequest);
            anonResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Petición autenticada revoca todos los RefreshTokens del usuario y limpia cookies
            using var authRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout-all");
            authRequest.Headers.Add("X-Test-UserId", userId.ToString());
            authRequest.Headers.Add("X-Test-Permissions", "NONE");
            authRequest.Headers.Add("Cookie", $"accessToken=dummy-jwt; refreshToken={userToken1}");
            authRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await _client.SendAsync(authRequest);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeTrue();
            payload.Data.Should().Be("Todas las sesiones han sido cerradas correctamente.");

            response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
            var cookieList = setCookies!.ToList();
            cookieList.Should().Contain(c => c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().Contain(c => c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));

            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var remainingUserTokens = await verifyContext.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == userId)
                .ToListAsync();
            remainingUserTokens.Should().BeEmpty();

            var remainingOtherTokens = await verifyContext.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.IdUser == otherUserId)
                .ToListAsync();
            remainingOtherTokens.Should().ContainSingle(rt => rt.Token == otherUserToken);
        }
    }
}
