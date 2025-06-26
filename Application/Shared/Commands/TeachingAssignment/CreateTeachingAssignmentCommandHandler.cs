using Application.Shared.DTOs.TeachingAssignment;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Shared.Commands.TeachingAssignment
{
    public class CreateTeachingAssignmentCommandHandler : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
    {
        private readonly ITeachingAssignmentRepository _repository;
        private readonly IRepository<TypeTeachingAssignment, int> _typeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeachingAssignmentCommandHandler(
            ITeachingAssignmentRepository repository,
            IRepository<TypeTeachingAssignment, int> typeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _typeRepository = typeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<TeachingAssignmentDto> Handle(CreateTeachingAssignmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            // Obtener el tipo de asignación para conocer el límite
            var type = await _typeRepository.GetByIdAsync(dto.IdTypeTeachingAssignment);
            if (type == null)
                throw new InvalidOperationException("Tipo de asignación docente no encontrado.");

            if (type.MaxAssignments.HasValue)
            {
                // Contar asignaciones activas para este docente y cargo
                var assignments = await _repository.GetAllAsync(x => x.IdTeacher == dto.IdTeacher && x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment && x.StatusRegister);
                var count = assignments.Count();
                if (count >= type.MaxAssignments.Value)
                    throw new InvalidOperationException($"El docente ya tiene el máximo permitido ({type.MaxAssignments}) de asignaciones activas para este cargo.");
            }

            // 1. Verificar si ya existe una asignación activa para el mismo teacher, inscripcion y cargo
            var existing = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdTeacher == dto.IdTeacher &&
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister,
                cancellationToken);
            if (existing != null)
            {
                // Ya existe exactamente la misma asignación activa, omitir registro (no error, solo return null o el existente)
                return _mapper.Map<TeachingAssignmentDto>(existing);
            }

            // 2. Verificar si existe una asignación activa para la misma inscripcion y cargo pero con otro teacher
            var previous = await _repository.GetFirstOrDefaultAsync(x =>
                x.IdInscriptionModality == dto.IdInscriptionModality &&
                x.IdTypeTeachingAssignment == dto.IdTypeTeachingAssignment &&
                x.StatusRegister &&
                x.IdTeacher != dto.IdTeacher,
                cancellationToken);
            if (previous != null)
            {
                previous.StatusRegister = false;
                await _repository.UpdateAsync(previous);
            }

            var entity = _mapper.Map<Domain.Entities.TeachingAssignment>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.CommitAsync(cancellationToken);
            return _mapper.Map<TeachingAssignmentDto>(entity);
        }
    }
}
