using Application.Shared.DTOs.UserRoles;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Commands.UserRoles.Handlers
{
    /// <summary>
    /// Crea el UserRole (dispara UserRoleAssignedEvent → permisos automáticos en el mismo commit)
    /// y, si el rol es TEACHER y el DTO trae cargos habilitados, los asigna vía TeacherEnabledCargo
    /// en la misma transacción. Si se envían cargos para un rol que no es TEACHER → 400.
    /// </summary>
    public class CreateUserRoleWithCargosCommandHandler : IRequestHandler<CreateUserRoleWithCargosCommand, UserRoleDto>
    {
        private readonly IRepository<UserRole, int> _userRoleRepository;
        private readonly IRepository<Role, int> _roleRepository;
        private readonly ITeacherEnabledCargoRepository _teacherEnabledCargoRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserRoleWithCargosCommandHandler(
            IRepository<UserRole, int> userRoleRepository,
            IRepository<Role, int> roleRepository,
            ITeacherEnabledCargoRepository teacherEnabledCargoRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _teacherEnabledCargoRepository = teacherEnabledCargoRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserRoleDto> Handle(CreateUserRoleWithCargosCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;
            var cargoIds = dto.TypeTeachingAssignmentIds;

            // Primera validación (antes de persistir nada): el rol se valida siempre,
            // incluso si el campo de cargos no viene — un docente (TEACHER) no puede
            // nacer sin al menos un cargo habilitado.
            var role = await _roleRepository.GetByIdAsync(dto.IdRole);

            if (cargoIds is { Count: > 0 })
            {
                if (role == null)
                    throw new InvalidOperationException("Rol no encontrado.");

                if (role.Code != "TEACHER")
                    throw new InvalidOperationException("Los cargos habilitados solo aplican a docentes (rol TEACHER).");
            }
            else if (role?.Code == "TEACHER")
            {
                // Campo ausente (null) o lista vacía ([]) con rol docente
                throw new InvalidOperationException("Debe indicar al menos un cargo habilitado para el docente.");
            }

            var entity = _mapper.Map<UserRole>(dto);
            entity.IdUserCreatedAt = request.CurrentUser.UserId;

            await _userRoleRepository.AddAsync(entity);

            if (cargoIds is { Count: > 0 })
            {
                // Ya validado: solo llega aquí con rol TEACHER
                await _teacherEnabledCargoRepository.BulkAssignAsync(
                    dto.IdUser,
                    cargoIds,
                    request.CurrentUser.UserId,
                    cancellationToken);
            }

            // Un solo commit: persiste UserRole (+ evento de permisos) y cargos habilitados de forma atómica
            await _unitOfWork.CommitAsync(cancellationToken);

            return _mapper.Map<UserRoleDto>(entity);
        }
    }
}
