using Domain.Configuration;
using FluentAssertions;
using Google.Apis.Auth;
using Infrastructure.Services.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class GoogleTokenValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WhenEmailNotVerified_ThrowsUnauthorizedAccessException()
        {
            var options = Options.Create(new GoogleAuthOptions
            {
                ClientId = "test-google-client-id"
            });

            var validator = new GoogleTokenValidator(
                options,
                (_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
                {
                    Email = "unverified@unicesar.edu.co",
                    EmailVerified = false,
                    GivenName = "Unverified",
                    FamilyName = "User"
                }));

            var act = () => validator.ValidateAsync("id-token");

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("El correo de Google no está verificado.");
        }

        [Fact]
        public async Task ValidateAsync_WhenEmailVerified_ReturnsTokenPayload()
        {
            var options = Options.Create(new GoogleAuthOptions
            {
                ClientId = "test-google-client-id"
            });

            var validator = new GoogleTokenValidator(
                options,
                (_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
                {
                    Email = "verified@unicesar.edu.co",
                    EmailVerified = true,
                    GivenName = "Verified",
                    FamilyName = "User"
                }));

            var result = await validator.ValidateAsync("id-token");

            result.Email.Should().Be("verified@unicesar.edu.co");
            result.GivenName.Should().Be("Verified");
            result.FamilyName.Should().Be("User");
        }

        [Fact]
        public async Task ValidateAsync_WhenAllowedDomainsConfigured_RejectsDisallowedDomain()
        {
            var options = Options.Create(new GoogleAuthOptions
            {
                ClientId = "test-google-client-id",
                AllowedDomains = new[] { "unicesar.edu.co" }
            });

            var validator = new GoogleTokenValidator(
                options,
                (_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
                {
                    Email = "external@gmail.com",
                    EmailVerified = true,
                    GivenName = "External",
                    FamilyName = "User"
                }));

            var act = () => validator.ValidateAsync("id-token");

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("El dominio de correo no está autorizado para acceder al sistema.");
        }
    }
}
