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
        }
    }
}
