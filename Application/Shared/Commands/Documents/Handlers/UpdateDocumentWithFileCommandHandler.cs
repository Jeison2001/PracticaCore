using Application.Shared.DTOs.Documents;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Storage;

namespace Application.Shared.Commands.Documents.Handlers
{
    public class UpdateDocumentWithFileCommandHandler : IRequestHandler<UpdateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;

        public UpdateDocumentWithFileCommandHandler(
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

        public async Task<DocumentDto> Handle(UpdateDocumentWithFileCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new KeyNotFoundException($"Documento con id {request.Id} no encontrado.");

            int idDocumentType = request.Dto.IdDocumentType;
            if (!string.IsNullOrWhiteSpace(request.Dto.CodeDocumentType))
            {
                var docTypeRepo = _unitOfWork.GetRepository<DocumentType, int>();
                var docType = await docTypeRepo.GetFirstOrDefaultAsync(x => x.Code == request.Dto.CodeDocumentType, cancellationToken);
                if (docType == null)
                    throw new KeyNotFoundException($"No se encontró DocumentType con code '{request.Dto.CodeDocumentType}'");
                idDocumentType = docType.Id;
            }

            // Actualizar metadatos
            entity.IdInscriptionModality = request.Dto.IdInscriptionModality;
            entity.IdDocumentType = idDocumentType;
            entity.Name = request.Dto.Name ?? entity.Name;
            entity.Version = request.Dto.Version;
            entity.IdUserUpdatedAt = request.Dto.IdUserUpdatedAt;
            
            // Asegurar que las fechas sean UTC
            // No actualizamos CreatedAt ya que es inmutable, pero aseguramos su Kind
            if (entity.CreatedAt.Kind == DateTimeKind.Unspecified)
                entity.CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc);

            entity.UpdatedAt = (request.Dto.UpdatedAt ?? DateTime.UtcNow).Kind == DateTimeKind.Utc
                ? (request.Dto.UpdatedAt ?? DateTime.UtcNow)
                : DateTime.SpecifyKind(request.Dto.UpdatedAt ?? DateTime.UtcNow, DateTimeKind.Utc);
            
            entity.OperationRegister = request.Dto.OperationRegister;
            entity.StatusRegister = request.Dto.StatusRegister;
            entity.IdDocumentOld = request.Dto.IdDocumentOld;

            // Si hay archivo nuevo, actualizar info de archivo
            if (request.Dto.File != null)
            {
                var uniqueFileName = await _fileStorageService.SaveFileAsync(request.Dto.File.OpenReadStream(), request.Dto.File.FileName, cancellationToken);
                
                entity.OriginalFileName = request.Dto.File.FileName;
                entity.StoredFileName = uniqueFileName;
                entity.StoragePath = string.Empty;
                entity.MimeType = request.Dto.File.ContentType;
                entity.FileSize = request.Dto.File.Length;
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<DocumentDto>(entity);
        }
    }
}