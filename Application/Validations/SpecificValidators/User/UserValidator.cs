using Application.Shared.DTOs.User;
using FluentValidation;

namespace Application.Validations.SpecificValidators.User
{
    public class UserValidator : AbstractValidator<UserDto>
    {
        public UserValidator()
        {
            RuleFor(x => x.IdIdentificationType)
                .GreaterThan(0).WithMessage("El ID del tipo de identificación debe ser mayor a 0.");

            RuleFor(x => x.Identification)
                .NotEmpty().WithMessage("El número de identificación es obligatorio.")
                .MaximumLength(50).WithMessage("El número de identificación no puede exceder los 50 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El correo electrónico debe ser válido.")
                .MaximumLength(100).WithMessage("El correo electrónico no puede exceder los 100 caracteres.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(100).WithMessage("El apellido no puede exceder los 100 caracteres.");

            RuleFor(x => x.IdAcademicProgram)
                .GreaterThan(0).WithMessage("El ID del programa académico debe ser mayor a 0.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("El número de teléfono no puede exceder los 20 caracteres.");

            RuleFor(x => x.CurrentAcademicPeriod)
                .MaximumLength(20).WithMessage("El período académico actual no puede exceder los 20 caracteres.");

            RuleFor(x => x.CumulativeAverage)
                .GreaterThanOrEqualTo(0).WithMessage("El promedio acumulado debe ser mayor o igual a 0.");

            RuleFor(x => x.ApprovedCredits)
                .GreaterThanOrEqualTo(0).WithMessage("Los créditos aprobados deben ser mayores o iguales a 0.");

            RuleFor(x => x.TotalAcademicCredits)
                .GreaterThanOrEqualTo(0).WithMessage("El total de créditos académicos debe ser mayor o igual a 0.");
        }
    }
}