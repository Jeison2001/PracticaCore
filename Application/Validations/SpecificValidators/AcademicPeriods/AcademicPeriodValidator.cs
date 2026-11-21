using Application.Shared.DTOs.AcademicPeriods;
using FluentValidation;

namespace Application.Validations.SpecificValidators.AcademicPeriods
{
    public class AcademicPeriodValidator : AbstractValidator<AcademicPeriodDto>
    {
        public AcademicPeriodValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del período académico es obligatorio.")
                .MaximumLength(20).WithMessage("El código no puede exceder los 20 caracteres.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("La fecha de fin debe ser mayor o igual a la fecha de inicio.");
        }
    }
}
