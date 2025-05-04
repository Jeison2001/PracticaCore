using Application.Shared.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands
{
    public class CreateDocumentWithFileCommandHandler : IRequestHandler<CreateDocumentWithFileCommand, DocumentDto>
    {
        private readonly IRepository<Document, int> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateDocumentWithFileCommandHandler(IRepository<Document, int> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DocumentDto> Handle(CreateDocumentWithFileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = new Document
            {
                IdInscriptionModality = dto.IdInscriptionModality,
                IdUploader = dto.IdUploader,
                IdDocumentType = dto.IdDocumentType,
                OriginalFileName = dto.File.FileName,
                StoredFileName = request.StoredFileName,
                StoragePath = request.StoragePath,
                MimeType = request.MimeType,
                FileSize = request.FileSize,
                Version = dto.Version,
                DocumentState = "CARGADO",
                IdUserCreatedAt = dto.IdUserCreatedAt,
                CreatedAt = dto.CreatedAt,
                IdUserUpdatedAt = dto.IdUserUpdatedAt,
                UpdatedAt = dto.UpdatedAt,
                OperationRegister = dto.OperationRegister,
                StatusRegister = dto.StatusRegister
            };
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<DocumentDto>(entity);
        }
    }
}
