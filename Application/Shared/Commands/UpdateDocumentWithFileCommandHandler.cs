using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands
{
    public class UpdateDocumentWithFileCommandHandler : IRequestHandler<UpdateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateDocumentWithFileCommandHandler(IRepository<Document, int> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            entity.IdUploader = request.Dto.IdUploader;
            entity.IdDocumentType = idDocumentType;
            entity.Name = request.Dto.Name ?? entity.Name;
            entity.Version = request.Dto.Version;
            entity.DocumentState = request.Dto.DocumentState ?? entity.DocumentState;
            entity.IdUserUpdatedAt = request.Dto.IdUserUpdatedAt;
            
            // Asegurar que las fechas sean UTC
            entity.CreatedAt = request.Dto.CreatedAt.Kind == DateTimeKind.Utc
                ? request.Dto.CreatedAt
                : DateTime.SpecifyKind(request.Dto.CreatedAt, DateTimeKind.Utc);
            entity.UpdatedAt = (request.Dto.UpdatedAt ?? DateTime.UtcNow).Kind == DateTimeKind.Utc
                ? (request.Dto.UpdatedAt ?? DateTime.UtcNow)
                : DateTime.SpecifyKind(request.Dto.UpdatedAt ?? DateTime.UtcNow, DateTimeKind.Utc);
            
            entity.OperationRegister = request.Dto.OperationRegister;
            entity.StatusRegister = request.Dto.StatusRegister;
            entity.IdDocumentOld = request.Dto.IdDocumentOld;

            // Si hay archivo nuevo, actualizar info de archivo
            if (request.StoredFileName != null && request.StoragePath != null && request.MimeType != null && request.FileSize != null)
            {
                entity.OriginalFileName = request.Dto.File?.FileName ?? entity.OriginalFileName;
                entity.StoredFileName = request.StoredFileName;
                entity.StoragePath = request.StoragePath;
                entity.MimeType = request.MimeType;
                entity.FileSize = request.FileSize.Value;
            }

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<DocumentDto>(entity);
        }
    }
}
