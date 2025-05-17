using Application.Shared.DTOs.StateProjectFinal;
using FluentValidation;

namespace Application.Validations.SpecificValidators.StateProjectFinal
{
    public class StateProjectFinalValidator : AbstractValidator<StateProjectFinalDto>
    {
        public StateProjectFinalValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.");
        }
    }
}
