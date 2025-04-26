using Application.Shared.DTOs.RegisterModality;
using FluentValidation;

namespace Application.Validations.specificsValidators.RegisterModality
{
    public class RegisterModalityValidator : AbstractValidator<RegisterModalityDto>
    {
        public RegisterModalityValidator()
        {
            RuleFor(x => x.IdModality)
                .NotEmpty().WithMessage("La modalidad es requerida.");

            RuleFor(x => x.IdStateInscription)
                .NotEmpty().WithMessage("El estado de inscripción es requerido.");

            RuleFor(x => x.IdAcademicPeriod)
                .NotEmpty().WithMessage("El período académico es requerido.");
        }
    }
}