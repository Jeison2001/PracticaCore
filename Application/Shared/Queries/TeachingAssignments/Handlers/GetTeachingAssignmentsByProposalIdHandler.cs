using Application.Shared.DTOs.TeachingAssignments;
using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Shared.Queries.TeachingAssignments.Handlers
{
    public class GetTeachingAssignmentsByProposalIdHandler : IRequestHandler<GetTeachingAssignmentsByProposalIdQuery, List<TeachingAssignmentTeacherDto>>
    {
        private static readonly HashSet<string> GlobalAdminReadPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            PermissionCodes.Configuraciones.N3CFSU,
            PermissionCodes.Configuraciones.N2CFDO,
            PermissionCodes.Configuraciones.N3CFDOT,
            PermissionCodes.ModalidadesGrado.N3MGPR
        };

        private static readonly Dictionary<string, IReadOnlySet<string>> ModalityManagementPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            [ModalityCodes.ProyectoGrado] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.ProyectoGrado.N4PGPPG,
                PermissionCodes.ProyectoGrado.N4PGPPR,
                PermissionCodes.ProyectoGrado.N4PGPPA,
                PermissionCodes.ProyectoGrado.N4PGPAG,
                PermissionCodes.ProyectoGrado.N4PGPAA,
                PermissionCodes.ProyectoGrado.N4PGPPRG,
                PermissionCodes.ProyectoGrado.N4PGPSG
            },
            [ModalityCodes.PracticaAcademica] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.PracticaAcademica.N4PAF0PG,
                PermissionCodes.PracticaAcademica.N4PAF0PA,
                PermissionCodes.PracticaAcademica.N4PAF0PR,
                PermissionCodes.PracticaAcademica.N4PAF1PG,
                PermissionCodes.PracticaAcademica.N4PAF2PG
            },
            [ModalityCodes.CoTerminal] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.CoTerminal.N3CTSGG,
                PermissionCodes.CoTerminal.N3CTSGR
            },
            ["CO_TERMINAL"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.CoTerminal.N3CTSGG,
                PermissionCodes.CoTerminal.N3CTSGR
            },
            [ModalityCodes.SeminarioAct] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.Seminario.N3SASGG,
                PermissionCodes.Seminario.N3SASGR
            },
            [ModalityCodes.PublicacionArticulo] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.PublicacionArticulo.N3PCSGG,
                PermissionCodes.PublicacionArticulo.N3PCSGR
            },
            [ModalityCodes.GradoPromedio] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.GradoPromedio.N3GPSGG,
                PermissionCodes.GradoPromedio.N3GPSGR
            },
            [ModalityCodes.SaberPro] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionCodes.SaberPro.N3SPSGG,
                PermissionCodes.SaberPro.N3SPSGR
            }
        };

        private static readonly HashSet<string> AnyModalityManagementPermissions = new(
            ModalityManagementPermissions.Values.SelectMany(v => v),
            StringComparer.OrdinalIgnoreCase);

        private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public GetTeachingAssignmentsByProposalIdHandler(
            ITeachingAssignmentRepository teachingAssignmentRepository,
            IUnitOfWork unitOfWork)
        {
            _teachingAssignmentRepository = teachingAssignmentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TeachingAssignmentTeacherDto>> Handle(GetTeachingAssignmentsByProposalIdQuery request, CancellationToken cancellationToken)
        {
            await EnsureUserCanReadInscriptionAssignmentsAsync(request.ProposalId, request.CurrentUser, cancellationToken);

            var assignmentsWithDetails = await _teachingAssignmentRepository.GetTeachingAssignmentsByProposalWithDetailsAsync(
                request.ProposalId,
                request.StatusRegister,
                cancellationToken
            );
            var result = assignmentsWithDetails.Select(detail => new TeachingAssignmentTeacherDto
            {
                Id = detail.TeachingAssignment.Id,
                IdUser = detail.TeachingAssignment.IdTeacher,
                FullName = detail.Teacher != null ? $"{detail.Teacher.FirstName} {detail.Teacher.LastName}" : string.Empty,
                Email = detail.Teacher?.Email ?? string.Empty,
                IdTypeTeachingAssignment = detail.TeachingAssignment.IdTypeTeachingAssignment,
                AssignmentType = detail.TypeTeachingAssignment?.Name ?? string.Empty,
                StatusRegister = detail.TeachingAssignment.StatusRegister,
                IsConfirmed = detail.TeachingAssignment.IsConfirmed
            }).ToList();
            return result;
        }

        private async Task EnsureUserCanReadInscriptionAssignmentsAsync(
            int inscriptionId,
            CurrentUserInfo? currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser?.UserId is not int userId || userId <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para consultar las asignaciones docentes.");

            IEnumerable<string> permissions = currentUser.Permissions ?? (IEnumerable<string>)Array.Empty<string>();
            if (permissions.Any(GlobalAdminReadPermissions.Contains))
                return;

            var inscription = await _unitOfWork.GetRepository<InscriptionModality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscriptionId && x.StatusRegister, cancellationToken);

            if (inscription == null)
            {
                if (permissions.Any(AnyModalityManagementPermissions.Contains))
                    return;

                throw new ForbiddenAccessException("No tienes permiso para consultar las asignaciones docentes de esta inscripción.");
            }

            var isStudentInInscription = await _unitOfWork.GetRepository<UserInscriptionModality, int>().AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdUser == userId &&
                     x.StatusRegister,
                cancellationToken);

            if (isStudentInInscription)
                return;

            var isConfirmedTeacher = await _unitOfWork.GetRepository<TeachingAssignment, int>().AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);

            if (isConfirmedTeacher)
                return;

            var modality = await _unitOfWork.GetRepository<Modality, int>()
                .GetFirstOrDefaultAsync(x => x.Id == inscription.IdModality && x.StatusRegister, cancellationToken);

            if (modality != null &&
                !string.IsNullOrWhiteSpace(modality.Code) &&
                ModalityManagementPermissions.TryGetValue(modality.Code, out var allowedModalityPermissions) &&
                permissions.Any(allowedModalityPermissions.Contains))
            {
                return;
            }

            throw new ForbiddenAccessException("No tienes permiso para consultar las asignaciones docentes de esta inscripción.");
        }
    }
}