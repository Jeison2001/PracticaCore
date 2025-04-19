using Application.Shared.DTOs.AcademicPeriod;
using FluentValidation;

namespace Application.Validations.specificsValidators.AcademicPeriod
{
    public class AcademicPeriodValidator : AbstractValidator<AcademicPeriodDto>
    {
        public AcademicPeriodValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(20).WithMessage("El código no puede exceder los 20 caracteres.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("La fecha de inicio es requerida.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("La fecha de fin es requerida.")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("La fecha de fin debe ser igual o posterior a la fecha de inicio.");
        }
    }
}