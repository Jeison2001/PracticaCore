using Application.Shared.DTOs;
using Application.Validations.BaseValidators;
using FluentValidation;


namespace Application.Validations.SpecificValidators.Example {
    public class CreateExampleCommandValidator : BaseCreateCommandValidator<Domain.Entities.Example, ExampleDto, int>
    {
        public CreateExampleCommandValidator(Domain.Interfaces.IRepository<Domain.Entities.Example, int> repository)
        {
            RuleFor(cmd => cmd.Dto.Name)
                .NotEmpty().WithMessage("El nombre es requerido.");

            RuleFor(cmd => cmd.Dto.Code)
                .NotEmpty()
                .MustAsync(async (code, cancellationToken) => {
                    var entity = await repository.GetFirstOrDefaultAsync(x => x.Code == code, cancellationToken);
                    return entity == null;
                }).WithMessage("El codigo debe ser unico");
        }
    }
}
