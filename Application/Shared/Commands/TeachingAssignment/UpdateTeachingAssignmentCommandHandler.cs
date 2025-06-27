using Application.Shared.DTOs.TeachingAssignment;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignment
{
    public class UpdateTeachingAssignmentCommand : IRequest<TeachingAssignmentDto>
    {
        public int Id { get; }
        public TeachingAssignmentDto Dto { get; }
        public UpdateTeachingAssignmentCommand(int id, TeachingAssignmentDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }

    public class UpdateTeachingAssignmentCommandHandler : IRequestHandler<UpdateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TeachingAssignmentDto> Handle(UpdateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
                throw new InvalidOperationException("Asignación docente no encontrada.");

            // Validar duplicidad
            var exists = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTeacher == dto.IdTeacher &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister &&
                x.Id != request.Id,
                cancellationToken);
            if (exists != null)
                return _mapper.Map<TeachingAssignmentDto>(entity);


            // Mapear cambios
            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeachingAssignmentDto>(entity);
        }
    }
}
