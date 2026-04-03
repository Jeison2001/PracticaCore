using Application.Shared.DTOs;
using Application.Common.Services.Jobs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Services.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Shared.Commands.Handlers
{
    /// <summary>
    /// Handler genérico para actualizar una entidad. Restaura campos de auditoría immutables
    /// (CreatedAt, IdUserCreatedAt) después del mapeo de AutoMapper para prevenir sobreescrituras
    /// accidentales.
    /// </summary>
    public class UpdateEntityCommandHandler<T, TId, TDto> : IRequestHandler<UpdateEntityCommand<T, TId, TDto>, TDto>
        where T : BaseEntity<TId>
        where TId : struct
        where TDto : BaseDto<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateEntityCommandHandler<T, TId, TDto>> _logger;

        public UpdateEntityCommandHandler(
            IRepository<T, TId> repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<UpdateEntityCommandHandler<T, TId, TDto>> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TDto> Handle(UpdateEntityCommand<T, TId, TDto> request, CancellationToken ct)
        {
            var existingEntity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Entity with ID {request.Id} not found.");

            // Capturar valores originales de campos inmutables antes del mapeo
            var originalId = existingEntity.Id;
            var originalCreatedAt = existingEntity.CreatedAt;
            var originalIdUserCreatedAt = existingEntity.IdUserCreatedAt;

            // Mapear DTO a entidad
            _mapper.Map(request.Dto, existingEntity);

            // Restaurar campos inmutables - el frontend envía fechas en hora Colombia (Kind=Unspecified)
            existingEntity.Id = originalId;
            // Preservar la fecha de creación inmutable original
            existingEntity.IdUserCreatedAt = originalIdUserCreatedAt;
            existingEntity.CreatedAt = originalCreatedAt;

            // UpdatedAt se genera globalmente
            existingEntity.UpdatedAt = DateTimeOffset.UtcNow;
            existingEntity.IdUserUpdatedAt = request.CurrentUser.UserId;

            await _repository.UpdateAsync(existingEntity);
            await _unitOfWork.CommitAsync(ct);

            return _mapper.Map<TDto>(existingEntity);
        }

    }
}

