using Application.Shared.DTOs.EvaluationTypes;
using FluentValidation;

namespace Application.Validations.SpecificValidators.EvaluationTypes
{
    public class EvaluationTypeValidator : AbstractValidator<EvaluationTypeDto>
    {
        public EvaluationTypeValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(100).WithMessage("El código no puede tener más de 100 caracteres.");
            
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(255).WithMessage("El nombre no puede tener más de 255 caracteres.");
        }
    }
}