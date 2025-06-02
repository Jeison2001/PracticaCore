using FluentValidation;
using Application.Shared.DTOs.StateStage;

namespace Application.Validations.SpecificValidators.StateStage
{
    public class StateStageValidator : AbstractValidator<StateStageDto>
    {
        public StateStageValidator()
        {
            RuleFor(x => x.IdStageModality)
                .GreaterThan(0).WithMessage("La etapa de modalidad es requerida.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(100).WithMessage("El código no puede exceder los 100 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("La descripción no puede exceder los 1000 caracteres.");
        }
    }
}
