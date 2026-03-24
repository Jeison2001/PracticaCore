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
        }

        [Fact]
        public async Task GoogleLogin_WithValidToken_ReturnsOkAndToken()
        {
            // Arrange
            var email = "test@unicesar.edu.co";
            var validToken = "valid-google-token";
            
            // Mock Token Validator
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

            // Seed dependencies in the NEW context created by WithWebHostBuilder
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

                // Seed user
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

            var request = new GoogleAuthRequest { IdToken = validToken };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google", request);

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
        public async Task GoogleLogin_WithEmptyToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new GoogleAuthRequest { IdToken = "" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/google", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GoogleLogin_WhenGoogleValidationFails_ReturnsUnauthorized()
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

            var request = new GoogleAuthRequest { IdToken = invalidToken };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/google", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
