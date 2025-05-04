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

            // Actualizar metadatos
            entity.IdInscriptionModality = request.Dto.IdInscriptionModality;
            entity.IdUploader = request.Dto.IdUploader;
            entity.IdDocumentType = request.Dto.IdDocumentType;
            entity.Version = request.Dto.Version;
            entity.DocumentState = request.Dto.DocumentState ?? entity.DocumentState;
            entity.IdUserUpdatedAt = request.Dto.IdUserUpdatedAt;
            entity.UpdatedAt = request.Dto.UpdatedAt ?? DateTime.UtcNow;
            entity.OperationRegister = request.Dto.OperationRegister;
            entity.StatusRegister = request.Dto.StatusRegister;

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
