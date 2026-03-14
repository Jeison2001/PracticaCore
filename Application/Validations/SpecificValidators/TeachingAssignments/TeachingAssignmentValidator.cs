using Application.Shared.DTOs.TeachingAssignments;
using FluentValidation;

namespace Application.Validations.SpecificValidators.TeachingAssignments
{
    public class TeachingAssignmentValidator : AbstractValidator<TeachingAssignmentDto>
    {
        public TeachingAssignmentValidator()
        {
            RuleFor(x => x.IdInscriptionModality)
                .GreaterThan(0).WithMessage("Debe seleccionar una modalidad de inscripción.");
                
            RuleFor(x => x.IdTeacher)
                .GreaterThan(0).WithMessage("Debe seleccionar un profesor.");
                
            RuleFor(x => x.IdTypeTeachingAssignment)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de asignación docente.");
                                
            RuleFor(x => x.RevocationDate)
                .GreaterThan(x => x.CreatedAt)
                .When(x => x.RevocationDate.HasValue)
                .WithMessage("La fecha de revocación debe ser posterior a la fecha de asignación.");
            
            RuleFor(x => x.IdUserCreatedAt)
                .GreaterThan(0).WithMessage("UserCreate es obligatorio.");
        }
    }
}