using Application.Shared.DTOs.Faculty;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Faculty
{
    public class FacultyValidator : AbstractValidator<FacultyDto>
    {
        public FacultyValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
        }
    }
}