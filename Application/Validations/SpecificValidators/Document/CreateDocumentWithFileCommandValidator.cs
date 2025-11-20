using Application.Shared.Commands.Document;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Document
{
    public class CreateDocumentWithFileCommandValidator : AbstractValidator<CreateDocumentWithFileCommand>
    {
        public CreateDocumentWithFileCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new DocumentUploadDtoValidator());
        }
    }
}
