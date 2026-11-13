using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.UnitTests.Infrastructure.Services.Auth
{
    public class JwtServiceTests
    {
        private static JwtService CreateJwtService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "SuperSecretKeyForJwtServiceUnitTesting1234567890!",
                    ["Jwt:Issuer"] = "PracticaCoreTest",
                    ["Jwt:Audience"] = "PracticaCoreClient",
                    ["Jwt:ExpireMinutes"] = "60"
                })
                .Build();

            return new JwtService(config);
        }

        [Theory]
        [InlineData("N1PA", "N2PAF0", "N3PAF0P", "N4PAF0PA")]
        [InlineData("N4PAF0PA", "N3PAF0P", "N2PAF0", "N1PA")]
        [InlineData("N1PG", "N2PGSU", "N3PGPSU", "N4PGPSG", "N4PGPSR")]
        [InlineData("N4PGPSR", "N4PGPSG", "N3PGPSU", "N2PGSU", "N1PG")]
        public void GenerateTokenWithClaims_IncludesAllN1ToN4PermissionsRegardlessOfOrdering(params string[] inputPermissions)
        {
            var jwtService = CreateJwtService();

            var token = jwtService.GenerateTokenWithClaims(
                userId: "1",
                email: "user@test.edu.co",
                roles: ["Estudiante"],
                permissions: inputPermissions);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var permissionsClaim = jwt.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value;

            permissionsClaim.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(permissionsClaim!);
            var serialized = doc.RootElement.GetRawText();

            foreach (var code in inputPermissions)
            {
                serialized.Should().Contain($"\"{code}\"");
            }
        }
    }
}
