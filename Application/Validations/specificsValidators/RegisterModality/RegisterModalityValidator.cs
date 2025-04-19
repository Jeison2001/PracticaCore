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

            RuleFor(x => x.IdRegisterModalityState)
                .NotEmpty().WithMessage("El estado de registro de modalidad es requerido.");

            RuleFor(x => x.IdAcademicPeriod)
                .NotEmpty().WithMessage("El período académico es requerido.");
        }
    }
}