using Application.Shared.DTOs.Document;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Shared.Commands
{
    public class CreateDocumentWithFileCommandHandler : IRequestHandler<CreateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateDocumentWithFileCommandHandler(
            IRepository<Document, int> repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            }            var entity = new Document
            {
                IdInscriptionModality = dto.IdInscriptionModality,
                IdDocumentType = idDocumentType,
                Name = dto.Name ?? dto.File.FileName,
                OriginalFileName = dto.File.FileName,
                StoredFileName = request.StoredFileName,
                StoragePath = string.Empty, // Ya no se usa
                MimeType = dto.File.ContentType,
                FileSize = dto.File.Length,
                Version = dto.Version,
                IdUserCreatedAt = dto.IdUserCreatedAt,
                CreatedAt = dto.CreatedAt,
                IdUserUpdatedAt = dto.IdUserUpdatedAt,
                UpdatedAt = dto.UpdatedAt,
                OperationRegister = dto.OperationRegister,
                StatusRegister = dto.StatusRegister
            };
            // Asegurar que las fechas sean UTC
            entity.CreatedAt = dto.CreatedAt.Kind == DateTimeKind.Utc
                ? dto.CreatedAt
                : DateTime.SpecifyKind(dto.CreatedAt, DateTimeKind.Utc);
            entity.UpdatedAt = (dto.UpdatedAt ?? DateTime.UtcNow).Kind == DateTimeKind.Utc
                ? (dto.UpdatedAt ?? DateTime.UtcNow)
                : DateTime.SpecifyKind(dto.UpdatedAt ?? DateTime.UtcNow, DateTimeKind.Utc);
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<DocumentDto>(entity);
        }
    }
}
