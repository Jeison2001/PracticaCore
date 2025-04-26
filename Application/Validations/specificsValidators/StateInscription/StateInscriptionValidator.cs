// filepath: c:\Users\LENOVO\source\repos\PracticaCore\Application\Validations\specificsValidators\StateInscription\StateInscriptionValidator.cs
using Application.Shared.DTOs.StateInscription;
using FluentValidation;

namespace Application.Validations.specificsValidators.StateInscription
{
    public class StateInscriptionValidator : AbstractValidator<StateInscriptionDto>
    {
        public StateInscriptionValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
        }
    }
}