using Application.Shared.DTOs.StateProposal;
using FluentValidation;

namespace Application.Validations.SpecificValidators.StateProposal
{
    public class StateProposalValidator : AbstractValidator<StateProposalDto>
    {
        public StateProposalValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
        }
    }
}