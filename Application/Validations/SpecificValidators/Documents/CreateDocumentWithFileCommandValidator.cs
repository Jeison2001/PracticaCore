using Application.Shared.Commands.Documents;
using FluentValidation;

namespace Application.Validations.SpecificValidators.Documents
{
    public class CreateDocumentWithFileCommandValidator : AbstractValidator<CreateDocumentWithFileCommand>
    {
        public CreateDocumentWithFileCommandValidator()
        {
            RuleFor(x => x.Dto).SetValidator(new DocumentUploadDtoValidator());
        }
    }
}
