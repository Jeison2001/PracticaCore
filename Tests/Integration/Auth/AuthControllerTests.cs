using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.Shared.DTOs.Auth;
using Domain.Entities;
using Domain.Interfaces.Auth;
using FluentAssertions;
using Google.Apis.Auth;
using Infrastructure.Services.Auth;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Integration.Auth
{
    public class AuthControllerTests : IntegrationTestBase
    {
        private readonly Mock<IGoogleTokenValidator> _googleTokenValidatorMock;

        public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
            _googleTokenValidatorMock = new Mock<IGoogleTokenValidator>();
        }

        [Fact]
        public async Task GoogleLogin_WithValidToken_ReturnsOkAndToken()
        {
            // Arrange
            var email = "test@unicesar.edu.co";
            var validToken = "valid-google-token";
            
            // Seed dependencies
            var identificationType = new IdentificationType 
            { 
                Code = "CC", 
                Name = "Cedula", 
                OperationRegister = "INSERT", 
                StatusRegister = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.Set<IdentificationType>().Add(identificationType);
            
            var faculty = new Faculty 
            { 
                Code = "ING",
                Name = "Ingenieria", 
                OperationRegister = "INSERT", 
                StatusRegister = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.Set<Faculty>().Add(faculty);
            await _context.SaveChangesAsync();

            var academicProgram = new AcademicProgram 
            { 
                Code = "SYS", 
                Name = "Systems", 
                IdFaculty = faculty.Id, 
                OperationRegister = "INSERT", 
                StatusRegister = true, 
                CreatedAt = DateTime.UtcNow 
            };
            _context.Set<AcademicProgram>().Add(academicProgram);
            await _context.SaveChangesAsync();

            // Seed user
            var user = new Domain.Entities.User
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
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Mock Google Validator
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                GivenName = "Test",
                FamilyName = "User",
                EmailVerified = true
            };

            _googleTokenValidatorMock.Setup(x => x.ValidateAsync(validToken))
                .ReturnsAsync(payload);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _googleTokenValidatorMock.Object);
                });
            }).CreateClient();

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
            _googleTokenValidatorMock.Setup(x => x.ValidateAsync(invalidToken))
                .ThrowsAsync(new InvalidJwtException("Invalid token"));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _googleTokenValidatorMock.Object);
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
