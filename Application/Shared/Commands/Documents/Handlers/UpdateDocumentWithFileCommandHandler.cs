using Application.Shared.DTOs.Documents;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Storage;

namespace Application.Shared.Commands.Documents.Handlers
{
    /// <summary>
    /// Actualiza metadatos del documento y opcionalmente reemplaza el archivo. Los campos
    /// de auditoría se establecen antes del mapeo para que los Domain Events capturados
    /// por los setters tengan el usuario corrector. Puede resolver IdDocumentType por Code.
    /// </summary>
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

            // Campos de auditoría (asignarlos PRIMERO para que el Domain Event
            // incrustado en los setters capture al usuario que actualiza)
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.OperationRegister = "UPDATE";
            entity.StatusRegister = request.Dto.StatusRegister;
            entity.IdDocumentOld = request.Dto.IdDocumentOld;

            // Actualizar metadatos
            entity.IdInscriptionModality = request.Dto.IdInscriptionModality;
            entity.IdDocumentType = idDocumentType;
            entity.Name = request.Dto.Name ?? entity.Name;
            entity.Version = request.Dto.Version;

            // Prevenir error Npgsql DateTimeKind=Unspecified para este recordtrack

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
