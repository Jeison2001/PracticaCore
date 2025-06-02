using Application.Shared.DTOs.Proposal;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Proposal
{
    public class ProposalValidator : AbstractValidator<ProposalDto>
    {
        public ProposalValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(500).WithMessage("El título no puede exceder los 500 caracteres.");

            RuleFor(x => x.IdResearchLine)
                .NotEmpty().WithMessage("La línea de investigación es obligatoria.");

            RuleFor(x => x.IdResearchSubLine)
                .NotEmpty().WithMessage("La sublínea de investigación es obligatoria.");
                
            RuleFor(x => x.IdStateStage)
                .NotEmpty().WithMessage("El estado de la propuesta es obligatorio.");
                
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("La modalidad de inscripción es obligatoria.");
        }
    }
}