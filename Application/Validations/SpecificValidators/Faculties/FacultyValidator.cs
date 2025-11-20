using Application.Shared.DTOs.Faculties;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Faculties
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