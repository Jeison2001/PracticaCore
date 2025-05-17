using Application.Shared.DTOs.Evaluation;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Evaluation
{
    public class EvaluationValidator : AbstractValidator<EvaluationDto>
    {
        public EvaluationValidator()
        {
            RuleFor(x => x.EntityType).NotEmpty().WithMessage("El tipo de entidad es requerido.")
                .MaximumLength(100).WithMessage("El tipo de entidad no puede tener más de 100 caracteres.");
            
            RuleFor(x => x.EntityId).NotEmpty().WithMessage("El ID de la entidad es requerido.");
            
            RuleFor(x => x.IdEvaluationType).NotEmpty().WithMessage("El tipo de evaluación es requerido.");
            
            RuleFor(x => x.IdEvaluator).NotEmpty().WithMessage("El evaluador es requerido.");
            
            RuleFor(x => x.Result).MaximumLength(100).WithMessage("El resultado no puede tener más de 100 caracteres.");
        }
    }
}