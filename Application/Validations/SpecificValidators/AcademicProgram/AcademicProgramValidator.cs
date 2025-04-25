using Application.Shared.DTOs.AcademicProgram;
using FluentValidation;

namespace Application.Validations.SpecificValidators.AcademicProgram
{
    public class AcademicProgramValidator : AbstractValidator<AcademicProgramDto>
    {
        public AcademicProgramValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es obligatorio.")
                .MaximumLength(20).WithMessage("El código no puede exceder los 20 caracteres.");

            RuleFor(x => x.IdFaculty)
                .GreaterThan(0).WithMessage("El ID de la facultad debe ser mayor a 0.");
        }
    }
}