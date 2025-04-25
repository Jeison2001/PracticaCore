using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Domain
{
    public class IdentificationTypeTests
    {
        [Fact]
        public void IdentificationType_Should_Have_Default_Values()
        {
            // Arrange & Act
            var idType = new IdentificationType();

            // Assert
            Assert.Null(idType.Code);
            Assert.Null(idType.Name);
            Assert.Null(idType.Description);
        }

        [Fact]
        public void IdentificationType_Should_Allow_Setting_Properties()
        {
            // Arrange
            var idType = new IdentificationType();

            // Act
            idType.Code = "CC";
            idType.Name = "Cédula de Ciudadanía";
            idType.Description = "Documento de identidad para ciudadanos colombianos.";

            // Assert
            Assert.Equal("CC", idType.Code);
            Assert.Equal("Cédula de Ciudadanía", idType.Name);
            Assert.Equal("Documento de identidad para ciudadanos colombianos.", idType.Description);
        }
    }
}