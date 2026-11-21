using Application.Shared.DTOs.StudyPlans;
using FluentValidation;

namespace Application.Validations.SpecificValidators.StudyPlans
{
    public class StudyPlanValidator : AbstractValidator<StudyPlanDto>
    {
        public StudyPlanValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código del plan de estudio es obligatorio.")
                .MaximumLength(100).WithMessage("El código no puede exceder los 100 caracteres.");

            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("El nombre no puede exceder los 200 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.IdAcademicProgram)
                .GreaterThan(0).WithMessage("El ID del programa académico debe ser mayor a 0.");

            RuleFor(x => x.StartYear)
                .GreaterThan(1900).WithMessage("El año de inicio debe ser mayor a 1900.");

            RuleFor(x => x.EndYear)
                .GreaterThanOrEqualTo(x => x.StartYear)
                .When(x => x.EndYear.HasValue)
                .WithMessage("El año de fin debe ser mayor o igual al año de inicio.");
        }
    }
}
