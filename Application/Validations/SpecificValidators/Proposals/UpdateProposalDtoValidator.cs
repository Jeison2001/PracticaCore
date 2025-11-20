using Application.Shared.DTOs.Proposals;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Proposals
{
    /// <summary>
    /// Validator for the DTO used in UPDATE operations.
    /// It validates only the fields that are required for an update.
    /// Fields like GeneralObjective and SpecificObjectives are optional in an update
    /// and therefore are not validated here.
    /// </summary>
    public class UpdateProposalDtoValidator : AbstractValidator<ProposalDto>
    {
        public UpdateProposalDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(500).WithMessage("El título no puede exceder los 500 caracteres.");

            RuleFor(x => x.IdResearchLine)
                .NotEmpty().WithMessage("La línea de investigación es obligatoria.");

            RuleFor(x => x.IdResearchSubLine)
                .NotEmpty().WithMessage("La sublínea de investigación es obligatoria.");

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("El identificador de la propuesta es obligatorio.");
        }
    }
}
