using FluentValidation;

namespace Application.Validations.SpecificValidators.Proposals
{
    public class ProposalValidator : UpdateProposalDtoValidator
    {
        public ProposalValidator()
        {
            // Las validaciones de Title, IdResearchLine, IdResearchSubLine e Id se heredan de UpdateProposalDtoValidator

            RuleFor(x => x.GeneralObjective)
                .NotEmpty().WithMessage("El objetivo general es obligatorio.");

            RuleFor(x => x.SpecificObjectives)
                .NotNull().WithMessage("Debe especificar al menos un objetivo específico.")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("Debe especificar al menos un objetivo específico.");
        }
    }
}