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

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("La modalidad de inscripción es obligatoria.");

            RuleFor(x => x.GeneralObjective)
                .NotEmpty().WithMessage("El objetivo general es obligatorio.");

            RuleFor(x => x.SpecificObjectives)
                .NotNull().WithMessage("Debe especificar al menos un objetivo específico.")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("Debe especificar al menos un objetivo específico.");
        }
    }
}