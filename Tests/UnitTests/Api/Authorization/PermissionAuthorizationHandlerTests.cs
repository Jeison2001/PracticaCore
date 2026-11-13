using System.Security.Claims;
using Api.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Xunit;

namespace Tests.UnitTests.Api.Authorization
{
    public class PermissionAuthorizationHandlerTests
    {
        private static ClaimsPrincipal BuildUserWithPermissions(string? permissionsJson)
        {
            var claims = new List<Claim> { new("sub", "1") };
            if (permissionsJson != null)
            {
                claims.Add(new Claim("permissions", permissionsJson));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        [Fact]
        public async Task HandleAsync_UserHasAnyRequiredPermission_SucceedsOrSemantics()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionRequirement(["N3PGRP", "N4PGPPG"]);
            var user = BuildUserWithPermissions("""["N1PG","N2PGP","N3PGRP"]""");
            var context = new AuthorizationHandlerContext([requirement], user, resource: null);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_UserLacksRequiredPermission_DoesNotSucceed()
        {
            var handler = new PermissionAuthorizationHandler();
            var requirement = new PermissionRequirement(["N3CFSRL"]);
            var user = BuildUserWithPermissions("""["N1CF","N2CFS","N3CFSU"]""");
            var context = new AuthorizationHandlerContext([requirement], user, resource: null);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAsync_MultipleRequirements_EnforcesAndSemantics()
        {
            var handler = new PermissionAuthorizationHandler();
            var req1 = new PermissionRequirement(["N3CFSRL"]);
            var req2 = new PermissionRequirement(["N3CFACPE"]);

            var partialUser = BuildUserWithPermissions("""["N3CFSRL"]""");
            var partialContext = new AuthorizationHandlerContext([req1, req2], partialUser, resource: null);
            await handler.HandleAsync(partialContext);
            partialContext.HasSucceeded.Should().BeFalse();

            var fullUser = BuildUserWithPermissions("""["N3CFSRL","N3CFACPE"]""");
            var fullContext = new AuthorizationHandlerContext([req1, req2], fullUser, resource: null);
            await handler.HandleAsync(fullContext);
            fullContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public void RequirePermissionAttribute_FormatsPolicyAndValidatesInput()
        {
            var attr = new RequirePermissionAttribute(" N1CF ", "N3CFSRL", "N1CF");
            attr.Policy.Should().Be("Perm:N1CF,N3CFSRL");
            attr.PermissionCodes.Should().Equal("N1CF", "N3CFSRL");

            Action emptyAct = () => _ = new RequirePermissionAttribute(" ", "");
            emptyAct.Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task PermissionPolicyProvider_ResolvesPermPolicyAndDelegatesOthers()
        {
            var options = Options.Create(new AuthorizationOptions());
            options.Value.AddPolicy("CustomPolicy", b => b.RequireClaim("role", "Admin"));
            var provider = new PermissionPolicyProvider(options);

            var permPolicy = await provider.GetPolicyAsync("Perm:N1CF,N3CFSRL");
            permPolicy.Should().NotBeNull();
            permPolicy!.Requirements.OfType<PermissionRequirement>()
                .Should().ContainSingle()
                .Which.RequiredPermissions.Should().Equal("N1CF", "N3CFSRL");

            var customPolicy = await provider.GetPolicyAsync("CustomPolicy");
            customPolicy.Should().NotBeNull();
            customPolicy!.Requirements.OfType<PermissionRequirement>().Should().BeEmpty();
        }
    }
}
