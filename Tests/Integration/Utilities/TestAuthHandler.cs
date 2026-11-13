using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Domain.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Integration.Utilities
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private static readonly string AllPermissionCodesJson = BuildAllPermissionCodesJson();

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("sub", "1"),
                new(ClaimTypes.NameIdentifier, "1"),
                new("email", "test@unicesar.edu.co"),
                new("firstName", "Test"),
                new("lastName", "User")
            };

            if (Request.Headers.TryGetValue("X-Test-Permissions", out var hdr))
            {
                var raw = hdr.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(raw) && !string.Equals(raw, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    var codes = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (codes.Length > 0)
                    {
                        claims.Add(new Claim("permissions", JsonSerializer.Serialize(codes)));
                    }
                }
            }
            else
            {
                claims.Add(new Claim("permissions", AllPermissionCodesJson));
            }

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private static string BuildAllPermissionCodesJson()
        {
            var codes = typeof(PermissionCodes)
                .GetNestedTypes(BindingFlags.Public)
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => f.GetRawConstantValue() as string)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return JsonSerializer.Serialize(codes);
        }
    }
}
