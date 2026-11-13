using Domain.Common.Auth;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class PermissionTreeHelperTests
    {
        private static List<Permission> BuildSampleCatalog() =>
        [
            new Permission { Id = 1, Code = "N1SP", ParentCode = null, StatusRegister = true },
            new Permission { Id = 2, Code = "N2SPC", ParentCode = "N1SP", StatusRegister = true },
            new Permission { Id = 3, Code = "N2SPSG", ParentCode = "N1SP", StatusRegister = true },
            new Permission { Id = 4, Code = "N3SPSGG", ParentCode = "N2SPSG", StatusRegister = true },
            new Permission { Id = 5, Code = "N3SPSGR", ParentCode = "N2SPSG", StatusRegister = true },

            new Permission { Id = 6, Code = "N1PC", ParentCode = null, StatusRegister = true },
            new Permission { Id = 7, Code = "N2PCSG", ParentCode = "N1PC", StatusRegister = true },
            new Permission { Id = 8, Code = "N3PCSGG", ParentCode = "N2PCSG", StatusRegister = true },

            new Permission { Id = 9, Code = "N1MG", ParentCode = null, StatusRegister = true },
            new Permission { Id = 10, Code = "N2MGR", ParentCode = "N1MG", StatusRegister = true },
            new Permission { Id = 11, Code = "N2MGC", ParentCode = "N1MG", StatusRegister = true },

            new Permission { Id = 12, Code = "N1PG", ParentCode = null, StatusRegister = true },
            new Permission { Id = 13, Code = "N2PGSU", ParentCode = "N1PG", StatusRegister = true },
            new Permission { Id = 14, Code = "N3PGPSU", ParentCode = "N2PGSU", StatusRegister = true },
            new Permission { Id = 15, Code = "N4PGPSG", ParentCode = "N3PGPSU", StatusRegister = true },
        ];

        [Fact]
        public void ResolveActiveWithAncestors_ResolvesMultiLevelUpToRoot()
        {
            var catalog = BuildSampleCatalog();

            var result = PermissionTreeHelper.ResolveActiveWithAncestors(catalog, ["N4PGPSG"]);

            result.Select(p => p.Code).Should().BeEquivalentTo(["N4PGPSG", "N3PGPSU", "N2PGSU", "N1PG"]);
        }

        [Fact]
        public void ResolveActiveWithAncestors_HandlesCyclesWithoutInfiniteLoop()
        {
            var cyclic = new List<Permission>
            {
                new() { Id = 1, Code = "A", ParentCode = "B", StatusRegister = true },
                new() { Id = 2, Code = "B", ParentCode = "A", StatusRegister = true },
            };

            var result = PermissionTreeHelper.ResolveActiveWithAncestors(cyclic, ["A", "B"]);

            result.Select(p => p.Code).Should().BeEquivalentTo(["A", "B"]);
        }

        [Fact]
        public void ResolveActiveWithAncestors_StopsAtInactiveAncestor()
        {
            var catalog = new List<Permission>
            {
                new() { Id = 1, Code = "N1X", ParentCode = null, StatusRegister = true },
                new() { Id = 2, Code = "N2X", ParentCode = "N1X", StatusRegister = false },
                new() { Id = 3, Code = "N3X", ParentCode = "N2X", StatusRegister = true },
            };

            var result = PermissionTreeHelper.ResolveActiveWithAncestors(catalog, ["N3X"]);

            result.Select(p => p.Code).Should().BeEquivalentTo(["N3X"]);
        }

        [Fact]
        public void NormalizeActiveLoginPermissions_PrunesEmptyContainersBottomUpAndKeepsActiveBranches()
        {
            var catalog = BuildSampleCatalog();
            // N1PC (6) + N2PCSG (7) sin hojas activas; N1MG (9) + N2MGR (10) + N2MGC (11) activas
            var candidateIds = new[] { 6, 7, 9, 10, 11 };

            var result = PermissionTreeHelper.NormalizeActiveLoginPermissions(catalog, candidateIds);

            result.Select(p => p.Code).Should().BeEquivalentTo(["N1MG", "N2MGR", "N2MGC"]);
        }

        [Fact]
        public void NormalizeActiveLoginPermissions_IncludesMissingAncestorsOfActiveLeaves()
        {
            var catalog = BuildSampleCatalog();
            // Solo hoja N4PGPSG (15) sin N3PGPSU (14), N2PGSU (13), N1PG (12)
            var candidateIds = new[] { 15 };

            var result = PermissionTreeHelper.NormalizeActiveLoginPermissions(catalog, candidateIds);

            result.Select(p => p.Code).Should().BeEquivalentTo(["N1PG", "N2PGSU", "N3PGPSU", "N4PGPSG"]);
        }
    }
}
