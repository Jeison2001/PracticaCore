using Application.Shared.DTOs.Documents;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Storage;

namespace Application.Shared.Commands.Documents.Handlers
{
    public class CreateDocumentWithFileCommandHandler : IRequestHandler<CreateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;

        public CreateDocumentWithFileCommandHandler(
            IRepository<Document, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileStorageService fileStorageService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
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

            // Guardar el archivo usando el servicio de almacenamiento
            var uniqueFileName = await _fileStorageService.SaveFileAsync(dto.File.OpenReadStream(), dto.File.FileName, cancellationToken);

            var now = DateTime.UtcNow;
            var entity = new Document
            {
                // Campos de auditoría primero para que el Domain Event capture correctamente al usuario
                IdUserCreatedAt = dto.IdUserCreatedAt ?? 0,
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