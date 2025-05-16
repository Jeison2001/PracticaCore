using FluentValidation;
using Application.Shared.DTOs.StateWorkGrade;

namespace Application.Validations.specificsValidators.StateWorkGrade
{
    public class StateWorkGradeValidator : AbstractValidator<StateWorkGradeDto>
    {
        public StateWorkGradeValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.").MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.").MaximumLength(255);
        }
    }
}
