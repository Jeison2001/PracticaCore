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
            var uimRepo = _unitOfWork.GetRepository<UserInscriptionModality, int>();
            var isStudentInInscription = await uimRepo.AnyAsync(
                x => x.IdInscriptionModality == inscriptionId && x.IdUser == currentUserId && x.StatusRegister,
                cancellationToken);

            if (isStudentInInscription)
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
    }
}
