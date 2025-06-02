using Application.Shared.Commands;
using Application.Shared.DTOs.Proposal;
using Application.Validations.BaseValidators;
using Domain.Entities;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Proposal
{
    public class UpdateProposalValidator : BaseUpdateCommandValidator<Domain.Entities.Proposal, ProposalDto, int>
    {
        public UpdateProposalValidator()
        {
            // Validación específica para IdStateStage en operaciones UPDATE
            RuleFor(cmd => cmd.Dto.IdStateStage)
                .GreaterThan(0)
                .WithMessage("El estado de la propuesta debe ser un valor válido mayor a 0.");

            // Incluir las validaciones del ProposalValidator para el DTO
            RuleFor(cmd => cmd.Dto)
                .SetValidator(new ProposalValidator());
        }
    }
}
