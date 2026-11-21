using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Responses;
using Application.Shared.DTOs.Auth;
using Domain.Common.Auth;
using Domain.Entities;
using Domain.Interfaces.Services.Auth;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Integration.Auth
{
    public class CsrfAndAuthHardeningTests : IntegrationTestBase
    {
        public CsrfAndAuthHardeningTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task RefreshTokens_WithCookieWithoutCsrfHeader_ReturnsForbidden()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", "refreshToken=dummy-refresh-token");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Encabezado anti-CSRF requerido para sesiones basadas en cookies.");
        }

        [Fact]
        public async Task RefreshTokens_WithCookieAndDisallowedOrigin_ReturnsForbidden()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", "refreshToken=dummy-refresh-token");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Origin", "https://evil.example");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Origen de petición no permitido (CSRF).");
        }

        [Fact]
        public async Task RefreshTokens_WithCookieAndDisallowedRefererWithoutOrigin_ReturnsForbidden()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", "refreshToken=dummy-refresh-token");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Referer", "https://evil.example/phish");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Origen de petición no permitido (CSRF).");
        }

        [Theory]
        [InlineData("http://localhost:4200", "X-Requested-With", "XMLHttpRequest")]
        [InlineData("http://localhost", "X-CSRF-Protection", "1")]
        public async Task RefreshTokens_WithCookieAllowedOriginAndCsrfHeader_IsNotBlockedByCsrf(
            string origin,
            string csrfHeaderName,
            string csrfHeaderValue)
        {
            var token = $"rt_{Guid.NewGuid():N}";
            await SeedActiveUserWithRefreshTokenAsync(_factory.Services, token);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", $"refreshToken={token}");
            request.Headers.Add("Origin", origin);
            request.Headers.Add(csrfHeaderName, csrfHeaderValue);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RefreshTokens_WithCookieAndBearerAuthorization_BypassesCsrfMiddleware()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", "refreshToken=invalid-token");
            request.Headers.Add("Authorization", "Bearer dummy-jwt");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_WithoutRefreshTokenCookie_WithAccessTokenCookie_ReturnsOkAndClearsBothCookies()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout");
            request.Headers.Add("X-Test-Anonymous", "true");
            request.Headers.Add("Cookie", "accessToken=some-access-jwt");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
            var cookieList = setCookies!.ToList();
            cookieList.Should().Contain(c => c.StartsWith("accessToken=;", StringComparison.OrdinalIgnoreCase));
            cookieList.Should().Contain(c => c.StartsWith("refreshToken=;", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task MutatingRequests_WithAccessTokenCookieWithoutCsrfHeader_ReturnForbidden(string httpMethod)
        {
            var url = httpMethod == "PATCH" ? "/api/AcademicPeriod/1/status" : "/api/AcademicPeriod/1";
            using var request = new HttpRequestMessage(new HttpMethod(httpMethod), url);
            request.Headers.Add("Cookie", "accessToken=some-access-jwt");
            request.Headers.Add("Origin", "http://localhost:4200");

            if (httpMethod == "PUT")
            {
                request.Content = JsonContent.Create(new Application.Shared.DTOs.AcademicPeriods.AcademicPeriodDto
                {
                    Id = 1,
                    Code = "2026-1",
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(6),
                    StatusRegister = true
                });
            }
            else if (httpMethod == "PATCH")
            {
                request.Content = JsonContent.Create(new Application.Shared.DTOs.UpdateStatusRequestDto
                {
                    StatusRegister = true,
                    OperationRegister = "CSRF_TEST"
                });
            }

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Encabezado anti-CSRF requerido para sesiones basadas en cookies.");
        }

        [Theory]
        [InlineData("PUT", "http://localhost:4200")]
        [InlineData("PUT", "http://localhost")]
        [InlineData("PATCH", "http://localhost:4200")]
        [InlineData("PATCH", "http://localhost")]
        [InlineData("DELETE", "http://localhost:4200")]
        [InlineData("DELETE", "http://localhost")]
        public async Task MutatingRequests_WithAccessTokenCookieCsrfHeaderAndAllowedOrSameOrigin_PassCsrfMiddleware(
            string httpMethod,
            string origin)
        {
            var periodId = await SeedAcademicPeriodAsync(_factory.Services);
            var url = httpMethod == "PATCH" ? $"/api/AcademicPeriod/{periodId}/status" : $"/api/AcademicPeriod/{periodId}";

            using var request = new HttpRequestMessage(new HttpMethod(httpMethod), url);
            request.Headers.Add("Cookie", "accessToken=some-access-jwt");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Origin", origin);

            if (httpMethod == "PUT")
            {
                request.Content = JsonContent.Create(new Application.Shared.DTOs.AcademicPeriods.AcademicPeriodDto
                {
                    Id = periodId,
                    Code = $"PER_{Guid.NewGuid():N}"[..10],
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(6),
                    StatusRegister = true
                });
            }
            else if (httpMethod == "PATCH")
            {
                request.Content = JsonContent.Create(new Application.Shared.DTOs.UpdateStatusRequestDto
                {
                    StatusRegister = false,
                    OperationRegister = "CSRF_ALLOWED_TEST"
                });
            }

            var response = await _client.SendAsync(request);

            // PUT/PATCH sobre AcademicPeriod ejecutan el endpoint (200 OK); DELETE no existe en AcademicPeriodController
            // y llega al enrutamiento HTTP (405/404), demostrando en todos los casos que el middleware anti-CSRF no bloqueó con 403.
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
            if (httpMethod is "PUT" or "PATCH")
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            else
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
            }
        }

        [Fact]
        public async Task ProductionEnvironment_ManualLogin_ReturnsNotFound()
        {
            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
            using var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var response = await client.PostAsJsonAsync("/api/Auth/manual", new ManualAuthRequest
            {
                Email = "anyone@unicesar.edu.co"
            });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ProductionEnvironment_TokenLoginAndRefresh_OmitTokenInJsonAndEmitHttpOnlyCookies()
        {
            const string validGoogleToken = "valid-prod-google-token";
            const string email = "produser@unicesar.edu.co";

            var tokenValidatorMock = new Mock<ITokenValidator>();
            tokenValidatorMock.SetupGet(x => x.Provider).Returns("google");
            tokenValidatorMock.Setup(x => x.ValidateAsync(validGoogleToken))
                .ReturnsAsync(new TokenPayload
                {
                    Email = email,
                    GivenName = "Prod",
                    FamilyName = "User"
                });

            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => tokenValidatorMock.Object);
                });
            });
            using var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            await SeedActiveUserAsync(prodFactory.Services, email);

            // 1. TokenLogin en Production omite token en JSON y emite cookies HttpOnly
            var loginRes = await client.PostAsJsonAsync("/api/Auth/token", new TokenAuthRequest
            {
                IdToken = validGoogleToken,
                Provider = "google"
            });

            loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginJson = await loginRes.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(loginJson))
            {
                var dataElement = doc.RootElement.GetProperty("data");
                dataElement.TryGetProperty("token", out _).Should().BeFalse();
            }

            var loginPayload = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(loginJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            loginPayload.Should().NotBeNull();
            loginPayload!.Success.Should().BeTrue();
            loginPayload.Data.Should().NotBeNull();
            loginPayload.Data!.Token.Should().BeNull();
            loginPayload.Data.User.Email.Should().Be(email);

            loginRes.Headers.TryGetValues("Set-Cookie", out var loginCookies).Should().BeTrue();
            var loginCookieList = loginCookies!.ToList();
            var accessCookie = loginCookieList.FirstOrDefault(c => c.StartsWith("accessToken=", StringComparison.OrdinalIgnoreCase));
            var refreshCookie = loginCookieList.FirstOrDefault(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
            accessCookie.Should().NotBeNull();
            accessCookie.Should().ContainEquivalentOf("httponly");
            refreshCookie.Should().NotBeNull();
            refreshCookie.Should().ContainEquivalentOf("httponly");

            var refreshTokenValue = refreshCookie!.Split(';', 2)[0]["refreshToken=".Length..];

            // 2. Refresh en Production omite token en JSON y emite cookies HttpOnly
            using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
            refreshReq.Headers.Add("X-Test-Anonymous", "true");
            refreshReq.Headers.Add("Cookie", $"refreshToken={refreshTokenValue}");
            refreshReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var refreshRes = await client.SendAsync(refreshReq);
            refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var refreshJson = await refreshRes.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(refreshJson))
            {
                var dataElement = doc.RootElement.GetProperty("data");
                dataElement.TryGetProperty("token", out _).Should().BeFalse();
            }

            var refreshPayload = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(refreshJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            refreshPayload.Should().NotBeNull();
            refreshPayload!.Success.Should().BeTrue();
            refreshPayload.Data!.Token.Should().BeNull();

            refreshRes.Headers.TryGetValues("Set-Cookie", out var refreshCookies).Should().BeTrue();
            var refreshCookieList = refreshCookies!.ToList();
            refreshCookieList.Should().Contain(c => c.StartsWith("accessToken=", StringComparison.OrdinalIgnoreCase) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
            refreshCookieList.Should().Contain(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase) && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ProductionEnvironment_Register_NormalizesMixedCaseEmail_AndStaysOpenForStudent()
        {
            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
            using var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var (idTypeId, programId) = await SeedRegistrationCatalogAsync(prodFactory.Services);
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            var rawEmail = $"  MixedCase.{uniqueSuffix}@UniCesar.EDU.CO  ";
            var expectedEmail = $"mixedcase.{uniqueSuffix}@unicesar.edu.co";

            var response = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = rawEmail,
                FirstName = "Student",
                LastName = "Case",
                IdAcademicProgram = programId,
                RoleId = 0
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data!.Email.Should().Be(expectedEmail);

            using (var scope = prodFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persisted = context.Users.Single(u => u.Id == apiResponse.Data.Id);
                persisted.Email.Should().Be(expectedEmail);
            }

            // Reintento con mayúsculas diferentes debe detectar duplicado
            var duplicateResponse = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = expectedEmail.ToUpperInvariant(),
                FirstName = "Student",
                LastName = "Duplicate",
                IdAcademicProgram = programId,
                RoleId = 0
            });

            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SwaggerJson_InProduction_ReturnsNotFound()
        {
            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
            using var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var prodResponse = await client.GetAsync("/swagger/v1/swagger.json");
            prodResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // También deshabilitado en Testing
            var testingResponse = await _client.GetAsync("/swagger/v1/swagger.json");
            testingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task HealthEndpoint_InProduction_ReturnsOkAnonymous()
        {
            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
            using var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("X-Test-Anonymous", "true");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("status").GetString().Should().Be("ok");
        }

        [Fact]
        public async Task SwaggerJson_InDevelopment_ReturnsOk()
        {
            using var devFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<Hangfire.IBackgroundJobClient>());
                });
            });
            using var client = devFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var response = await client.GetAsync("/swagger/v1/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        [Theory]
        [InlineData("user@gmail.com")]
        [InlineData("user@fakeunicesar.edu.co")]
        public async Task Register_WithDisallowedEmailDomain_ReturnsBadRequest(string disallowedEmail)
        {
            var (idTypeId, programId) = await SeedRegistrationCatalogAsync(_factory.Services);

            var response = await _client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = disallowedEmail,
                FirstName = "Student",
                LastName = "External",
                IdAcademicProgram = programId,
                RoleId = 0
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            payload.Should().NotBeNull();
            payload!.Success.Should().BeFalse();
            payload.Errors.Should().Contain("Solo se permite el registro con correos institucionales autorizados (@unicesar.edu.co).");
        }

        [Fact]
        public async Task Register_WithAllowedDomainAndFlatConfigOverride_RespectsConfiguredDomains()
        {
            using var customFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Register:AllowedDomains"] = "otro.edu.co,unicesar.edu.co"
                    });
                });
            });
            using var client = customFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

            var (idTypeId, programId) = await SeedRegistrationCatalogAsync(customFactory.Services);

            var otroResponse = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"est_{Guid.NewGuid():N}@otro.edu.co",
                FirstName = "Student",
                LastName = "Otro",
                IdAcademicProgram = programId,
                RoleId = 0
            });
            otroResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var unicesarResponse = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"est_{Guid.NewGuid():N}@unicesar.edu.co",
                FirstName = "Student",
                LastName = "Unicesar",
                IdAcademicProgram = programId,
                RoleId = 0
            });
            unicesarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var rejectedResponse = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequest
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = $"est_{Guid.NewGuid():N}@gmail.com",
                FirstName = "Student",
                LastName = "Rejected",
                IdAcademicProgram = programId,
                RoleId = 0
            });
            rejectedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var rejectedPayload = await rejectedResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
            rejectedPayload.Should().NotBeNull();
            rejectedPayload!.Success.Should().BeFalse();
            rejectedPayload.Errors.Should().Contain("Solo se permite el registro con correos institucionales autorizados (@otro.edu.co, @unicesar.edu.co).");
        }

        private static async Task<(int idTypeId, int programId)> SeedRegistrationCatalogAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
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

            await context.SaveChangesAsync();
            return (idType.Id, program.Id);
        }

        private static async Task<int> SeedActiveUserAsync(IServiceProvider services, string email)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (idTypeId, programId) = await SeedRegistrationCatalogAsync(services);
            var user = new User
            {
                IdIdentificationType = idTypeId,
                Identification = Guid.NewGuid().ToString("N")[..10],
                Email = email,
                FirstName = "Prod",
                LastName = "User",
                IdAcademicProgram = programId,
                StatusRegister = true,
                OperationRegister = "SEED"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        private static async Task<int> SeedAcademicPeriodAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var period = new AcademicPeriod
            {
                Code = $"CSRF_{Guid.NewGuid():N}"[..12],
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddMonths(6),
                StatusRegister = true,
                OperationRegister = "SEED"
            };
            context.Set<AcademicPeriod>().Add(period);
            await context.SaveChangesAsync();
            return period.Id;
        }

        private static async Task SeedActiveUserWithRefreshTokenAsync(IServiceProvider services, string refreshToken)
        {
            var userId = await SeedActiveUserAsync(services, $"csrf_{Guid.NewGuid():N}@unicesar.edu.co");

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.RefreshTokens.Add(new RefreshToken
            {
                IdUser = userId,
                Token = refreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                StatusRegister = true,
                OperationRegister = "SEED"
            });
            await context.SaveChangesAsync();
        }
    }
}
