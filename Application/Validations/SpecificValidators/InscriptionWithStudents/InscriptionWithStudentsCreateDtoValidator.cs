using Application.Shared.DTOs.InscriptionWithStudents;
using FluentValidation;

namespace Application.Validations.SpecificValidators.InscriptionWithStudents
{
    public class InscriptionWithStudentsCreateDtoValidator : AbstractValidator<InscriptionWithStudentsCreateDto>
    {
        public InscriptionWithStudentsCreateDtoValidator()
        {
            RuleFor(x => x.InscriptionModality)
                .NotNull().WithMessage("La información de la modalidad es requerida.");

            RuleFor(x => x.InscriptionModality.IdModality)
                .GreaterThan(0).WithMessage("El ID de la modalidad es requerido.");

            RuleFor(x => x.Students)
                .NotEmpty().WithMessage("Debe inscribir al menos un estudiante.")
                .Must(x => x != null && x.Count > 0).WithMessage("La lista de estudiantes no puede estar vacía.");

            RuleForEach(x => x.Students).ChildRules(student =>
            {
                student.RuleFor(s => s.Identification)
                    .NotEmpty().WithMessage("La identificación del estudiante es requerida.");

                student.RuleFor(s => s.IdIdentificationType)
                    .GreaterThan(0).WithMessage("El tipo de identificación es requerido.");
            });
        }
    }
}
