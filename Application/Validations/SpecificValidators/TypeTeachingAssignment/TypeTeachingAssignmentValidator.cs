using Application.Shared.DTOs.TypeTeachingAssignment;
using FluentValidation;

namespace Application.Validations.SpecificValidators.TypeTeachingAssignment
{
    public class TypeTeachingAssignmentValidator : AbstractValidator<TypeTeachingAssignmentDto>
    {
        public TypeTeachingAssignmentValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("El código es requerido.")
                .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres.");
                
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");
        }
    }
}