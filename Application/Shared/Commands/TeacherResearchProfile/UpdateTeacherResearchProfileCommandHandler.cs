using Application.Shared.DTOs.TeacherResearchProfile;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfile
{
    public class UpdateTeacherResearchProfileCommandHandler : IRequestHandler<UpdateTeacherResearchProfileCommand, TeacherResearchProfileDto>
    {
        private readonly IRepository<Domain.Entities.TeacherResearchProfile, int> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeacherResearchProfileCommandHandler(
            IRepository<Domain.Entities.TeacherResearchProfile, int> repository,
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

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeacherResearchProfileDto>(entity);
        }
    }
}
