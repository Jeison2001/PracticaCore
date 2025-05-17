using Application.Shared.DTOs.Modality;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Modality
{
    public class ModalityValidator : AbstractValidator<ModalityDto>
    {
        public ModalityValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(150).WithMessage("El código no puede exceder los 150 caracteres.");

            RuleFor(x => x.MaximumTermPeriods)
                .GreaterThanOrEqualTo(0).When(x => x.MaximumTermPeriods.HasValue)
                .WithMessage("El número máximo de periodos debe ser un valor positivo.");

            RuleFor(x => x.MaxStudents)
                .GreaterThan(0).WithMessage("El número máximo de estudiantes debe ser mayor a 0.");
        }
    }
}