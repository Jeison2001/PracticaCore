using Application.Shared.DTOs.Document;
using MediatR;

namespace Application.Shared.Commands
{
    public class CreateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public DocumentUploadDto Dto { get; }
        public string StoredFileName { get; }
        public string StoragePath { get; }
        public string MimeType { get; }
        public long FileSize { get; }

        public CreateDocumentWithFileCommand(DocumentUploadDto dto, string storedFileName, string storagePath, string mimeType, long fileSize)
        {
            Dto = dto;
            StoredFileName = storedFileName;
            StoragePath = storagePath;
            MimeType = mimeType;
            FileSize = fileSize;
        }
    }
}
