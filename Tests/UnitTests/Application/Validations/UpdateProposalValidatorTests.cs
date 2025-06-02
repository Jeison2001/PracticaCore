using Application.Shared.Commands;
using Application.Shared.DTOs.Proposal;
using Application.Validations.SpecificValidators.Proposal;
using Domain.Entities;
using Xunit;

namespace Tests.UnitTests.Application.Validations
{
    public class UpdateProposalValidatorTests
    {
        private readonly UpdateProposalValidator _validator;

        public UpdateProposalValidatorTests()
        {
            _validator = new UpdateProposalValidator();
        }

        [Fact]
        public void Should_Have_Error_When_IdStateStage_Is_Zero()
        {
            // Arrange
            var command = new UpdateEntityCommand<Proposal, int, ProposalDto>(
                1,
                new ProposalDto 
                { 
                    Id = 1,
                    Title = "Test Proposal",
                    IdResearchLine = 1,
                    IdResearchSubLine = 1,
                    IdStateStage = 0, // Valor inválido
                    StatusRegister = true
                });

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.IdStateStage");
        }

        [Fact]
        public void Should_Have_Error_When_IdStateStage_Is_Negative()
        {
            // Arrange
            var command = new UpdateEntityCommand<Proposal, int, ProposalDto>(
                1,
                new ProposalDto 
                { 
                    Id = 1,
                    Title = "Test Proposal",
                    IdResearchLine = 1,
                    IdResearchSubLine = 1,
                    IdStateStage = -1, // Valor inválido
                    StatusRegister = true
                });

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.IdStateStage");
        }

        [Fact]
        public void Should_Not_Have_Error_When_IdStateStage_Is_Valid()
        {
            // Arrange
            var command = new UpdateEntityCommand<Proposal, int, ProposalDto>(
                1,
                new ProposalDto 
                { 
                    Id = 1,
                    Title = "Test Proposal",
                    IdResearchLine = 1,
                    IdResearchSubLine = 1,
                    IdStateStage = 1, // Valor válido
                    StatusRegister = true
                });

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Should_Validate_Base_Properties_From_ProposalValidator()
        {
            // Arrange
            var command = new UpdateEntityCommand<Proposal, int, ProposalDto>(
                1,
                new ProposalDto 
                { 
                    Id = 1,
                    Title = "", // Título vacío - debe fallar
                    IdResearchLine = 1,
                    IdResearchSubLine = 1,
                    IdStateStage = 1,
                    StatusRegister = true
                });

            // Act
            var result = _validator.Validate(command);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.Title");
        }
    }
}
