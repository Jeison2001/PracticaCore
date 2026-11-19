using System.Security.Cryptography;
using Domain.Configuration;
using FluentAssertions;
using Infrastructure.Services.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class AzureAdTokenValidatorTests
    {
        private static (string token, OpenIdConnectConfiguration oidcConfig) CreateSignedAzureToken(
            string tid,
            string clientId,
            string email = "azureuser@unicesar.edu.co")
        {
            using var rsa = RSA.Create(2048);
            var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = "test-kid" };
            var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var handler = new JsonWebTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = $"https://login.microsoftonline.com/{tid}/v2.0",
                Audience = clientId,
                Claims = new Dictionary<string, object>
                {
                    ["tid"] = tid,
                    ["email"] = email,
                    ["given_name"] = "Azure",
                    ["family_name"] = "User"
                },
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = credentials
            };

            var token = handler.CreateToken(descriptor);

            var publicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "test-kid" };
            var oidcConfig = new OpenIdConnectConfiguration
            {
                Issuer = $"https://login.microsoftonline.com/{tid}/v2.0"
            };
            oidcConfig.SigningKeys.Add(publicKey);

            return (token, oidcConfig);
        }

        [Fact]
        public async Task ValidateAsync_MultiTenantWithAllowedTenantIds_RejectsDisallowedTenant()
        {
            const string clientId = "azure-client-id";
            var (token, oidcConfig) = CreateSignedAzureToken("disallowed-tenant-guid", clientId);

            var configManagerMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
            configManagerMock
                .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(oidcConfig);

            var validator = new AzureAdTokenValidator(
                Options.Create(new AzureAdAuthOptions
                {
                    TenantId = "common",
                    ClientId = clientId,
                    AllowedTenantIds = new[] { "allowed-tenant-guid" }
                }),
                configManagerMock.Object);

            var act = () => validator.ValidateAsync(token);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Tenant de Azure AD no autorizado.");
        }

        [Fact]
        public async Task ValidateAsync_MultiTenantWithAllowedTenantIds_AllowsMatchingTenantCaseInsensitive()
        {
            const string clientId = "azure-client-id";
            var (token, oidcConfig) = CreateSignedAzureToken("allowed-tenant-guid", clientId);

            var configManagerMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
            configManagerMock
                .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(oidcConfig);

            var validator = new AzureAdTokenValidator(
                Options.Create(new AzureAdAuthOptions
                {
                    TenantId = "common",
                    ClientId = clientId,
                    AllowedTenantIds = new[] { "ALLOWED-TENANT-GUID" }
                }),
                configManagerMock.Object);

            var payload = await validator.ValidateAsync(token);

            payload.Email.Should().Be("azureuser@unicesar.edu.co");
            payload.GivenName.Should().Be("Azure");
            payload.FamilyName.Should().Be("User");
        }
    }
}
