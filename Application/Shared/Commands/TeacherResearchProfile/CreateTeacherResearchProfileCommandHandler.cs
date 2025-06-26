using Application.Shared.DTOs.TeacherResearchProfile;
using AutoMapper;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.TeacherResearchProfile
{
    public class CreateTeacherResearchProfileCommandHandler : IRequestHandler<CreateTeacherResearchProfileCommand, TeacherResearchProfileDto>
    {
        private readonly IRepository<Domain.Entities.TeacherResearchProfile, int> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeacherResearchProfileCommandHandler(
            IRepository<Domain.Entities.TeacherResearchProfile, int> repository,
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

            var entity = _mapper.Map<Domain.Entities.TeacherResearchProfile>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeacherResearchProfileDto>(entity);
        }
    }
}
