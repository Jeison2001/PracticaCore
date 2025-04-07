using Application.Shared.DTOs;
using Application.Validations.BaseValidators;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Example
{
    public class CreateExampleCommandValidator
    : BaseCreateCommandValidator<Domain.Entities.Example, ExampleDto, int>
    {
        public CreateExampleCommandValidator()
        {
            // Regla adicional para Country
            RuleFor(cmd => cmd.Dto.Name)
                .NotEmpty().WithMessage("El nombre es requerido.");
        }
    }
}
