using FluentValidation;
using Application.Shared.DTOs.StatusWorkGrade;

namespace Application.Validations.specificsValidators.StatusWorkGrade
{
    public class StatusWorkGradeValidator : AbstractValidator<StatusWorkGradeDto>
    {
        public StatusWorkGradeValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.").MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.").MaximumLength(255);
            RuleFor(x => x.IsFinalState).NotNull();
        }
    }
}
