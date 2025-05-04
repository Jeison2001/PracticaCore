using Application.Shared.DTOs;
using MediatR;

namespace Application.Shared.Commands
{
    public class UpdateDocumentWithFileCommand : IRequest<DocumentDto>
    {
        public int Id { get; }
        public DocumentUpdateDto Dto { get; }
        public string? StoredFileName { get; }
        public string? StoragePath { get; }
        public string? MimeType { get; }
        public long? FileSize { get; }

        public UpdateDocumentWithFileCommand(int id, DocumentUpdateDto dto, string? storedFileName, string? storagePath, string? mimeType, long? fileSize)
        {
            Id = id;
            Dto = dto;
            StoredFileName = storedFileName;
            StoragePath = storagePath;
            MimeType = mimeType;
            FileSize = fileSize;
        }
    }
}
