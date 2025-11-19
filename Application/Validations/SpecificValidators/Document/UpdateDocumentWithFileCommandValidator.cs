using Application.Shared.Commands;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Document
{
    public class UpdateDocumentWithFileCommandValidator : AbstractValidator<UpdateDocumentWithFileCommand>
    {
        public UpdateDocumentWithFileCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new DocumentUpdateDtoValidator());
        }
    }
}
