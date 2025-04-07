using Application.Shared.DTOs;
using Application.Validations.BaseValidators;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Example
{
    public class UpdateExampleCommandValidator
        : BaseUpdateCommandValidator<Domain.Entities.Example, ExampleDto, int>
    {
        public UpdateExampleCommandValidator()
        {    
            // Validaciones específicas para Country
            RuleFor(cmd => cmd.Dto.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
        }
    }
}
