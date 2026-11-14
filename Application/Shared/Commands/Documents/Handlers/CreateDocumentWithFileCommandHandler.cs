using Application.Shared.DTOs.Documents;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Documents;
using Domain.Interfaces.Services.Storage;

namespace Application.Shared.Commands.Documents.Handlers
{
    /// <summary>
    /// Crea una entidad Document y guarda el archivo adjunto usando IFileStorageService.
    /// Puede resolver IdDocumentType por Code si se provee CodeDocumentType en lugar del ID.
    /// Dispara DocumentUploadedEvent vía TryDispatchEvent en los setters de la entidad Document.
    /// </summary>
    public class CreateDocumentWithFileCommandHandler : IRequestHandler<CreateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDocumentAccessGuard _accessGuard;

        public CreateDocumentWithFileCommandHandler(
            IRepository<Document, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorageService,
            IDocumentAccessGuard accessGuard)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
            _accessGuard = accessGuard;
        }

        public async Task<DocumentDto> Handle(CreateDocumentWithFileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            int idDocumentType = dto.IdDocumentType;
            // Si se provee el code, buscar el id correspondiente
            if (!string.IsNullOrWhiteSpace(dto.CodeDocumentType))
            {
                var docTypeRepo = _unitOfWork.GetRepository<DocumentType, int>();
                var docType = await docTypeRepo.GetFirstOrDefaultAsync(x => x.Code == dto.CodeDocumentType, cancellationToken);
                if (docType == null)
                    throw new KeyNotFoundException($"No se encontró DocumentType con code '{dto.CodeDocumentType}'");
                idDocumentType = docType.Id;
            }

            await _accessGuard.EnsureUserCanModifyAsync(
                new Document
                {
                    IdInscriptionModality = dto.IdInscriptionModality,
                    IdDocumentType = idDocumentType
                },
                request.CurrentUser.UserId ?? 0,
                cancellationToken,
                request.CurrentUser.Permissions);

            // Guardar el archivo usando el servicio de almacenamiento
            var uniqueFileName = await _fileStorageService.SaveFileAsync(dto.File.OpenReadStream(), dto.File.FileName, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var entity = new Document
            {
                // Campos de auditoría primero para que el Domain Event capture correctamente al usuario
                IdUserCreatedAt = request.CurrentUser.UserId,
                CreatedAt = now,
                OperationRegister = "INSERT",
                StatusRegister = true,

                // Estas propiedades desencadenan TryDispatchEvent()
                IdInscriptionModality = dto.IdInscriptionModality,
                IdDocumentType = idDocumentType,

                // Demás propiedades
                Name = dto.Name ?? dto.File.FileName,
                OriginalFileName = dto.File.FileName,
                StoredFileName = uniqueFileName,
                StoragePath = string.Empty,
                MimeType = dto.File.ContentType,
                FileSize = dto.File.Length,
                Version = dto.Version ?? "1.0"
            };
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<DocumentDto>(entity);
        }
    }
}
