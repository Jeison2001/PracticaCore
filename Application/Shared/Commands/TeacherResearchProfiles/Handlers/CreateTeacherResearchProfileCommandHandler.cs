using Application.Shared.DTOs.TeacherResearchProfiles;
using AutoMapper;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.Shared.Commands.TeacherResearchProfiles.Handlers
{
    /// <summary>
    /// Crea un TeacherResearchProfile tras validar unicidad: mismo usuario + línea de investigación + subínea.
    /// Un docente puede tener múltiples perfiles pero no combinaciones duplicadas de línea/subínea.
    /// </summary>
    public class CreateTeacherResearchProfileCommandHandler : IRequestHandler<CreateTeacherResearchProfileCommand, TeacherResearchProfileDto>
    {
        private readonly IRepository<TeacherResearchProfile, int> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeacherResearchProfileCommandHandler(
            IRepository<TeacherResearchProfile, int> repository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TeacherResearchProfileDto> Handle(CreateTeacherResearchProfileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Validar duplicidad: mismo docente, línea y sublínea (puede ser null)
            var existing = await _repository.GetFirstOrDefaultAsync(
                x => x.IdUser == dto.IdUser && x.IdResearchLine == dto.IdResearchLine && x.IdResearchSubLine == dto.IdResearchSubLine,
                cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un perfil de investigación para este docente con la misma línea y sublínea.");

            var entity = _mapper.Map<TeacherResearchProfile>(dto);
            entity.IdUserCreatedAt = request.CurrentUser.UserId;
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeacherResearchProfileDto>(entity);
        }
    }
}