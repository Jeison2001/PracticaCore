using FluentValidation;
using Application.Shared.DTOs.StageModality;

namespace Application.Validations.SpecificValidators.StageModality
{
    public class StageModalityValidator : AbstractValidator<StageModalityDto>
    {
        public StageModalityValidator()
        {
            RuleFor(x => x.IdModality)
                .GreaterThan(0).WithMessage("La modalidad es requerida.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(100).WithMessage("El código no puede exceder los 100 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

            RuleFor(x => x.StageOrder)
                .GreaterThan(0).WithMessage("El orden de etapa debe ser mayor a 0.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("La descripción no puede exceder los 1000 caracteres.");
        }
    }
}
