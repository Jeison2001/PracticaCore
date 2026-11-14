using Domain.Common.Users;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services.Documents;

namespace Application.Common.Services.Documents
{
    /// <summary>
    /// Guard de acceso a documentos. Centraliza la autorizacion a nivel de dato: es
    /// fail-closed, deniega salvo que el usuario este explicitamente vinculado a la
    /// inscripcion del documento (como estudiante o como docente).
    /// </summary>
    public class DocumentAccessGuard : IDocumentAccessGuard
    {
        // Permisos habilitados para lectura transversal de documentos de cualquier inscripcion.
        // Excluye explicitamente permisos de solo-revision docente (N4PGPAR, N4PGPPRR, N4PGPSR, N4PAF0PR, N4PAF1PR, N4PAF2PR).
        private static readonly HashSet<string> DocumentReadAllPermissions = new(StringComparer.Ordinal)
        {
            PermissionCodes.Configuraciones.N3CFSU,
            PermissionCodes.Configuraciones.N2CFDO,
            PermissionCodes.Configuraciones.N3CFDOT,

            PermissionCodes.ModalidadesGrado.N2MGP,
            PermissionCodes.ModalidadesGrado.N3MGPG,
            PermissionCodes.ModalidadesGrado.N3MGPR,

            PermissionCodes.ProyectoGrado.N4PGPPG,
            PermissionCodes.ProyectoGrado.N4PGPPR,
            PermissionCodes.ProyectoGrado.N4PGPPA,
            PermissionCodes.ProyectoGrado.N4PGPAG,
            PermissionCodes.ProyectoGrado.N4PGPAA,
            PermissionCodes.ProyectoGrado.N4PGPPRG,
            PermissionCodes.ProyectoGrado.N4PGPSG,

            PermissionCodes.PracticaAcademica.N4PAF0PG,
            PermissionCodes.PracticaAcademica.N4PAF0PA,
            PermissionCodes.PracticaAcademica.N4PAF1PG,
            PermissionCodes.PracticaAcademica.N4PAF2PG,

            PermissionCodes.CoTerminal.N3CTSGG,
            PermissionCodes.CoTerminal.N3CTSGR,
            PermissionCodes.Seminario.N3SASGG,
            PermissionCodes.Seminario.N3SASGR,
            PermissionCodes.PublicacionArticulo.N3PCSGG,
            PermissionCodes.PublicacionArticulo.N3PCSGR,
            PermissionCodes.GradoPromedio.N3GPSGG,
            PermissionCodes.GradoPromedio.N3GPSGR,
            PermissionCodes.SaberPro.N3SPSGG,
            PermissionCodes.SaberPro.N3SPSGR,
        };

        private readonly IUnitOfWork _unitOfWork;

        public DocumentAccessGuard(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task EnsureUserCanModifyAsync(Document entity, int currentUserId, CancellationToken cancellationToken = default)
        {
            // Fail-closed: sin identidad de usuario => denegar.
            if (currentUserId <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para modificar este documento.");

            // Un documento sin inscripcion asociada no puede verificar membresia => denegar.
            if (!entity.IdInscriptionModality.HasValue)
                throw new ForbiddenAccessException("El documento no esta asociado a ninguna inscripcion. No tienes permiso para modificarlo.");

            var inscriptionId = entity.IdInscriptionModality.Value;

            // Permitir si el usuario es estudiante vinculado a la inscripcion (registro activo).
            if (await IsStudentInInscriptionAsync(inscriptionId, currentUserId, cancellationToken))
                return;

            // O si es docente asignado a la inscripcion.
            var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            var isTeacherInInscription = await taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId && x.IdTeacher == currentUserId,
                cancellationToken);

            if (isTeacherInInscription)
                return;

            throw new ForbiddenAccessException("No tienes permiso para modificar este documento. No estas vinculado a esta inscripcion.");
        }

        public async Task EnsureUserCanReadAsync(int? inscriptionModalityId, CurrentUserInfo currentUser, CancellationToken cancellationToken = default)
        {
            // Fail-closed: sin identidad de usuario => denegar.
            if (currentUser?.UserId is null or <= 0)
                throw new ForbiddenAccessException("Usuario no autenticado. No tienes permiso para consultar este documento.");

            // Sin inscripcion asociada (nula o invalida) => denegar.
            if (inscriptionModalityId is null or <= 0)
                throw new ForbiddenAccessException("El documento no esta asociado a ninguna inscripcion. No tienes permiso para consultarlo.");

            if (currentUser.Permissions != null && currentUser.Permissions.Any(DocumentReadAllPermissions.Contains))
                return;

            var userId = currentUser.UserId.Value;
            var inscriptionId = inscriptionModalityId.Value;

            if (await IsStudentInInscriptionAsync(inscriptionId, userId, cancellationToken))
                return;

            var taRepo = _unitOfWork.GetRepository<TeachingAssignment, int>();
            var isConfirmedTeacher = await taRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId &&
                     x.IdTeacher == userId &&
                     x.StatusRegister &&
                     x.RevocationDate == null &&
                     x.IsConfirmed,
                cancellationToken);

            if (isConfirmedTeacher)
                return;

            throw new ForbiddenAccessException("No tienes permiso para consultar este documento. No estas vinculado a esta inscripcion.");
        }

        private Task<bool> IsStudentInInscriptionAsync(int inscriptionId, int userId, CancellationToken cancellationToken)
        {
            var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            return uimRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId && x.IdUser == userId && x.StatusRegister,
                cancellationToken);
        }
    }
}
