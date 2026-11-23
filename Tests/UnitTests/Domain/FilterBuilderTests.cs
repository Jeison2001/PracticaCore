using Domain.Common;
using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class FilterBuilderTests
    {
        [Theory]
        [InlineData("José María", "Jose Maria")]
        [InlineData("Gestión Académica", "Gestion Academica")]
        [InlineData("Investigación y Desarrollo", "Investigacion y Desarrollo")]
        [InlineData("Árbol Único Índice Éxito Órgano", "Arbol Unico Indice Exito Organo")]
        [InlineData("Texto normal sin tildes", "Texto normal sin tildes")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void RemoveDiacritics_ShouldStripAccentsCorrectly(string? input, string? expected)
        {
            // Act
            var result = FilterBuilder.RemoveDiacritics(input!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildFilter_WithLikeOperator_ShouldBuildExpression()
        {
            // Arrange
            var filters = new Dictionary<string, string>
            {
                { "firstname@like", "José" }
            };

            // Act
            var filter = FilterBuilder.BuildFilter<User, int>(filters);

            // Assert
            Assert.NotNull(filter);
            var str = filter.ToString();
            Assert.Contains("Contains", str);
        }

        [Fact]
        public void BuildFilter_DateTimeOffset_CalendarDate_AppliesColombiaDayBoundariesInUtc()
        {
            var geExpr = FilterBuilder.BuildFilter<User, int>(new Dictionary<string, string>
            {
                { "createdat@ge", "2026-03-15" }
            })!.Compile();

            var leExpr = FilterBuilder.BuildFilter<User, int>(new Dictionary<string, string>
            {
                { "createdat@le", "2026-03-15" }
            })!.Compile();

            var ltExpr = FilterBuilder.BuildFilter<User, int>(new Dictionary<string, string>
            {
                { "createdat@lt", "2026-03-15" }
            })!.Compile();

            var justBeforeColombiaStart = new User { CreatedAt = new DateTimeOffset(2026, 3, 15, 4, 59, 59, TimeSpan.Zero) };
            var exactColombiaStart = new User { CreatedAt = new DateTimeOffset(2026, 3, 15, 5, 0, 0, TimeSpan.Zero) };
            var exactColombiaEnd = new User { CreatedAt = new DateTimeOffset(2026, 3, 16, 4, 59, 59, 999, TimeSpan.Zero).AddTicks(9999) };
            var nextColombiaDayStart = new User { CreatedAt = new DateTimeOffset(2026, 3, 16, 5, 0, 0, TimeSpan.Zero) };

            Assert.False(geExpr(justBeforeColombiaStart));
            Assert.True(geExpr(exactColombiaStart));

            Assert.True(leExpr(exactColombiaStart));
            Assert.True(leExpr(exactColombiaEnd));
            Assert.False(leExpr(nextColombiaDayStart));

            Assert.True(ltExpr(justBeforeColombiaStart));
            Assert.False(ltExpr(exactColombiaStart));
        }

        [Fact]
        public void BuildFilter_DateTimeOffset_InterpretsNoOffsetAsColombia_AndConvertsExplicitOffsetToUtc()
        {
            var noOffsetFilter = FilterBuilder.BuildFilter<User, int>(new Dictionary<string, string>
            {
                { "createdat@eq", "2026-03-15T23:30:00" }
            })!.Compile();

            var explicitOffsetFilter = FilterBuilder.BuildFilter<User, int>(new Dictionary<string, string>
            {
                { "createdat@eq", "2026-03-15T23:30:00-05:00" }
            })!.Compile();

            var expectedUtc = new User { CreatedAt = new DateTimeOffset(2026, 3, 16, 4, 30, 0, TimeSpan.Zero) };
            var wrongUtc = new User { CreatedAt = new DateTimeOffset(2026, 3, 15, 23, 30, 0, TimeSpan.Zero) };

            Assert.True(noOffsetFilter(expectedUtc));
            Assert.False(noOffsetFilter(wrongUtc));
            Assert.True(explicitOffsetFilter(expectedUtc));
        }
    }
}
