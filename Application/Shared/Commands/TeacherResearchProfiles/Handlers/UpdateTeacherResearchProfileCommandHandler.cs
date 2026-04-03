using Application.Shared.DTOs.TeacherResearchProfiles;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Commands.TeacherResearchProfiles.Handlers
{
    /// <summary>
    /// Actualiza un TeacherResearchProfile. Valida unicidad excluyendo el registro actual
    /// (mismo usuario + línea + subínea). Preserva campos immutables (CreatedAt, IdUserCreatedAt)
    /// y establece UpdatedAt/IdUserUpdatedAt.
    /// </summary>
    public class UpdateTeacherResearchProfileCommandHandler : IRequestHandler<UpdateTeacherResearchProfileCommand, TeacherResearchProfileDto>
    {
        private readonly IRepository<TeacherResearchProfile, int> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeacherResearchProfileCommandHandler(
            IRepository<TeacherResearchProfile, int> repository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TeacherResearchProfileDto> Handle(UpdateTeacherResearchProfileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Validar duplicidad (excluyendo el propio registro)
            var existing = await _repository.GetFirstOrDefaultAsync(
                x => x.IdUser == dto.IdUser && x.IdResearchLine == dto.IdResearchLine && x.IdResearchSubLine == dto.IdResearchSubLine && x.Id != request.Id,
                cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("Ya existe otro perfil de investigación para este docente con la misma línea y sublínea.");

            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new InvalidOperationException("Perfil de investigación no encontrado.");

            // Preservar campos de auditoría inmutables
            var originalCreatedAt = entity.CreatedAt;
            var originalIdUserCreatedAt = entity.IdUserCreatedAt;

            _mapper.Map(dto, entity);

            // Restaurar campos inmutables y asegurar UTC
            entity.CreatedAt = originalCreatedAt;
            entity.IdUserCreatedAt = originalIdUserCreatedAt;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.IdUserUpdatedAt = request.CurrentUser.UserId;

            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeacherResearchProfileDto>(entity);
        }
    }
}
