using System.Security.Claims;
using Domain.Common.Auth;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class PermissionClaimHelperTests
    {
        [Fact]
        public void ExtractPermissionCodes_NullUserOrNoClaims_ReturnsEmptySet()
        {
            PermissionClaimHelper.ExtractPermissionCodes(null).Should().BeEmpty();

            var emptyPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            PermissionClaimHelper.ExtractPermissionCodes(emptyPrincipal).Should().BeEmpty();
        }

        [Fact]
        public void ExtractPermissionCodes_HybridJwtServiceFormat_ExtractsAllCodes()
        {
            const string hybridJson = """
            {
              "N1PG": ["N2PGP"],
              "N2PGP": [{"N3PGRP": []}],
              "N3PAF0P": [{"N4PAF0PG": []}],
              "N4PGPPC": []
            }
            """;

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("permissions", hybridJson)
            ], "Bearer"));

            var codes = PermissionClaimHelper.ExtractPermissionCodes(principal);

            codes.Should().BeEquivalentTo(["N1PG", "N2PGP", "N3PGRP", "N3PAF0P", "N4PAF0PG", "N4PGPPC"]);
        }

        [Fact]
        public void ExtractPermissionCodes_NestedTreeFormat_ExtractsAllLevels()
        {
            const string treeJson = """
            {
              "N1PG": {
                "N2PGSU": {
                  "N3PGPSU": {
                    "N4PGPSG": [],
                    "N4PGPSR": []
                  },
                  "N3PGCSU": []
                }
              }
            }
            """;

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("permissions", treeJson)
            ], "Bearer"));

            var codes = PermissionClaimHelper.ExtractPermissionCodes(principal);

            codes.Should().BeEquivalentTo(["N1PG", "N2PGSU", "N3PGPSU", "N4PGPSG", "N4PGPSR", "N3PGCSU"]);
        }

        [Fact]
        public void ExtractPermissionCodes_FlatArrayAndSinglePermissionClaim_ExtractsAllCodes()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("permissions", """["N1CF","N2CFS","N3CFSRL"]"""),
                new Claim("permission", """["N3CFACPE"]""")
            ], "Bearer"));

            var codes = PermissionClaimHelper.ExtractPermissionCodes(principal);

            codes.Should().BeEquivalentTo(["N1CF", "N2CFS", "N3CFSRL", "N3CFACPE"]);
        }

        [Fact]
        public void ExtractPermissionCodes_MalformedJson_DoesNotThrowAndSkipsInvalidClaim()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("permissions", "{malformed-json"),
                new Claim("permission", """["N1ED","N2EDD"]""")
            ], "Bearer"));

            var codes = PermissionClaimHelper.ExtractPermissionCodes(principal);

            codes.Should().BeEquivalentTo(["N1ED", "N2EDD"]);
        }
    }
}
