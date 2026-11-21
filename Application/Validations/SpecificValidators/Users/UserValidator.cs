using Application.Shared.DTOs.Users;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Users
{
    public class UserValidator : AbstractValidator<UserDto>
    {
        public UserValidator()
        {
            RuleFor(x => x.IdIdentificationType)
                .GreaterThan(0).WithMessage("El ID del tipo de identificación debe ser mayor a 0.");

            RuleFor(x => x.Identification)
                .NotEmpty().WithMessage("El número de identificación es obligatorio.")
                .MaximumLength(20).WithMessage("El número de identificación no puede exceder los 20 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico debe ser válido.")
                .MaximumLength(255).WithMessage("El correo electrónico no puede exceder los 255 caracteres.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(200).WithMessage("El nombre no puede exceder los 200 caracteres.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(200).WithMessage("El apellido no puede exceder los 200 caracteres.");

            RuleFor(x => x.IdAcademicProgram)
                .GreaterThan(0).WithMessage("El ID del programa académico debe ser mayor a 0.");

            RuleFor(x => x.IdStudyPlan)
                .GreaterThan(0).When(x => x.IdStudyPlan.HasValue)
                .WithMessage("El ID del plan de estudio debe ser mayor a 0.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("El número de teléfono no puede exceder los 20 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.CurrentAcademicPeriod)
                .MaximumLength(20).WithMessage("El período académico actual no puede exceder los 20 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.CurrentAcademicPeriod));

            RuleFor(x => x.CumulativeAverage)
                .InclusiveBetween(0, 5).When(x => x.CumulativeAverage.HasValue)
                .WithMessage("El promedio acumulado debe estar entre 0 y 5.");

            RuleFor(x => x.ApprovedCredits)
                .GreaterThanOrEqualTo(0).When(x => x.ApprovedCredits.HasValue)
                .WithMessage("Los créditos aprobados deben ser mayores o iguales a 0.");

            RuleFor(x => x.TotalAcademicCredits)
                .GreaterThanOrEqualTo(0).When(x => x.TotalAcademicCredits.HasValue)
                .WithMessage("El total de créditos académicos debe ser mayor o igual a 0.");
        }
    }
}