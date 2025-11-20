using Application.Shared.Commands.Documents;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Documents
{
    public class UpdateDocumentWithFileCommandValidator : AbstractValidator<UpdateDocumentWithFileCommand>
    {
        public UpdateDocumentWithFileCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new DocumentUpdateDtoValidator());
        }
    }
}
