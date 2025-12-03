using Application.Shared.DTOs.TeacherResearchProfiles;
using FluentValidation;

namespace Application.Validations.SpecificValidators.TeacherResearchProfiles
{
    public class TeacherResearchProfileValidator : AbstractValidator<TeacherResearchProfileDto>
    {
        public TeacherResearchProfileValidator()
        {
            RuleFor(x => x.IdUser)
                .GreaterThan(0).WithMessage("El ID del usuario debe ser mayor a 0.");

            RuleFor(x => x.IdResearchLine)
                .GreaterThan(0).WithMessage("El ID de la línea de investigación debe ser mayor a 0.");

            RuleFor(x => x.IdResearchSubLine)
                .GreaterThan(0).When(x => x.IdResearchSubLine.HasValue)
                .WithMessage("El ID de la sublínea de investigación debe ser mayor a 0.");

            RuleFor(x => x.ProfileDescription)
                .MaximumLength(2000).WithMessage("La descripción del perfil no puede exceder los 2000 caracteres.");
        }
    }
}
