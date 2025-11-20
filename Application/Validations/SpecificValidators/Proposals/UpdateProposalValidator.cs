using Application.Shared.DTOs.Proposals;
using Application.Validations.BaseValidators;
using FluentValidation;
using Domain.Entities;

namespace Application.Validations.SpecificValidators.Proposals
{
    public class UpdateProposalValidator : BaseUpdateCommandValidator<Proposal, ProposalDto, int>
    {
        public UpdateProposalValidator()
        {
            // Validación específica para IdStateStage en operaciones UPDATE
            RuleFor(cmd => cmd.Dto.IdStateStage)
                .GreaterThan(0)
                .WithMessage("El estado de la propuesta debe ser un valor válido mayor a 0.");

            // Incluir las validaciones del UpdateProposalDtoValidator para el DTO
            RuleFor(cmd => cmd.Dto)
                .SetValidator(new UpdateProposalDtoValidator());
        }
    }
}