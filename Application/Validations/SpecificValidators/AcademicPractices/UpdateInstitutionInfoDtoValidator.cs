using Application.Shared.DTOs.AcademicPractices;
using FluentValidation;

namespace Application.Validations.SpecificValidators.AcademicPractices
{
    public class UpdateInstitutionInfoDtoValidator : AbstractValidator<UpdateInstitutionInfoDto>
    {
        public UpdateInstitutionInfoDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El ID de la práctica académica es requerido.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("El título es requerido.")
                .MaximumLength(200).WithMessage("El título no puede exceder los 200 caracteres.");

            RuleFor(x => x.InstitutionName)
                .NotEmpty().WithMessage("El nombre de la institución es requerido.")
                .MaximumLength(200).WithMessage("El nombre de la institución no puede exceder los 200 caracteres.");

            RuleFor(x => x.InstitutionContact)
                .NotEmpty().WithMessage("El contacto de la institución es requerido.");

            RuleFor(x => x.PracticeStartDate)
                .NotNull().WithMessage("La fecha de inicio es requerida.");

            RuleFor(x => x.PracticeEndDate)
                .NotNull().WithMessage("La fecha de fin es requerida.")
                .GreaterThan(x => x.PracticeStartDate).When(x => x.PracticeStartDate.HasValue)
                .WithMessage("La fecha de fin debe ser posterior a la fecha de inicio.");

            RuleFor(x => x.PracticeHours)
                .NotNull().WithMessage("Las horas de práctica son requeridas.")
                .GreaterThan(0).WithMessage("Las horas de práctica deben ser mayores a 0.");
        }
    }
}
