using Application.Shared.DTOs.InscriptionModality;
using FluentValidation;

namespace Application.Validations.SpecificValidators.InscriptionModality
{
    public class InscriptionModalityValidator : AbstractValidator<InscriptionModalityDto>
    {
        public InscriptionModalityValidator()
        {
            RuleFor(x => x.IdModality)
                .NotEmpty().WithMessage("La modalidad es requerida.");

            RuleFor(x => x.IdStateInscription)
                .NotEmpty().WithMessage("El estado de inscripción es requerido.");

            RuleFor(x => x.IdAcademicPeriod)
                .NotEmpty().WithMessage("El período académico es requerido.");
            // IdStageModality es opcional, no se valida como requerido
        }
    }
}